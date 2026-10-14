using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GITC.Core
{
    // Text Input Module: the console. Typed text is one logical line (inputText); output lines are wrapped
    // to the console width and kept newest-first in history so they can be scrolled back.
    internal class TIMModule : GameModule
    {
        // What the player is typing, without the prompt
        public StringBuilder inputText { get; set; }
        public String consoleToken { get; set; }
        // Newest line at index 0
        public List<string> history { get; set; }
        // Submitted lines, oldest first (Up/Down recall)
        public List<string> commandLog { get; set; }
        // 0 = following the newest line; larger = scrolled back that many lines
        public int scrollOffset { get; set; }
        // -1 = not recalling; otherwise how far back in commandLog
        private int recallIndex { get; set; }
        private string recallDraft { get; set; }
        public CommandRegistry commandRegistry { get; set; }
        // Commands the player has been told to type, with their countdowns
        public List<CommandChallenge> challenges { get; set; }
        // Called for every key the player types into the console: the character, '\b' for Backspace, '\n' for Enter.
        // Audio hooks in here (Phase 12).
        public Action<char> onKeyTyped { get; set; }
        // Cursor blinks twice a second and restarts solid whenever the player types
        private double totalSeconds { get; set; }
        // Game clock as the TIM last saw it (command cooldowns use it)
        public double elapsedSeconds { get { return totalSeconds; } }
        private double lastTypedSeconds { get; set; }
        // Older lines exist above what is shown / the view is scrolled back from the newest line
        public bool hasMoreAbove { get { return history.Count > scrollOffset + CONSOLEHISTORY_SIZE; } }
        public bool isScrolledBack { get { return scrollOffset > 0; } }
        private VirtualFileSystem _fileSystem;
        // Created on first use: the game state (and its SaveManager) does not exist yet while the first mission is being built
        public VirtualFileSystem fileSystem
        {
            get
            {
                if (_fileSystem == null)
                    _fileSystem = new VirtualFileSystem(currGame.currGameState.saveManager);
                return _fileSystem;
            }
        }
        // Where commands are being run: "mission" or "overworld" (CommandDef.scope)
        public string scope { get; set; }

        // Offsets and sizes are character-cell (8 px) multiples that match the 99x60 cell TIM region.
        // X/Y offsets are relative to the region's top-left corner.
        public const int CONSOLEHISTORY_SIZE = 27;
        public const int CONSOLEWRITELINE_SIZE = 99-6;
        public const int CONSOLEMAXHEIGHT = 56*8;
        public const int CONSOLEMINWIDTH = 3*8;
        // Lines kept for scrollback and the longest line the player can type
        public const int HISTORYLIMIT = 500;
        public const int MAXINPUT = 200;

        public TIMModule(GITCGame currGame) : base(currGame, ModuleTypes.tim, HudLayout.TIM)
        {
            consoleToken = "USER:> ";
            inputText = new StringBuilder();
            history = new List<string>();
            commandLog = new List<string>();
            scrollOffset = 0;
            recallIndex = -1;
            recallDraft = "";
            commandRegistry = new CommandRegistry();
            challenges = new List<CommandChallenge>();
            scope = "mission";
        }

        public override void Update(GameTime gameTime)
        {
            // Count down each challenge; one that runs out raises command_failed (Mission turns it into an OP penalty)
            totalSeconds = gameTime.TotalGameTime.TotalSeconds;
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (CommandChallenge challenge in challenges.ToArray())
            {
                challenge.remaining -= elapsed;
                if (challenge.remaining <= 0f)
                {
                    challenges.Remove(challenge);
                    AddHistoryLines("TIME OUT: " + challenge.command);
                    Raise(new ModuleEvent(ModuleEventTypes.command_failed, ModuleTypes.tim, 0, challenge.command));
                }
            }
        }

        // command_required: amount = seconds allowed, tag = the command to type. Takes focus so the player can answer (D2).
        public override void OnEvent(ModuleEvent moduleEvent)
        {
            if (moduleEvent.type == ModuleEventTypes.command_required)
            {
                challenges.Add(new CommandChallenge(moduleEvent.tag, moduleEvent.amount));
                manager?.SetFocus(ModuleTypes.tim);
            }
        }

        // Typed characters arrive through InputManager.typedChars (only while this module is focused)
        public override void HandleInput(InputManager input)
        {
            foreach (TextInputEventArgs typed in input.typedChars.ToArray())
                OnTextInput(typed);

            // Command recall
            if (input.KeyPressed(Keys.Up))
                Recall(1);
            if (input.KeyPressed(Keys.Down))
                Recall(-1);

            // Scrollback: PageUp/PageDown by half a screen, mouse wheel by 3 lines per notch
            if (input.KeyPressed(Keys.PageUp))
                Scroll(CONSOLEHISTORY_SIZE / 2);
            if (input.KeyPressed(Keys.PageDown))
                Scroll(-CONSOLEHISTORY_SIZE / 2);
            if (input.scrollDelta != 0)
                Scroll(input.scrollDelta / 120 * 3);
        }

        internal void OnTextInput(TextInputEventArgs e)
        {
            // Handle Backspace
            if (e.Key == Keys.Back)
            {
                if (inputText.Length > 0)
                    inputText.Remove(inputText.Length - 1, 1);
                KeyTyped('\b');
            }
            // Handle Enter/Return
            else if (e.Key == Keys.Enter)
            {
                KeyTyped('\n');
                SubmitText();
            }
            // Append valid character typing (filters out unprintable keys automatically)
            else if (e.Character != '\0' && !char.IsControl(e.Character))
            {
                if (inputText.Length < MAXINPUT)
                {
                    recallIndex = -1;
                    inputText.Append(e.Character);
                    KeyTyped(e.Character);
                }
            }
        }

        // Restarts the cursor blink and tells the typing hook
        private void KeyTyped(char key)
        {
            lastTypedSeconds = totalSeconds;
            onKeyTyped?.Invoke(key);
        }

        // Splits text into console-width lines; explicit line breaks are kept, an empty line stays one empty line
        public static List<string> WrapLines(string text)
        {
            List<string> lines = new List<string>();
            foreach (string part in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (part.Length == 0)
                {
                    lines.Add("");
                    continue;
                }
                for (int i = 0; i < part.Length; i += CONSOLEWRITELINE_SIZE)
                    lines.Add(part.Substring(i, Math.Min(CONSOLEWRITELINE_SIZE, part.Length - i)));
            }
            return lines;
        }

        // Wraps text and adds it as the newest lines, in reading order
        public void AddHistoryLines(string text)
        {
            foreach (string line in WrapLines(text))
                history.Insert(0, line);
            if (history.Count > HISTORYLIMIT)
                history.RemoveRange(HISTORYLIMIT, history.Count - HISTORYLIMIT);
        }

        private void SubmitText()
        {
            SubmitLine(inputText.ToString());
            inputText.Clear();
        }

        // Echoes the line to the history, remembers it for recall, and runs it
        public void SubmitLine(string line)
        {
            AddHistoryLines(consoleToken + line);
            if (line.Trim().Length > 0 && (commandLog.Count == 0 || commandLog[commandLog.Count - 1] != line))
                commandLog.Add(line);
            recallIndex = -1;
            scrollOffset = 0;

            // Answering a challenge clears it; if the text is also a real command it still runs
            CommandChallenge answered = challenges.Find(c => c.Matches(line));
            if (answered != null)
            {
                challenges.Remove(answered);
                Raise(new ModuleEvent(ModuleEventTypes.command_satisfied, ModuleTypes.tim, 0, answered.command));
                if (commandRegistry.Find(line.Trim().Split(' ')[0]) == null)
                {
                    AddHistoryLines("ACCEPTED: " + answered.command);
                    return;
                }
            }
            ProcessCommand(line);
        }

        // Runs the line through the command registry and prints its output
        private void ProcessCommand(string line)
        {
            CommandContext context = new CommandContext() { currGame = currGame, tim = this, registry = commandRegistry, scope = scope };
            string[] output = commandRegistry.Execute(line, context);
            if (output.Length > 0)
                AddHistoryLines(string.Join("\n", output));
        }

        public void ClearHistory()
        {
            history.Clear();
            scrollOffset = 0;
        }

        // direction +1 = older command, -1 = newer (back to the unsent draft)
        internal void Recall(int direction)
        {
            if (commandLog.Count == 0)
                return;
            if (direction > 0)
            {
                if (recallIndex == -1)
                    recallDraft = inputText.ToString();
                recallIndex = Math.Min(recallIndex + 1, commandLog.Count - 1);
            }
            else
            {
                if (recallIndex == -1)
                    return;
                recallIndex--;
            }
            inputText.Clear();
            inputText.Append(recallIndex == -1 ? recallDraft : commandLog[commandLog.Count - 1 - recallIndex]);
        }

        internal void Scroll(int lines)
        {
            scrollOffset = Math.Clamp(scrollOffset + lines, 0, Math.Max(0, history.Count - CONSOLEHISTORY_SIZE));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Company logo behind the console text, drawn first so the text stays on top
            DrawLogo(spriteBatch);

            SpriteFont font = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);

            //Text variables
            Vector2 fontSize = font.MeasureString("H");
            int textWidth = (int)fontSize.X;
            int textHeight = (int)fontSize.Y;
            while (textHeight % textWidth != 0)
            {
                textHeight++;
            }

            int originX = region.X + CONSOLEMINWIDTH;
            int originY = region.Y + CONSOLEMAXHEIGHT;

            // The input line shows the end of what has been typed once it is wider than the console
            string inputLine = consoleToken + inputText.ToString();
            string visibleInput = inputLine.Substring(Math.Max(0, inputLine.Length - CONSOLEWRITELINE_SIZE));
            spriteBatch.DrawString(font, visibleInput, new Vector2(originX, originY), Color.White);

            // History, newest nearest the input line
            for (int i = 0; i < CONSOLEHISTORY_SIZE; i++)
            {
                int index = scrollOffset + i;
                if (index < history.Count)
                    spriteBatch.DrawString(font, history[index], new Vector2(originX, originY - (textHeight * (i + 1))), Color.White);
            }

            // Pending command challenges: a strip across the top, flashing in the last 3 seconds
            for (int i = 0; i < challenges.Count; i++)
            {
                CommandChallenge challenge = challenges[i];
                Rectangle strip = new Rectangle(region.X, region.Y + 4 + i * textHeight, region.Width, textHeight);
                bool urgent = challenge.remaining < 3f && (int)(challenge.remaining * 4f) % 2 == 0;
                spriteBatch.Draw(currGame._content.pixel, strip, Color.Black * 0.85f);
                spriteBatch.DrawString(font, "REQUIRED: " + challenge.command + " - " + (int)Math.Ceiling(challenge.remaining) + "s",
                                       new Vector2(originX, strip.Y), urgent ? Color.Red : Color.Yellow);
            }

            // Scrollback indicators: older lines above the view, and a way back to the newest line
            int indicatorY = region.Y + 4 + challenges.Count * textHeight;
            if (hasMoreAbove)
                DrawIndicator(spriteBatch, font, "^ MORE ABOVE (PgUp)", indicatorY);
            if (isScrolledBack)
                DrawIndicator(spriteBatch, font, "v " + scrollOffset + " LINES BELOW (PgDn)", originY + textHeight);

            // Cursor, blinking
            bool cursorOn = ((totalSeconds - lastTypedSeconds) % 1.0) < 0.5;
            Rectangle cursRect = new Rectangle(new Point(visibleInput.Length * textWidth + originX,
                                                         (textHeight / 4) * 3 + originY),
                                               new Point(textWidth,
                                                         textHeight / 4));
            if (cursorOn)
                spriteBatch.Draw(currGame._content.pixel, cursRect, Color.White);
        }

        // Small right-aligned label on a black strip
        private void DrawIndicator(SpriteBatch spriteBatch, SpriteFont font, string text, int y)
        {
            Vector2 size = font.MeasureString(text);
            Rectangle area = new Rectangle(region.Right - CONSOLEMINWIDTH - (int)size.X - 8, y, (int)size.X + 8, (int)size.Y);
            spriteBatch.Draw(currGame._content.pixel, area, Color.Black * 0.85f);
            spriteBatch.DrawString(font, text, new Vector2(area.X + 4, y), Color.Gray);
        }

        // Centered in the region, faint, as authored (point sampling keeps the pixel art crisp)
        private void DrawLogo(SpriteBatch spriteBatch)
        {
            CompanyTheme theme = currGame.currGameState.currentTheme;
            if (string.IsNullOrEmpty(theme.logoKey) || theme.logoOpacity <= 0f)
                return;
            Rectangle logoArea = new Rectangle(region.Center.X - CompanyTheme.LOGOSIZE / 2,
                                               region.Center.Y - CompanyTheme.LOGOSIZE / 2,
                                               CompanyTheme.LOGOSIZE,
                                               CompanyTheme.LOGOSIZE);
            spriteBatch.Draw(currGame._content.GetTexture(theme.logoKey), logoArea, Color.White * theme.logoOpacity);
        }
    }
}

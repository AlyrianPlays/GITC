using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GITC.Core
{
    // Text Input Module: the console. Ported from GameState/GITCGame (T-5.1); later tasks rework wrapping,
    // add the command registry, the virtual filesystem and timed command prompts.
    internal class TIMModule : GameModule
    {
        public StringBuilder userText { get; set; }
        public String consoleToken { get; set; }
        public String[] consoleHistory { get; set; }
        // Offsets and sizes are character-cell (8 px) multiples that match the 99x60 cell TIM region.
        // X/Y offsets are relative to the region's top-left corner.
        public const int CONSOLEHISTORY_SIZE = 27;
        public const int CONSOLEWRITELINE_SIZE = 99-6;
        public const int CONSOLEMAXHEIGHT = 56*8;
        public const int CONSOLEMINWIDTH = 3*8;

        public TIMModule(GITCGame currGame) : base(currGame, ModuleTypes.tim, HudLayout.TIM)
        {
            consoleToken = "USER:> ";
            consoleHistory = new String[CONSOLEHISTORY_SIZE];
            userText = new StringBuilder(consoleToken);
        }

        public override void Update(GameTime gameTime)
        {
        }

        // Typed characters arrive through InputManager.typedChars (only while this module is focused)
        public override void HandleInput(InputManager input)
        {
            foreach (TextInputEventArgs typed in input.typedChars.ToArray())
                OnTextInput(typed);
        }

        internal void OnTextInput(TextInputEventArgs e)
        {
            // Handle Backspace
            if (e.Key == Microsoft.Xna.Framework.Input.Keys.Back)
            {
                if (userText.Length > consoleToken.Length)
                {
                    if (userText.ToString().EndsWith(Environment.NewLine))
                    {
                        userText.Remove(userText.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                    }
                    if (userText.ToString().Replace(Environment.NewLine, "").Length > consoleToken.Length)
                    {
                        userText.Remove(userText.Length - 1, 1);
                    }
                }
            }
            // Handle Enter/Return if you want to submit text
            else if (e.Key == Microsoft.Xna.Framework.Input.Keys.Enter)
            {
                SubmitText();
            }
            // Append valid character typing (filters out unprintable keys automatically)
            else if (e.Character != '\0' && !char.IsControl(e.Character))
            {
                if (userText.Length > 0 && userText.ToString().Replace(Environment.NewLine, "").Length % CONSOLEWRITELINE_SIZE == 0)
                {
                    userText.Append(Environment.NewLine);
                }
                userText.Append(e.Character);
            }
        }

        public static int GetLineCount(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;

            int count = 1;
            int index = 0;

            while ((index = input.IndexOf('\n', index)) != -1)
            {
                count++;
                index++;
            }

            return count;
        }

        private void SubmitText()
        {
            int userLineNum = GetLineCount(userText.ToString());
            string[] userLines = userText.ToString().Split(Environment.NewLine);
            for (int k = 0; k < userLineNum; k++)
            {
                for (int i = CONSOLEHISTORY_SIZE - 1; i > 0; i--)
                {
                    consoleHistory[i] = consoleHistory[i - 1];
                }
                consoleHistory[0] = userLines[k];
            }
            ProcessCommand();
            userText.Clear();
            userText = new StringBuilder(consoleToken);
        }

        private void ProcessCommand()
        {
            string command = userText.ToString().Remove(0, consoleToken.Length).Split(' ')[0];
            string args = string.Join(' ', userText.ToString().Remove(0, consoleToken.Length).Split(' ')[1..]);
            if (Enum.GetNames<Commands>().Contains(command))
            {
                switch (Enum.Parse(typeof(Commands), command))
                {
                    case Commands.echo:
                        string[] outputLines = string.Join(Environment.NewLine, Enumerable.Range(0, (args.Length + CONSOLEWRITELINE_SIZE - 1) / CONSOLEWRITELINE_SIZE).Select(i => args.ToString().Substring(i * CONSOLEWRITELINE_SIZE, Math.Min(CONSOLEWRITELINE_SIZE, args.Length - i * CONSOLEWRITELINE_SIZE)))).Split(Environment.NewLine);
                        int outputLineNum = GetLineCount(string.Join(Environment.NewLine, outputLines));
                        for (int k = 0; k < outputLineNum; k++)
                        {
                            for (int i = CONSOLEHISTORY_SIZE - 1; i > 0; i--)
                            {
                                consoleHistory[i] = consoleHistory[i - 1];
                            }
                            consoleHistory[0] = outputLines[k];
                        }
                        break;
                }
            }
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

            // Draw the accumulating string buffer
            string inputLine = userText.ToString().Replace(Environment.NewLine, "");
            string visibleInput = inputLine.Substring(Math.Max(0, inputLine.Length - CONSOLEWRITELINE_SIZE));
            spriteBatch.DrawString(font, visibleInput, new Vector2(originX, originY), Color.White);
            for (int i = 0; i < CONSOLEHISTORY_SIZE; i++)
            {
                if (consoleHistory[i] != null)
                    spriteBatch.DrawString(font, consoleHistory[i], new Vector2(originX, originY - (textHeight * (i + 1))), Color.White);
            }

            // Cursor
            Rectangle cursRect = new Rectangle(new Point(visibleInput.Length * textWidth + originX,
                                                         (textHeight / 4) * 3 + originY),
                                               new Point(textWidth,
                                                         textHeight / 4));
            spriteBatch.Draw(currGame._content.pixel, cursRect, Color.White);
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

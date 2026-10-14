using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GITC.Core
{
    internal class GameState
    {
        public Sprite mainChar { get; set; }
        public List<Sprite> activeSprites { get; set; }
        public List<Sprite> elementSprites { get; set; }
        public GameStates currentGameState { get; set; }
        public InputManager input { get; set; }
        public Mission currMission { get; set; }
        // Colours the module borders and logo; follows the mission, the overworld sets its own
        public CompanyTheme currentTheme { get; set; }
        // The running mission's modules (null outside a mission)
        public ModuleManager moduleManager { get { return currMission?.moduleManager; } }
        public SaveManager saveManager { get; set; }
        // True once the end of currMission has been recorded in the save
        private bool missionReported { get; set; }
        public MissionResultScreen resultScreen { get; set; }
        private KeyboardState lastDebugKeys { get; set; }
        // Pass-throughs to InputManager, kept so existing callers keep working
        public MouseState currMouseState { get { return input.currMouseState; } }
        public MouseState lastMouseState { get { return input.lastMouseState; } }
        public List<bool> currKeyStates { get { return input.currKeyStates; } }
        public List<bool> lastKeyStates { get { return input.lastKeyStates; } }
        public double currGameTime { get; set; }
        public double lastGameTime { get; set; }
        public GITCGame currGame { get; set; }
        //Deprecated: the console moved into TIMModule (T-5.1)
        //public StringBuilder userText { get; set; }
        //public String consoleToken { get; set;  }
        //public String[] consoleHistory { get; set; }
        //public const int CONSOLEHISTORY_SIZE = 27;
        //public const int CONSOLEWRITELINE_SIZE = 102-11;
        //public const int CONSOLEMAXHEIGHT = 63*8;
        //public const int CONSOLEMINWIDTH = 11*8;
        //public const int CONSOLEMAXWIDTH = 102*8;
        public GameState(GITCGame currGame)
        {
            this.currGame = currGame;
            input = new InputManager(currGame);
            currentTheme = CompanyTheme.neutral;
            saveManager = new SaveManager();
            saveManager.Load();
            StartDebugMission();
            resultScreen = new MissionResultScreen(currGame);
            //Deprecated: console state moved into TIMModule (T-5.1)
        //    consoleToken = "USER:> ";
        //    consoleHistory = new String[CONSOLEHISTORY_SIZE];
        //    userText = new StringBuilder(consoleToken);
        //    // Subscribe to the text input event
        //    currGame.Window.TextInput += OnTextInput;
            //testing
            mainChar = new Sprite();
            currentGameState = GameStates.play;
            currGameTime = 0;
            lastGameTime = 0;
        }
        //TESTING, REMOVE AFTER: stands in for the overworld starting a mission (T-11.1)
        private void StartDebugMission()
        {
            MissionDef debugDef = new MissionDef() { id = "debug_mission", companyId = "swiftail", name = "Debug mission", durationSeconds = 60, startingOps = 30 };
            //TESTING: env var GITC_DEBUGSECONDS shortens the mission so the end screens can be reached without keys
            if (int.TryParse(Environment.GetEnvironmentVariable("GITC_DEBUGSECONDS"), out int debugSeconds))
                debugDef.durationSeconds = debugSeconds;
            currMission = new Mission(currGame, debugDef);
            missionReported = false;
            currentTheme = currMission.theme;
        }

        private void ReturnToOverworld()
        {
            currMission = null;
            currentTheme = CompanyTheme.neutral;
            currentGameState = GameStates.overworld;
        }

        public void Update(GameTime gameTime)
        {
            if (currGameTime != 0)
                lastGameTime = currGameTime;
            currGameTime = gameTime.TotalGameTime.TotalMilliseconds;

            // Set key states
            input.Update();

            //TESTING, REMOVE AFTER: confirm Pressed fires once per key press
            foreach (KeyBinds kb in Enum.GetValues(typeof(KeyBinds)))
                if (input.Pressed(kb))
                    System.Diagnostics.Debug.WriteLine("Pressed " + kb);

            //TESTING, REMOVE AFTER: F9 = -10 OP, F10 = +10 OP, F11 = jump to 5 seconds left
            KeyboardState debugKeys = Keyboard.GetState();
            if (currMission != null && currentGameState == GameStates.play)
            {
                if (debugKeys.IsKeyDown(Keys.F9) && !lastDebugKeys.IsKeyDown(Keys.F9))
                    currMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.op_penalty, ModuleTypes.pm, 10));
                if (debugKeys.IsKeyDown(Keys.F10) && !lastDebugKeys.IsKeyDown(Keys.F10))
                    currMission.moduleManager.Raise(new ModuleEvent(ModuleEventTypes.op_gain, ModuleTypes.pm, 10));
                if (debugKeys.IsKeyDown(Keys.F11) && !lastDebugKeys.IsKeyDown(Keys.F11))
                    currMission.timeRemaining = Math.Min(currMission.timeRemaining, 5f);
            }
            lastDebugKeys = debugKeys;

            // Esc toggles the pause stub (T-11.6 turns it into a real menu)
            if (input.Pressed(KeyBinds.settings))
            {
                if (currentGameState == GameStates.play)
                    currentGameState = GameStates.pause;
                else if (currentGameState == GameStates.pause)
                    currentGameState = GameStates.play;
            }

            // Update game state logic here
            switch (currentGameState)
            {
                case GameStates.settings:
                    break;
                case GameStates.inventory:
                    break;
                case GameStates.play:
                    PlayHandler(gameTime);
                    break;
                case GameStates.mainmenu:
                    break;
                case GameStates.overworld:
                    //TESTING, REMOVE AFTER: Enter restarts the debug mission until the real overworld exists (T-11.1)
                    if (input.Pressed(KeyBinds.confirm))
                    {
                        StartDebugMission();
                        currentGameState = GameStates.play;
                    }
                    break;
                case GameStates.interact:
                    break;
                case GameStates.pause:
                    // Modules are frozen while paused
                    if (Keyboard.GetState().IsKeyDown(Keys.Q))
                        currGame.Exit();
                    break;
                default:
                    break;
            }

            // TIMModule drains the typed queue while it is focused; whatever is left is dropped here
            input.ClearTyped();
        }

        //Deprecated: the console moved into TIMModule (T-5.1)
        //internal void OnTextInput(object sender, TextInputEventArgs e)
        //{
        //    // Typing only reaches the console while the TIM is focused (D2); T-5.1 moves the console into TIMModule
        //    if (currentGameState != GameStates.play || currMission == null || currMission.status != MissionStatus.running
        //        || moduleManager.focusedModule != ModuleTypes.tim)
        //        return;

        //    // Handle Backspace
        //    if (e.Key == Microsoft.Xna.Framework.Input.Keys.Back)
        //    {
        //        if (userText.Length > consoleToken.Length)
        //        {
        //            if (userText.ToString().EndsWith(Environment.NewLine))
        //            {
        //                userText.Remove(userText.Length - Environment.NewLine.Length, Environment.NewLine.Length);
        //            }
        //            if (userText.ToString().Replace(Environment.NewLine, "").Length > consoleToken.Length) 
        //            {
        //                userText.Remove(userText.Length - 1, 1);
        //            }
        //        }
        //    }
        //    // Handle Enter/Return if you want to submit text
        //    else if (e.Key == Microsoft.Xna.Framework.Input.Keys.Enter)
        //    {
        //        SubmitText();
        //    }
        //    // Append valid character typing (filters out unprintable keys automatically)
        //    else if (e.Character != '\0' && !char.IsControl(e.Character))
        //    {
        //        if (userText.Length > 0 && userText.ToString().Replace(Environment.NewLine,"").Length % CONSOLEWRITELINE_SIZE == 0)
        //        {
        //            userText.Append(Environment.NewLine);
        //        }
        //        userText.Append(e.Character);
        //    }
        //}

        //public static int GetLineCount(string input)
        //{
        //    if (string.IsNullOrEmpty(input)) return 0;

        //    int count = 1;
        //    int index = 0;

        //    while ((index = input.IndexOf('\n', index)) != -1)
        //    {
        //        count++;
        //        index++;
        //    }

        //    return count;
        //}
        //private void SubmitText()
        //{
        //    int userLineNum = GetLineCount(userText.ToString());
        //    string[] userLines = userText.ToString().Split(Environment.NewLine);
        //    for (int k = 0; k < userLineNum; k++)
        //    {
        //        for (int i = CONSOLEHISTORY_SIZE - 1; i > 0; i--)
        //        {
        //            consoleHistory[i] = consoleHistory[i - 1];
        //        }
        //        consoleHistory[0] = userLines[k];
        //    }
        //    ProcessCommand();
        //    userText.Clear();
        //    userText = new StringBuilder(consoleToken);
        //}

        //private void ProcessCommand()
        //{
        //    string command = userText.ToString().Remove(0,consoleToken.Length).Split(' ')[0];
        //    string args = string.Join(' ',userText.ToString().Remove(0, consoleToken.Length).Split(' ')[1..]);
        //    if (Enum.GetNames<Commands>().Contains(command))
        //    {
        //        switch (Enum.Parse(typeof(Commands), command))
        //        {
        //            case Commands.echo:
        //                string[] outputLines = string.Join(Environment.NewLine, Enumerable.Range(0, (args.Length + CONSOLEWRITELINE_SIZE - 1) / CONSOLEWRITELINE_SIZE).Select(i => args.ToString().Substring(i * CONSOLEWRITELINE_SIZE, Math.Min(CONSOLEWRITELINE_SIZE, args.Length - i * CONSOLEWRITELINE_SIZE)))).Split(Environment.NewLine);
        //                int outputLineNum = GetLineCount(string.Join(Environment.NewLine,outputLines));
        //                for (int k = 0; k < outputLineNum; k++)
        //                {
        //                    for (int i = CONSOLEHISTORY_SIZE - 1; i > 0; i--)
        //                    {
        //                        consoleHistory[i] = consoleHistory[i - 1];
        //                    }
        //                    consoleHistory[0] = outputLines[k];
        //                }
        //                break;
        //        }
        //    }
        //}
        public void PlayHandler(GameTime gameTime)
        {
            // Time since last frame
            double deltaTime = currGameTime - lastGameTime;

            if (currMission == null)
                return;
            currMission.Update(gameTime, input);

            // Record the end of the mission once (autosave)
            if (currMission.status != MissionStatus.running && !missionReported)
            {
                missionReported = true;
                //Debug missions never touch the real save
                if (!currMission.def.id.StartsWith("debug"))
                    saveManager.OnMissionEnded(currMission.def.id, currMission.status == MissionStatus.won, currMission.operatorPoints, currMission.def.unlocks);
                System.Diagnostics.Debug.WriteLine("Mission " + currMission.def.id + " " + currMission.status + " with " + currMission.operatorPoints + " OP");
            }

            // Result screen is up: Enter returns to the overworld stub
            if (currMission.status != MissionStatus.running && input.Pressed(KeyBinds.confirm))
                ReturnToOverworld();
        }

        // Draws the modules (call with no SpriteBatch open)
        public void Draw(SpriteBatch spriteBatch)
        {
            if (currMission != null && (currentGameState == GameStates.play || currentGameState == GameStates.pause))
                moduleManager.Draw(spriteBatch);
        }

        // Drawn last, over everything (call with no SpriteBatch open)
        public void DrawOverlay(SpriteBatch spriteBatch)
        {
            if (currMission != null && currMission.status != MissionStatus.running && currentGameState == GameStates.play)
                resultScreen.Draw(spriteBatch, currMission);

            if (currentGameState == GameStates.overworld)
            {
                SpriteFont stubFont = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);
                string stub = "OVERWORLD (stub) - press Enter to start the debug mission";
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                spriteBatch.Draw(currGame._content.pixel, new Microsoft.Xna.Framework.Rectangle(0, 0, 1920, 1080), Microsoft.Xna.Framework.Color.Black * 0.6f);
                spriteBatch.DrawString(stubFont, stub, new Vector2(960 - stubFont.MeasureString(stub).X / 2, 520), Microsoft.Xna.Framework.Color.White);
                spriteBatch.End();
            }

            if (currentGameState != GameStates.pause)
                return;
            SpriteFont font = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);
            string line1 = "PAUSED";
            string line2 = "Esc: resume    Q: quit";
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            spriteBatch.Draw(currGame._content.pixel, new Microsoft.Xna.Framework.Rectangle(0, 0, 1920, 1080), Microsoft.Xna.Framework.Color.Black * 0.6f);
            spriteBatch.DrawString(font, line1, new Vector2(960 - font.MeasureString(line1).X / 2, 500), Microsoft.Xna.Framework.Color.White);
            spriteBatch.DrawString(font, line2, new Vector2(960 - font.MeasureString(line2).X / 2, 500 + font.MeasureString(line1).Y * 2), Microsoft.Xna.Framework.Color.White);
            spriteBatch.End();
        }

        //Deprecated: replaced by InputManager.Update (T-1.5)
        //        //Get all relevant keyboard IsKeyDown states
        //        public void SetKeyStates()
        //        {
        //            List<bool> newKeyStates = new List<bool>();
        //            lastKeyStates = currKeyStates;
        //            KeyboardState ks = Keyboard.GetState();

        //            //Get mouse positional and click information
        //            lastMouseState = currMouseState;
        //            currMouseState = Mouse.GetState();

        //            //GOLDEN FORMULA FOR SCALING MOUSE POSITION, NEVER LOSE THIS!!!
        //            Vector2 scaledMousePosition = new Vector2(((currMouseState.X) / currGame.currCamera.Zoom) + ((currGame.currCamera.Pos.X) - (currGame.currCamera._viewportWidth / 2) / currGame.currCamera.Zoom),
        //                                              ((currMouseState.Y) / currGame.currCamera.Zoom) + ((currGame.currCamera.Pos.Y) - (currGame.currCamera._viewportHeight / 2) / currGame.currCamera.Zoom));

        //            for (int i = 0; i < currGame.currUserInfo.keyBinds.Count + Enum.GetNames(typeof(WeirdKeys)).Length; i++)
        //            {
        //                newKeyStates.Add(false);
        //            }

        //            for (int i = 0; i < currGame.currUserInfo.keyBinds.Count; i++)
        //            {
        //                if (currGame.currUserInfo.keyBinds[i] >= (int)WeirdKeys.leftclick)
        //                {
        //                    switch (currGame.currUserInfo.keyBinds[i])
        //                    {
        //                        case (int)WeirdKeys.leftclick:
        //                            newKeyStates[i] = currMouseState.LeftButton == ButtonState.Pressed;
        //                            break;
        //                        case (int)WeirdKeys.rightclick:
        //                            newKeyStates[i] = currMouseState.RightButton == ButtonState.Pressed;
        //                            break;
        //                        case (int)WeirdKeys.scroll_up:
        //                            newKeyStates[i] = currMouseState.ScrollWheelValue > lastMouseState.ScrollWheelValue;
        //                            break;
        //                        case (int)WeirdKeys.scroll_down:
        //                            newKeyStates[i] = currMouseState.ScrollWheelValue < lastMouseState.ScrollWheelValue;
        //                            break;
        //                        default:
        //                            break;
        //                    }
        //                }
        //                else
        //                {
        //                    newKeyStates[i] = ks.IsKeyDown((Keys)currGame.currUserInfo.keyBinds[i]);
        //                }
        //            }
        //            currKeyStates = newKeyStates;
        //        }
    }
}

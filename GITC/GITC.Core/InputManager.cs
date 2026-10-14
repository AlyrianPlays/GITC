using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // Owns per-frame keyboard/mouse state and edge detection (curr/last pairs).
    // Key states are indexed by KeyBinds, the same way keyBinds in UserInfo is.
    internal class InputManager
    {
        public GITCGame currGame { get; set; }
        public List<bool> currKeyStates { get; set; }
        public List<bool> lastKeyStates { get; set; }
        public MouseState currMouseState { get; set; }
        public MouseState lastMouseState { get; set; }
        // Raw keyboard, for keys that have no KeyBinds entry (arrows, PageUp/PageDown in the TIM)
        public KeyboardState currKeyboard { get; set; }
        public KeyboardState lastKeyboard { get; set; }
        // Mouse wheel movement this frame (120 per notch)
        public int scrollDelta { get; set; }
        // Mouse position in 1920x1080 logical coordinates (letterbox-aware)
        public Point mouseLogical { get; set; }
        // Mouse position through the camera (the golden formula, kept for world-space use)
        public Vector2 scaledMousePosition { get; set; }
        // Characters/keys typed since the consumer last drained the queue (cleared by ClearTyped)
        public Queue<TextInputEventArgs> typedChars { get; set; }

        public InputManager(GITCGame currGame)
        {
            this.currGame = currGame;
            currKeyStates = new List<bool>();
            lastKeyStates = new List<bool>();
            typedChars = new Queue<TextInputEventArgs>();
            for (int i = 0; i < Enum.GetValues(typeof(KeyBinds)).Length + Enum.GetNames(typeof(WeirdKeys)).Length; i++)
            {
                currKeyStates.Add(false);
                lastKeyStates.Add(false);
            }
            currGame.Window.TextInput += OnTextInput;
        }

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            typedChars.Enqueue(e);
        }

        public void ClearTyped()
        {
            typedChars.Clear();
        }

        // Call once per frame, before anything reads input
        public void Update()
        {
            List<bool> newKeyStates = new List<bool>();
            lastKeyStates = currKeyStates;
            KeyboardState ks = Keyboard.GetState();
            lastKeyboard = currKeyboard;
            currKeyboard = ks;

            //Get mouse positional and click information
            lastMouseState = currMouseState;
            currMouseState = Mouse.GetState();
            mouseLogical = currGame.ScreenToLogical(currMouseState.Position);
            scrollDelta = currMouseState.ScrollWheelValue - lastMouseState.ScrollWheelValue;

            //GOLDEN FORMULA FOR SCALING MOUSE POSITION, NEVER LOSE THIS!!!
            scaledMousePosition = new Vector2(((currMouseState.X) / currGame.currCamera.Zoom) + ((currGame.currCamera.Pos.X) - (currGame.currCamera._viewportWidth / 2) / currGame.currCamera.Zoom),
                                              ((currMouseState.Y) / currGame.currCamera.Zoom) + ((currGame.currCamera.Pos.Y) - (currGame.currCamera._viewportHeight / 2) / currGame.currCamera.Zoom));

            for (int i = 0; i < currGame.currUserInfo.keyBinds.Count + Enum.GetNames(typeof(WeirdKeys)).Length; i++)
            {
                newKeyStates.Add(false);
            }

            for (int i = 0; i < currGame.currUserInfo.keyBinds.Count; i++)
            {
                if (currGame.currUserInfo.keyBinds[i] >= (int)WeirdKeys.leftclick)
                {
                    switch (currGame.currUserInfo.keyBinds[i])
                    {
                        case (int)WeirdKeys.leftclick:
                            newKeyStates[i] = currMouseState.LeftButton == ButtonState.Pressed;
                            break;
                        case (int)WeirdKeys.rightclick:
                            newKeyStates[i] = currMouseState.RightButton == ButtonState.Pressed;
                            break;
                        case (int)WeirdKeys.scroll_up:
                            newKeyStates[i] = currMouseState.ScrollWheelValue > lastMouseState.ScrollWheelValue;
                            break;
                        case (int)WeirdKeys.scroll_down:
                            newKeyStates[i] = currMouseState.ScrollWheelValue < lastMouseState.ScrollWheelValue;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    newKeyStates[i] = ks.IsKeyDown((Keys)currGame.currUserInfo.keyBinds[i]);
                }
            }
            currKeyStates = newKeyStates;
        }

        private bool Curr(KeyBinds kb)
        {
            return (int)kb < currKeyStates.Count && currKeyStates[(int)kb];
        }

        private bool Last(KeyBinds kb)
        {
            return (int)kb < lastKeyStates.Count && lastKeyStates[(int)kb];
        }

        // A press fires on curr && !last
        public bool Pressed(KeyBinds kb)
        {
            return Curr(kb) && !Last(kb);
        }

        public bool Held(KeyBinds kb)
        {
            return Curr(kb);
        }

        public bool Released(KeyBinds kb)
        {
            return !Curr(kb) && Last(kb);
        }

        // Any key went down this frame (curr && !last)
        public bool KeyPressed(Keys key)
        {
            return currKeyboard.IsKeyDown(key) && !lastKeyboard.IsKeyDown(key);
        }

        // Left button went down this frame inside the rectangle (logical coordinates)
        public bool MouseClicked(Rectangle area)
        {
            return currMouseState.LeftButton == ButtonState.Pressed
                && lastMouseState.LeftButton == ButtonState.Released
                && area.Contains(mouseLogical);
        }
    }
}

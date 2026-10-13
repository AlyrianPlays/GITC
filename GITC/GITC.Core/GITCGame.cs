using GITC.Core.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

namespace GITC.Core
{
    /// <summary>
    /// The main class for the game, responsible for managing game components, settings, 
    /// and platform-specific configurations.
    /// </summary>
    public class GITCGame : Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphicsDeviceManager;
        private SpriteBatch _spriteBatch;
        internal Camera currCamera { get; set; }
        internal UserInfo currUserInfo { get; set; }
        internal GameState currGameState { get; set; }
        private SpriteFont _gameFont { get; set; }
        private RenderTarget2D _renderTarget { get; set; }
        internal Texture2D _pixelTexture { get; set; }
        private Rectangle _viewRect { get; set; }
        private Dictionary<string, Texture2D> _backgroundGameTextures {  get; set; }
        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        /// <summary>
        /// Initializes a new instance of the game. Configures platform-specific settings, 
        /// initializes services like settings and leaderboard managers, and sets up the 
        /// screen manager for screen transitions.
        /// </summary>
        public GITCGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this) 
            {
                // Set the window size to the current display size
                PreferredBackBufferWidth = Camera.AutoResolution().Item1,
                PreferredBackBufferHeight = Camera.AutoResolution().Item2,
            };

            // Share GraphicsDeviceManager as a service.
            Services.AddService(typeof(GraphicsDeviceManager), graphicsDeviceManager);

            Content.RootDirectory = "Content";

            // Configure screen orientations.
            graphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }

        /// <summary>
        /// Initializes the game, including setting up localization and adding the 
        /// initial screens to the ScreenManager.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Load supported languages and set the default language.
            List<CultureInfo> cultures = LocalizationManager.GetSupportedCultures();
            var languages = new List<CultureInfo>();
            for (int i = 0; i < cultures.Count; i++)
            {
                languages.Add(cultures[i]);
            }

            // TODO You should load this from a settings file or similar,
            // based on what the user or operating system selected.
            var selectedLanguage = LocalizationManager.DEFAULT_CULTURE_CODE;
            LocalizationManager.SetCulture(selectedLanguage);
            currCamera = new Camera(GraphicsDevice.Viewport,
                Camera.AutoResolution().Item1,
                Camera.AutoResolution().Item2,
                1.0f);
            currUserInfo = new UserInfo();
            currGameState = new GameState(this);
        }

        /// <summary>
        /// Loads game content, such as textures and particle systems.
        /// </summary>
        protected override void LoadContent()
        {
            _gameFont = Content.Load<SpriteFont>("Fonts/File");
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            //Created once and reused every frame (was allocated per frame in Draw)
            _renderTarget = new RenderTarget2D(GraphicsDevice, 1920, 1080);
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            // TODO: Add all textures to this load loop, based on string list or some book-keeping method
            _backgroundGameTextures = new Dictionary<string, Texture2D>()
            {
                { "background", Content.Load<Texture2D>("HUD") },
                { "screen_overlay", Content.Load<Texture2D>("HUD_ScreenOverlay") }
            };
            graphicsDeviceManager.IsFullScreen = true;
            graphicsDeviceManager.ApplyChanges();
            base.LoadContent();
        }

        /// <summary>
        /// Destination rectangle on the back buffer for the 1920x1080 target: uniform scale, centered.
        /// </summary>
        private Rectangle GetLetterboxRect()
        {
            int bbWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int bbHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float scale = Math.Min(bbWidth / Globals.ScreenWidthLogical, bbHeight / Globals.ScreenHeightLogical);
            int width = (int)(Globals.ScreenWidthLogical * scale);
            int height = (int)(Globals.ScreenHeightLogical * scale);
            return new Rectangle((bbWidth - width) / 2, (bbHeight - height) / 2, width, height);
        }

        /// <summary>
        /// Converts a window/back-buffer point (e.g. the mouse) to 1920x1080 logical coordinates,
        /// using the same letterbox math as Draw.
        /// </summary>
        internal Point ScreenToLogical(Point screenPoint)
        {
            Rectangle view = GetLetterboxRect();
            return new Point((int)((screenPoint.X - view.X) * Globals.ScreenWidthLogical / view.Width),
                             (int)((screenPoint.Y - view.Y) * Globals.ScreenHeightLogical / view.Height));
        }

        /// <summary>
        /// Releases the render target and shared pixel texture created in LoadContent.
        /// </summary>
        protected override void UnloadContent()
        {
            _renderTarget?.Dispose();
            _pixelTexture?.Dispose();
            base.UnloadContent();
        }

        /// <summary>
        /// Updates the game's logic, called once per frame.
        /// </summary>
        /// <param name="gameTime">
        /// Provides a snapshot of timing values used for game updates.
        /// </param>
        protected override void Update(GameTime gameTime)
        {
            // Exit the game if the Back button (GamePad) or Escape key (Keyboard) is pressed.
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            currGameState.Update(gameTime);

            //TESTING, REMOVE AFTER: verify window-to-logical mouse mapping
            if (currGameState.currMouseState.LeftButton == ButtonState.Pressed && currGameState.lastMouseState.LeftButton == ButtonState.Released)
                System.Diagnostics.Debug.WriteLine("Click logical: " + ScreenToLogical(currGameState.currMouseState.Position));

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game's graphics, called once per frame.
        /// </summary>
        /// <param name="gameTime">
        /// Provides a snapshot of timing values used for rendering.
        /// </param>
        protected override void Draw(GameTime gameTime)
        {
            //Text variables
            Vector2 fontSize = _gameFont.MeasureString("H");
            int textWidth = (int)fontSize.X;
            int textHeight = (int)fontSize.Y;
            while (textHeight % textWidth != 0)
            {
                textHeight++;
            }

            //Reuse the RenderTarget2D (1920x1080) created in LoadContent
            RenderTarget2D renderTarget = _renderTarget;

            //Deprecated: stretched the target separately in X and Y to the display (distorted non-16:9 windows)
            //float scaleFactorX = (float)Globals.DisplayWidth / (float)renderTarget.Width;
            //float scaleFactorY = (float)Globals.DisplayHeight / (float)renderTarget.Height;
            //Matrix scaleMatrix = Matrix.CreateScale(scaleFactorX, scaleFactorY, 1);

            //Draw the whole scene (HUD, console) into the 1920x1080 logical target
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            // Draw background elements
            foreach (String s in _backgroundGameTextures.Keys)
            _spriteBatch.Draw(_backgroundGameTextures[s],
                                new Vector2(0, 0),
                                null,
                                Color.White,
                                0,
                                Vector2.Zero,
                                1,
                                SpriteEffects.None,
                                0);

            // Draw the accumulating string buffer
            _spriteBatch.DrawString(_gameFont, currGameState.userText.ToString().Replace(Environment.NewLine, "").Substring(Math.Max(0, currGameState.userText.ToString().Replace(Environment.NewLine, "").Length - (GameState.CONSOLEWRITELINE_SIZE))), new Vector2((float)GameState.CONSOLEMINWIDTH, (float)GameState.CONSOLEMAXHEIGHT), Color.White);
            for (int i = 0; i < GameState.CONSOLEHISTORY_SIZE; i++)
            {
                if (currGameState.consoleHistory[i] != null)
                    _spriteBatch.DrawString(_gameFont, currGameState.consoleHistory[i].ToString(), new Vector2((float)GameState.CONSOLEMINWIDTH, (float)GameState.CONSOLEMAXHEIGHT - (textHeight*(i+1))), Color.White);
            }

            // Cursor
            Texture2D cursTexture = _pixelTexture;
            Rectangle cursRect = new Rectangle(new Point((currGameState.userText.ToString().Replace(Environment.NewLine, "").Substring(Math.Max(0, currGameState.userText.ToString().Replace(Environment.NewLine, "").Length - (GameState.CONSOLEWRITELINE_SIZE))).Length * textWidth) + GameState.CONSOLEMINWIDTH,
                                                         (textHeight / 4) * 3 + GameState.CONSOLEMAXHEIGHT),
                                                         //OLD METHOD: Chases newlines, typical console buffer
                                                         //((currGameState.userText.ToString().Replace(Environment.NewLine, "").Length % GameState.CONSOLEWRITELINE_SIZE)*textWidth) + GameState.CONSOLEMINWIDTH,
                                                         //((currGameState.userText.ToString().Replace(Environment.NewLine, "").Length / GameState.CONSOLEWRITELINE_SIZE)*textHeight)+(textHeight/4)*3+ GameState.CONSOLEMAXHEIGHT),
                                               new Point(textWidth,
                                                         textHeight / 4));
            _spriteBatch.Draw(cursTexture,cursRect,Color.White);

            _spriteBatch.End();

            //Reset RenderTarget and draw the target to the back buffer, uniformly scaled and letterboxed
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _viewRect = GetLetterboxRect();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(renderTarget, _viewRect, Color.White);
            _spriteBatch.End();


            base.Draw(gameTime);
        }
    }
}
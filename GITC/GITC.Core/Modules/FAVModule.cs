using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GITC.Core
{
    // Field Agent Visualizer: a small side-scroller in the lower-left quadrant. A (left), D (right), Space (jump),
    // S (hide, toggles while grounded). The world keeps running when the module is not focused; the agent just stands still.
    // The camera is a plain horizontal offset that follows the agent (the GOLDEN FORMULA camera is not involved).
    internal class FAVModule : GameModule
    {
        public FavWorld world { get; set; }
        // Level file under Content/Data/ (ModuleConfig.favLevelFile, or DEFAULTLEVELFILE)
        public string levelFile { get; set; }
        // Left edge of the view in world pixels
        public float cameraX { get; set; }

        public const string DEFAULTLEVELFILE = "fav/test_level.json";

        public FAVModule(GITCGame currGame, string levelFile) : base(currGame, ModuleTypes.fav, HudLayout.FAV)
        {
            this.levelFile = string.IsNullOrEmpty(levelFile) ? DEFAULTLEVELFILE : levelFile;
            world = new FavWorld(LoadLevel(this.levelFile));
            FollowAgent();
        }

        // A missing or broken level file still gives a playable flat strip (DataLoader logs the problem)
        public static FavLevelDef LoadLevel(string file)
        {
            FavLevelDef def = DataLoader.Load<FavLevelDef>(file);
            return def != null && def.tiles.Count > 0 ? def : FavLevelDef.Flat(64, 20);
        }

        public override void HandleInput(InputManager input)
        {
            int move = 0;
            if (input.Held(KeyBinds.fav_left)) move--;
            if (input.Held(KeyBinds.fav_right)) move++;
            world.moveDirection = move;
            if (input.Pressed(KeyBinds.fav_jump))
                world.jumpRequested = true;
            if (input.Pressed(KeyBinds.fav_hide))
                world.ToggleHide();
        }

        public override void Update(GameTime gameTime)
        {
            world.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            FollowAgent();
            // Intent only lasts one frame; an unfocused FAV gets none, so the agent stops
            world.moveDirection = 0;
            world.jumpRequested = false;
        }

        // Agent centred horizontally, clamped to the level's ends
        private void FollowAgent()
        {
            float maxX = Math.Max(0f, world.worldWidth - region.Width);
            cameraX = MathHelper.Clamp(world.agentCenter.X - region.Width / 2f, 0f, maxX);
        }

        private static readonly Color terrainColor = new Color(70, 255, 110);
        private static readonly Color agentColor = Color.White;
        private const int MARGIN = 16;

        // World pixels to screen: horizontal camera offset, level bottom-aligned in the quadrant
        private Vector2 ToScreen(float worldX, float worldY)
        {
            return new Vector2(region.X + worldX - cameraX, region.Bottom - world.worldHeight + worldY);
        }

        private Rectangle ScreenRect(float worldX, float worldY, float width, float height)
        {
            Vector2 at = ToScreen(worldX, worldY);
            return new Rectangle((int)Math.Round(at.X), (int)Math.Round(at.Y), (int)Math.Round(width), (int)Math.Round(height));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D px = currGame._content.pixel;
            SpriteFont font = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);
            int ts = world.tileSize;
            spriteBatch.Draw(px, region, Color.Black * 0.85f);

            // Terrain: only the columns in view; a brighter top edge where a solid tile has open air above it
            int firstCol = Math.Max(0, (int)(cameraX / ts));
            int lastCol = Math.Min(world.columns - 1, (int)((cameraX + region.Width) / ts) + 1);
            for (int x = firstCol; x <= lastCol; x++)
                for (int y = 0; y < world.rows; y++)
                {
                    if (!world.IsSolid(x, y))
                        continue;
                    Rectangle tile = ScreenRect(x * ts, y * ts, ts, ts);
                    spriteBatch.Draw(px, tile, terrainColor * 0.25f);
                    if (!world.IsSolid(x, y - 1))
                        spriteBatch.Draw(px, new Rectangle(tile.X, tile.Y, tile.Width, 2), terrainColor);
                }

            // Goal marker: a pole and flag at goalX
            Rectangle pole = ScreenRect(world.level.goalX * ts + ts / 2f - 1f, 0f, 2f, world.worldHeight);
            spriteBatch.Draw(px, pole, Color.Gold * 0.5f);

            // Agent: a box; hidden agents crouch and dim
            Vector2 a = world.agentPosition;
            if (world.isHidden)
                spriteBatch.Draw(px, ScreenRect(a.X, a.Y + world.agentHeight * 0.45f, world.agentWidth, world.agentHeight * 0.55f), agentColor * 0.35f);
            else
            {
                spriteBatch.Draw(px, ScreenRect(a.X, a.Y, world.agentWidth, world.agentHeight), agentColor);
                // Visor on the side the agent faces
                float visorX = world.facing > 0 ? a.X + world.agentWidth * 0.55f : a.X + world.agentWidth * 0.1f;
                spriteBatch.Draw(px, ScreenRect(visorX, a.Y + world.agentHeight * 0.15f, world.agentWidth * 0.35f, 3f), Color.Black);
            }

            // Header
            string status = world.isHidden ? "HIDDEN" : "";
            spriteBatch.Draw(px, new Rectangle(region.X, region.Y, region.Width, MARGIN * 2), Color.Black * 0.6f);
            spriteBatch.DrawString(font, "FIELD AGENT" + (isFocused ? "  [FOCUS]" : ""), new Vector2(region.X + MARGIN, region.Y + MARGIN / 2), terrainColor);
            if (status.Length > 0)
                spriteBatch.DrawString(font, status, new Vector2(region.Right - MARGIN - font.MeasureString(status).X, region.Y + MARGIN / 2), Color.Gold);
        }
    }
}

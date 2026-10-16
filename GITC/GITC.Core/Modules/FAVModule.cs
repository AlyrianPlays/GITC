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
        public int deaths { get; set; }
        // Counts down while the agent is down; the agent respawns at zero
        public float respawnSeconds { get; set; }
        // Text and countdown for the header after a death or a hit
        private string flashText { get; set; }
        private float flashSeconds { get; set; }
        private double elapsedSeconds { get; set; }

        public const string DEFAULTLEVELFILE = "fav/test_level.json";
        public const float RESPAWNSECONDS = 2f;

        public FAVModule(GITCGame currGame, string levelFile) : base(currGame, ModuleTypes.fav, HudLayout.FAV)
        {
            this.levelFile = string.IsNullOrEmpty(levelFile) ? DEFAULTLEVELFILE : levelFile;
            SetWorld(new FavWorld(LoadLevel(this.levelFile)));
        }

        // Swaps in a world (a level load, or a test level) and hooks its callbacks to module events
        public void SetWorld(FavWorld newWorld)
        {
            world = newWorld;
            world.onDeath = AgentDied;
            world.onDamaged = AgentHit;
            world.onBarrierTouched = BarrierTouched;
            respawnSeconds = 0f;
            FollowAgent();
        }

        private void AgentDied(string cause)
        {
            deaths++;
            respawnSeconds = RESPAWNSECONDS;
            ShowFlash("AGENT DOWN (" + cause.ToUpper() + ")");
            Raise(new ModuleEvent(ModuleEventTypes.agent_died, ModuleTypes.fav, 0, cause));
        }

        // A non-lethal enemy spotted the agent: the enemy's damage is an OP penalty
        private void AgentHit(FavEnemy enemy)
        {
            ShowFlash("SPOTTED! -" + enemy.def.damage + " OP");
            Raise(new ModuleEvent(ModuleEventTypes.op_penalty, ModuleTypes.fav, enemy.def.damage, "fav_spotted"));
        }

        // The agent is stuck at a barrier: ask the TIM for its command (the TIM takes focus and shows the countdown)
        private void BarrierTouched(FavBarrierDef barrier)
        {
            Raise(new ModuleEvent(ModuleEventTypes.command_required, ModuleTypes.fav, barrier.seconds, barrier.command));
        }

        // The TIM's answer to a barrier request: satisfied opens it, failed leaves it shut (Mission charges the OPs for command_failed)
        public override void OnEvent(ModuleEvent moduleEvent)
        {
            FavBarrierDef pending = world.level.barriers.Find(b => b.id == world.requestedBarrier);
            if (pending == null || CommandChallenge.Normalize(moduleEvent.tag) != CommandChallenge.Normalize(pending.command))
                return;
            if (moduleEvent.type == ModuleEventTypes.command_satisfied)
            {
                world.OpenBarrier(pending.id);
                ShowFlash("BARRIER OPEN");
            }
            else if (moduleEvent.type == ModuleEventTypes.command_failed)
                world.BarrierFailed(pending.id);
        }

        private void ShowFlash(string text)
        {
            flashText = text;
            flashSeconds = RESPAWNSECONDS;
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
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            elapsedSeconds += dt;
            flashSeconds = Math.Max(0f, flashSeconds - dt);
            if (world.isAgentDown)
            {
                respawnSeconds -= dt;
                if (respawnSeconds <= 0f)
                {
                    world.Respawn();
                    // Spawn protection, so a guard that can see the spawn can't kill the agent again straight away
                    world.graceSeconds = FavWorld.GRACESECONDS;
                }
            }
            world.Update(dt);
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
        private static readonly Color enemyColor = new Color(255, 70, 70);
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

            // Spikes: three teeth per tile, in the lower part of the tile
            float spikeTop = (1f - FavWorld.SPIKEHEIGHT) * ts;
            for (int x = firstCol; x <= lastCol; x++)
                for (int y = 0; y < world.rows; y++)
                    if (world.Tile(x, y) == '^')
                        for (int tooth = 0; tooth < 3; tooth++)
                            for (int step = 0; step < 4; step++)
                            {
                                float w = ts / 3f * (step + 1) / 4f;
                                float h = (ts - spikeTop) / 4f;
                                spriteBatch.Draw(px, ScreenRect(x * ts + tooth * ts / 3f + (ts / 3f - w) / 2f, y * ts + spikeTop + step * h, w, h + 1f), Color.Lerp(Color.White, Color.Red, step / 3f));
                            }

            // Barriers: red flickering bars while shut, gold while the TIM is being asked, a faint frame once open
            foreach (FavBarrierDef b in world.level.barriers)
            {
                bool open = world.openBarriers.Contains(b.id);
                bool asking = world.requestedBarrier == b.id;
                Color bar = open ? terrainColor * 0.15f : asking ? Color.Gold * (0.6f + 0.4f * (float)Math.Sin(elapsedSeconds * 8.0)) : Color.Red * (0.55f + 0.25f * (float)Math.Sin(elapsedSeconds * 20.0 + b.x));
                for (int i = 0; i < 3; i++)
                    spriteBatch.Draw(px, ScreenRect(b.x * ts + ts * (0.2f + 0.3f * i), b.y * ts, 3f, b.height * ts), bar);
                if (!open)
                    for (int row = 0; row < b.height; row += 2)
                        spriteBatch.Draw(px, ScreenRect(b.x * ts, (b.y + row) * ts + ts / 2f, ts, 2f), bar * 0.6f);
            }

            // Goal marker: a pole and flag at goalX
            Rectangle pole = ScreenRect(world.level.goalX * ts + ts / 2f - 1f, 0f, 2f, world.worldHeight);
            spriteBatch.Draw(px, pole, Color.Gold * 0.5f);

            // Enemies: vision cone first (stops at walls, warms from yellow to red with the alert), then the body
            foreach (FavEnemy enemy in world.enemies)
                DrawEnemy(spriteBatch, px, font, enemy);

            // Agent: a box; hidden agents crouch and dim; a downed agent lies flat and blinks
            Vector2 a = world.agentPosition;
            if (world.isAgentDown)
            {
                if ((int)(elapsedSeconds * 6) % 2 == 0)
                    spriteBatch.Draw(px, ScreenRect(a.X - world.agentHeight * 0.3f, a.Y + world.agentHeight - world.agentWidth * 0.6f, world.agentHeight, world.agentWidth * 0.6f), Color.Red);
            }
            else if (world.isHidden)
                spriteBatch.Draw(px, ScreenRect(a.X, a.Y + world.agentHeight * 0.45f, world.agentWidth, world.agentHeight * 0.55f), agentColor * 0.35f);
            else
            {
                // Flickers during the grace period after a hit
                if (world.graceSeconds <= 0f || (int)(elapsedSeconds * 10) % 2 == 0)
                    spriteBatch.Draw(px, ScreenRect(a.X, a.Y, world.agentWidth, world.agentHeight), agentColor);
                // Visor on the side the agent faces
                float visorX = world.facing > 0 ? a.X + world.agentWidth * 0.55f : a.X + world.agentWidth * 0.1f;
                spriteBatch.Draw(px, ScreenRect(visorX, a.Y + world.agentHeight * 0.15f, world.agentWidth * 0.35f, 3f), Color.Black);
            }

            // Header
            FavBarrierDef waiting = world.level.barriers.Find(b => b.id == world.requestedBarrier);
            string status = flashSeconds > 0f ? flashText
                          : waiting != null ? "BARRIER: TYPE '" + waiting.command.ToUpper() + "'"
                          : world.isHidden ? "HIDDEN" : "";
            spriteBatch.Draw(px, new Rectangle(region.X, region.Y, region.Width, MARGIN * 2), Color.Black * 0.6f);
            spriteBatch.DrawString(font, "FIELD AGENT" + (isFocused ? "  [FOCUS]" : ""), new Vector2(region.X + MARGIN, region.Y + MARGIN / 2), terrainColor);
            if (status.Length > 0)
                spriteBatch.DrawString(font, status, new Vector2(region.Right - MARGIN - font.MeasureString(status).X, region.Y + MARGIN / 2), flashSeconds > 0f ? Color.Red : Color.Gold);
        }

        private void DrawEnemy(SpriteBatch spriteBatch, Texture2D px, SpriteFont font, FavEnemy enemy)
        {
            Vector2 eye = enemy.eye;
            Color cone = Color.Lerp(Color.Yellow, Color.Red, enemy.alert) * (0.10f + 0.15f * enemy.alert);
            // One 4 px strip per step ahead; the cone ends at the first wall at eye height
            const int STRIP = 4;
            for (float ahead = 0f; ahead < enemy.sightPixels; ahead += STRIP)
            {
                float x = eye.X + enemy.facing * ahead;
                if (world.IsSolid((int)Math.Floor(x / world.tileSize), (int)Math.Floor(eye.Y / world.tileSize)))
                    break;
                float half = enemy.ConeHalfHeight(ahead);
                // Clip the strip to the open air in its column (no cone drawn into the floor or ceiling)
                int col = (int)Math.Floor(x / world.tileSize);
                int eyeRow = (int)Math.Floor(eye.Y / world.tileSize);
                float top = eye.Y - half, bottom = eye.Y + half;
                for (int row = eyeRow + 1; row * world.tileSize < bottom; row++)
                    if (world.IsSolid(col, row)) { bottom = row * world.tileSize; break; }
                for (int row = eyeRow - 1; (row + 1) * world.tileSize > top; row--)
                    if (world.IsSolid(col, row)) { top = (row + 1) * world.tileSize; break; }
                float stripX = enemy.facing > 0 ? x : x - STRIP;
                spriteBatch.Draw(px, ScreenRect(stripX, top, STRIP, bottom - top), cone);
            }

            Vector2 p = enemy.position;
            spriteBatch.Draw(px, ScreenRect(p.X, p.Y, enemy.width, enemy.height), enemyColor);
            float eyeX = enemy.facing > 0 ? p.X + enemy.width * 0.6f : p.X + enemy.width * 0.1f;
            spriteBatch.Draw(px, ScreenRect(eyeX, p.Y + enemy.height * 0.15f, enemy.width * 0.3f, 3f), Color.Yellow);

            // ? while it is growing suspicious, ! when it is about to spot the agent
            if (enemy.alert > 0f)
            {
                string mark = enemy.alert >= 0.75f ? "!" : "?";
                Vector2 at = ToScreen(p.X + enemy.width / 2f, p.Y);
                spriteBatch.DrawString(font, mark, new Vector2(at.X - font.MeasureString(mark).X / 2f, at.Y - font.LineSpacing - 2), Color.Lerp(Color.Yellow, Color.Red, enemy.alert));
            }
        }
    }
}

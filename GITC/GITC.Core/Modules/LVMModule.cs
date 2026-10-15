using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // Lidar Validation Module: stands by until a maze_required event arrives (amount = seconds, 0 = config default), then the player
    // walks a perfect maze from the left edge to the exit on the right with WASD, one cell per press, against a countdown.
    // Stepping onto a cell that is not on the solution path is a wrong turn (maze_failed + op_penalty, back to the start).
    // Reaching the exit raises maze_completed and the module goes back to standing by; running out of time raises maze_failed
    // and op_penalty. When and how often requests arrive (and how tight the time is) is up to the mission, not this module.
    internal class LVMModule : GameModule
    {
        public MazeConfig config { get; set; }
        public MazeGenerator maze { get; set; }
        // 1 = easiest; set by the mission's lvm config (bigger maze)
        public int difficulty { get; set; }
        public Point playerCell { get; set; }
        // Cells the player has stood on in this attempt, start first
        public List<Point> path { get; set; }
        public int mazesCompleted { get; set; }
        public int mazesFailed { get; set; }
        // True from a maze_required request until the maze is solved or the time runs out
        public bool isChallengeActive { get; set; }
        public float timeRemaining { get; set; }
        public int wrongTurns { get; set; }

        // Attempts made at the current difficulty, so a fixed seed still gives a different maze after each regeneration
        private int attempt { get; set; }
        // Colour wash after a wrong turn / solved maze, counting down from FLASHSECONDS
        private float flashSeconds { get; set; }
        private bool flashGood { get; set; }
        private double elapsedSeconds { get; set; }

        public LVMModule(GITCGame currGame, MazeConfig config, int difficulty = 1) : base(currGame, ModuleTypes.lvm, HudLayout.LVM)
        {
            this.config = config ?? new MazeConfig();
            this.difficulty = Math.Max(1, difficulty);
            path = new List<Point>();
            isChallengeActive = false;
            NewMaze();
        }

        // A maze request: seconds <= 0 uses the config's time limit. Ignored while a maze is already being solved.
        public void StartChallenge(int seconds)
        {
            if (isChallengeActive)
                return;
            attempt++;
            NewMaze();
            timeRemaining = seconds > 0 ? seconds : config.timeLimitSeconds;
            isChallengeActive = true;
            // Same rule as the TIM: a request pulls focus to the module
            manager?.SetFocus(ModuleTypes.lvm);
        }

        public override void OnEvent(ModuleEvent moduleEvent)
        {
            if (moduleEvent.type == ModuleEventTypes.maze_required)
                StartChallenge(moduleEvent.amount);
        }

        // Builds a maze for the current difficulty (config width/height win over the difficulty size when set)
        public void NewMaze()
        {
            int side = MazeGenerator.SizeFor(difficulty);
            int w = config.width > 0 ? config.width : side;
            int h = config.height > 0 ? config.height : side;
            int seed = config.seed == 0 ? 0 : config.seed + difficulty * 1000 + attempt;
            maze = new MazeGenerator(w, h, seed);
            ResetToStart();
        }

        public void ResetToStart()
        {
            playerCell = maze.start;
            path.Clear();
            path.Add(playerCell);
        }

        public override void Update(GameTime gameTime)
        {
            elapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;
            if (flashSeconds > 0f)
                flashSeconds = Math.Max(0f, flashSeconds - (float)gameTime.ElapsedGameTime.TotalSeconds);
            if (!isChallengeActive)
                return;
            timeRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeRemaining <= 0f)
                TimedOut();
        }

        public override void HandleInput(InputManager input)
        {
            if (!isChallengeActive)
                return;
            if (input.Pressed(KeyBinds.lvm_up)) TryMove(MazeGenerator.NORTH);
            else if (input.Pressed(KeyBinds.lvm_down)) TryMove(MazeGenerator.SOUTH);
            else if (input.Pressed(KeyBinds.lvm_left)) TryMove(MazeGenerator.WEST);
            else if (input.Pressed(KeyBinds.lvm_right)) TryMove(MazeGenerator.EAST);
        }

        // One step in a direction. A wall blocks the move with no penalty; an open passage onto a cell off the solution is a wrong turn.
        public void TryMove(int direction)
        {
            if (!maze.IsOpen(playerCell, direction))
                return;
            Point next = new Point(playerCell.X + MazeGenerator.OffsetX(direction), playerCell.Y + MazeGenerator.OffsetY(direction));
            playerCell = next;
            path.Add(next);

            if (!maze.solutionSet.Contains(next))
            {
                WrongTurn();
                return;
            }
            if (next == maze.exit)
                Completed();
        }

        private void WrongTurn()
        {
            wrongTurns++;
            flashSeconds = FLASHSECONDS;
            flashGood = false;
            Raise(new ModuleEvent(ModuleEventTypes.maze_failed, ModuleTypes.lvm));
            Raise(new ModuleEvent(ModuleEventTypes.op_penalty, ModuleTypes.lvm, config.wrongTurnPenalty, "wrong_turn"));
            if (config.regenerateOnFail)
            {
                attempt++;
                NewMaze();
            }
            else
                ResetToStart();
        }

        private void Completed()
        {
            mazesCompleted++;
            flashSeconds = FLASHSECONDS;
            flashGood = true;
            isChallengeActive = false;
            Raise(new ModuleEvent(ModuleEventTypes.maze_completed, ModuleTypes.lvm, difficulty));
        }

        private void TimedOut()
        {
            mazesFailed++;
            flashSeconds = FLASHSECONDS;
            flashGood = false;
            isChallengeActive = false;
            timeRemaining = 0f;
            Raise(new ModuleEvent(ModuleEventTypes.maze_failed, ModuleTypes.lvm, 0, "timeout"));
            Raise(new ModuleEvent(ModuleEventTypes.op_penalty, ModuleTypes.lvm, config.timeoutPenalty, "maze_timeout"));
        }

        // Wall and trail colour; the player and exit stand out against it
        private static readonly Color wallColor = new Color(70, 255, 110);
        private const int WALL = 2;
        private const int MARGIN = 16;
        private const int HEADERHEIGHT = 32;
        private const float FLASHSECONDS = 0.4f;

        // Cell brightness 0..1: everything is lit unless revealRadius is set, then cells fade with distance from the player
        private float Light(Point cell)
        {
            if (config.revealRadius <= 0)
                return 1f;
            float distance = Vector2.Distance(cell.ToVector2(), playerCell.ToVector2());
            if (distance > config.revealRadius)
                return 0f;
            return MathHelper.Clamp(1f - distance / (config.revealRadius + 1f), 0.1f, 1f);
        }

        private float WallLight(Point a, Point b)
        {
            return maze.InBounds(b) ? Math.Max(Light(a), Light(b)) : Light(a);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D px = currGame._content.pixel;
            SpriteFont font = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);
            spriteBatch.Draw(px, region, Color.Black * 0.85f);

            // Standing by: no maze until the mission asks for one
            if (!isChallengeActive)
            {
                spriteBatch.DrawString(font, "LIDAR  STANDING BY" + (isFocused ? "  [FOCUS]" : ""),
                                       new Vector2(region.X + MARGIN, region.Y + MARGIN / 2), wallColor * 0.6f);
                spriteBatch.DrawString(font, "NO VALIDATION REQUESTED", new Vector2(region.X + MARGIN, region.Center.Y), wallColor * 0.35f);
                if (flashSeconds > 0f)
                    spriteBatch.Draw(px, region, (flashGood ? Color.Lime : Color.Red) * (0.35f * flashSeconds / FLASHSECONDS));
                return;
            }

            // Header: size and the countdown (flashes red in the last 3 s)
            spriteBatch.DrawString(font, "LIDAR  " + maze.width + "x" + maze.height + (isFocused ? "  [FOCUS]" : ""),
                                   new Vector2(region.X + MARGIN, region.Y + MARGIN / 2), wallColor);
            bool urgent = timeRemaining <= 3f && (int)(elapsedSeconds * 4) % 2 == 0;
            string clock = Math.Ceiling(timeRemaining) + "s";
            spriteBatch.DrawString(font, clock, new Vector2(region.Right - MARGIN - font.MeasureString(clock).X, region.Y + MARGIN / 2), urgent ? Color.Red : Color.Gold);

            // Fit the maze into the area under the header, centred
            int areaW = region.Width - 2 * MARGIN;
            int areaH = region.Height - HEADERHEIGHT - MARGIN;
            int cell = Math.Max(4, Math.Min(areaW / maze.width, areaH / maze.height));
            int originX = region.X + (region.Width - cell * maze.width) / 2;
            int originY = region.Y + HEADERHEIGHT + (areaH - cell * maze.height) / 2;

            // Trail, then the exit and start markers
            foreach (Point p in path)
            {
                float light = Light(p);
                if (light > 0f)
                    spriteBatch.Draw(px, new Rectangle(originX + p.X * cell + WALL, originY + p.Y * cell + WALL, cell - WALL, cell - WALL), wallColor * 0.2f * light);
            }
            float pulse = 0.6f + 0.4f * (float)Math.Sin(elapsedSeconds * 4.0);
            spriteBatch.Draw(px, new Rectangle(originX + maze.exit.X * cell + WALL, originY + maze.exit.Y * cell + WALL, cell - WALL, cell - WALL), Color.Gold * pulse);
            Rectangle startMark = new Rectangle(originX + maze.start.X * cell + WALL, originY + maze.start.Y * cell + WALL, cell - WALL, cell - WALL);
            spriteBatch.Draw(px, startMark, Color.Cyan * 0.3f);

            // Walls: each cell draws its north and west side, the last row/column also draw south/east
            for (int x = 0; x < maze.width; x++)
                for (int y = 0; y < maze.height; y++)
                {
                    Point at = new Point(x, y);
                    int left = originX + x * cell;
                    int top = originY + y * cell;
                    if (!maze.IsOpen(at, MazeGenerator.NORTH))
                        DrawWall(spriteBatch, px, new Rectangle(left, top, cell + WALL, WALL), WallLight(at, new Point(x, y - 1)));
                    if (!maze.IsOpen(at, MazeGenerator.WEST) && at != maze.start)
                        DrawWall(spriteBatch, px, new Rectangle(left, top, WALL, cell + WALL), WallLight(at, new Point(x - 1, y)));
                    if (y == maze.height - 1)
                        DrawWall(spriteBatch, px, new Rectangle(left, top + cell, cell + WALL, WALL), Light(at));
                    if (x == maze.width - 1 && at != maze.exit)
                        DrawWall(spriteBatch, px, new Rectangle(left + cell, top, WALL, cell + WALL), Light(at));
                }

            // Player dot
            int dot = Math.Max(2, cell / 2);
            spriteBatch.Draw(px, new Rectangle(originX + playerCell.X * cell + (cell + WALL - dot) / 2, originY + playerCell.Y * cell + (cell + WALL - dot) / 2, dot, dot), Color.White);

            // Brief colour wash after a wrong turn (red) or a solved maze (green)
            if (flashSeconds > 0f)
                spriteBatch.Draw(px, region, (flashGood ? Color.Lime : Color.Red) * (0.35f * flashSeconds / FLASHSECONDS));
        }

        private void DrawWall(SpriteBatch spriteBatch, Texture2D px, Rectangle wall, float light)
        {
            if (light > 0f)
                spriteBatch.Draw(px, wall, wallColor * light);
        }
    }
}

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // The Field Agent's side-scrolling world: the tile grid of a FavLevelDef and the agent's AABB physics.
    // Positions are world pixels (tile coordinates x tileSize); FAVModule draws it with a horizontal camera offset.
    // Gravity, speeds and the agent's size are in tiles, so a level with a different tileSize plays the same.
    internal class FavWorld
    {
        public FavLevelDef level { get; set; }
        public int tileSize { get; set; }
        public int columns { get; set; }
        public int rows { get; set; }
        // Agent box, top-left corner in world pixels
        public Vector2 agentPosition { get; set; }
        public Vector2 agentVelocity { get; set; }
        public bool isGrounded { get; set; }
        // Hidden agents can't move; toggled with S while grounded
        public bool isHidden { get; set; }
        // -1 left, 1 right (the way the agent last moved)
        public int facing { get; set; }
        // Input intent, set by FAVModule.HandleInput before each Update and cleared after it
        public int moveDirection { get; set; }
        public bool jumpRequested { get; set; }
        public List<FavEnemy> enemies { get; set; }
        // Dead agents don't move and can't be seen; FAVModule respawns them
        public bool isAgentDown { get; set; }
        // Seconds left in which no enemy can spot the agent (after a hit, or after a respawn)
        public float graceSeconds { get; set; }
        // Seconds left of a knockback, during which the push replaces the player's steering
        public float knockbackSeconds { get; set; }
        // Called once when the agent dies (cause: spotted, spikes, pit ...) and when a non-lethal enemy spots it
        public Action<string> onDeath { get; set; }
        public Action<FavEnemy> onDamaged { get; set; }

        // Tiles per second (squared for gravity). A jump clears about 3 tiles up and 5 across.
        public const float GRAVITY = 60f;
        public const float JUMPSPEED = 19.5f;
        public const float RUNSPEED = 7f;
        public const float MAXFALLSPEED = 24f;
        // Agent box in tiles
        public const float AGENTWIDTH = 0.6f;
        public const float AGENTHEIGHT = 1.4f;
        // After a non-lethal spot: seconds before the agent can be spotted again, and how far it is pushed back (tiles/s)
        public const float GRACESECONDS = 2f;
        public const float KNOCKBACKSPEED = 9f;
        public const float KNOCKBACKTIME = 0.25f;
        // Physics runs in fixed slices so a long frame (or a simulated second) can't tunnel through a tile
        private const float STEP = 1f / 120f;

        public FavWorld(FavLevelDef level)
        {
            this.level = level;
            tileSize = Math.Max(4, level.tileSize);
            rows = level.tiles.Count;
            columns = 0;
            foreach (string row in level.tiles)
                columns = Math.Max(columns, row.Length);
            enemies = new List<FavEnemy>();
            Respawn();
        }

        public float worldWidth { get { return columns * tileSize; } }
        public float worldHeight { get { return rows * tileSize; } }
        public float agentWidth { get { return AGENTWIDTH * tileSize; } }
        public float agentHeight { get { return AGENTHEIGHT * tileSize; } }
        public Vector2 agentCenter { get { return agentPosition + new Vector2(agentWidth / 2f, agentHeight / 2f); } }

        // Agent standing on the spawn tile (spawnY is the row the agent stands in, so its feet are on the row below)
        public void Respawn()
        {
            agentPosition = new Vector2(level.spawnX * tileSize + (tileSize - agentWidth) / 2f, (level.spawnY + 1) * tileSize - agentHeight);
            agentVelocity = Vector2.Zero;
            isGrounded = false;
            isHidden = false;
            isAgentDown = false;
            graceSeconds = 0f;
            knockbackSeconds = 0f;
            facing = 1;
            // Enemies go back to their starting posts
            enemies.Clear();
            foreach (FavEnemyDef e in level.enemies)
                enemies.Add(new FavEnemy(e, tileSize));
        }

        // Character at a tile; the sides of the level are walls, above and below are open air
        public char Tile(int x, int y)
        {
            if (x < 0 || x >= columns)
                return '#';
            if (y < 0 || y >= rows || x >= level.tiles[y].Length)
                return '.';
            return level.tiles[y][x];
        }

        public bool IsSolid(int x, int y)
        {
            return Tile(x, y) == '#';
        }

        // Walks the segment in quarter-tile steps; any solid tile on the way blocks the view
        public bool LineOfSight(Vector2 from, Vector2 to)
        {
            float distance = Vector2.Distance(from, to);
            int steps = Math.Max(1, (int)(distance / (tileSize / 4f)));
            for (int i = 1; i < steps; i++)
            {
                Vector2 at = Vector2.Lerp(from, to, i / (float)steps);
                if (IsSolid(Floor(at.X), Floor(at.Y)))
                    return false;
            }
            return true;
        }

        public void KillAgent(string cause)
        {
            if (isAgentDown)
                return;
            isAgentDown = true;
            isHidden = false;
            agentVelocity = Vector2.Zero;
            onDeath?.Invoke(cause);
        }

        public void ToggleHide()
        {
            if (isHidden)
                isHidden = false;
            else if (isGrounded)
                isHidden = true;
        }

        public void Update(float deltaSeconds)
        {
            float remaining = Math.Min(deltaSeconds, 1f);
            while (remaining > 0f)
            {
                float dt = Math.Min(STEP, remaining);
                Step(dt);
                remaining -= dt;
            }
        }

        private void Step(float dt)
        {
            foreach (FavEnemy enemy in enemies)
                enemy.Update(dt, this);
            if (isAgentDown)
                return;
            graceSeconds = Math.Max(0f, graceSeconds - dt);
            knockbackSeconds = Math.Max(0f, knockbackSeconds - dt);
            CheckSpotted();
            if (isAgentDown)
                return;

            Vector2 v = agentVelocity;
            int move = isHidden ? 0 : Math.Sign(moveDirection);
            // While being knocked back the push replaces the player's steering
            if (knockbackSeconds > 0f)
                move = 0;
            else
                v.X = move * RUNSPEED * tileSize;
            if (move != 0)
                facing = move;
            if (jumpRequested && isGrounded && !isHidden)
            {
                v.Y = -JUMPSPEED * tileSize;
                isGrounded = false;
                // One press, one jump
                jumpRequested = false;
            }
            v.Y = Math.Min(v.Y + GRAVITY * tileSize * dt, MAXFALLSPEED * tileSize);
            agentVelocity = v;

            MoveX(v.X * dt);
            MoveY(v.Y * dt);

            // Fell out of the bottom of the level: back to the spawn (T-8.3 turns this into a death)
            if (agentPosition.Y > worldHeight)
                Respawn();
        }

        // An enemy with a full alert spots the agent: lethal ones kill it, the others damage it, push it away and give it a grace period
        private void CheckSpotted()
        {
            foreach (FavEnemy enemy in enemies)
            {
                if (enemy.alert < 1f || graceSeconds > 0f)
                    continue;
                if (enemy.def.lethal)
                {
                    KillAgent("spotted");
                    return;
                }
                enemy.alert = 0f;
                graceSeconds = GRACESECONDS;
                knockbackSeconds = KNOCKBACKTIME;
                int away = agentCenter.X < enemy.position.X + enemy.width / 2f ? -1 : 1;
                agentVelocity = new Vector2(away * KNOCKBACKSPEED * tileSize, -JUMPSPEED * 0.4f * tileSize);
                isGrounded = false;
                onDamaged?.Invoke(enemy);
            }
        }

        // Moves horizontally and stops at the first solid column in the way (moves are always under a tile per slice)
        private void MoveX(float dx)
        {
            if (dx == 0f)
                return;
            Vector2 p = agentPosition;
            p.X += dx;
            int top = Floor(p.Y);
            int bottom = Floor(p.Y + agentHeight - 0.01f);
            int col = dx > 0f ? Floor(p.X + agentWidth - 0.01f) : Floor(p.X);
            for (int y = top; y <= bottom; y++)
                if (IsSolid(col, y))
                {
                    p.X = dx > 0f ? col * tileSize - agentWidth : (col + 1) * tileSize;
                    agentVelocity = new Vector2(0f, agentVelocity.Y);
                    break;
                }
            agentPosition = p;
        }

        private void MoveY(float dy)
        {
            Vector2 p = agentPosition;
            p.Y += dy;
            isGrounded = false;
            int left = Floor(p.X);
            int right = Floor(p.X + agentWidth - 0.01f);
            int row = dy > 0f ? Floor(p.Y + agentHeight - 0.01f) : Floor(p.Y);
            for (int x = left; x <= right; x++)
                if (IsSolid(x, row))
                {
                    if (dy > 0f)
                    {
                        p.Y = row * tileSize - agentHeight;
                        isGrounded = true;
                    }
                    else
                        p.Y = (row + 1) * tileSize;
                    agentVelocity = new Vector2(agentVelocity.X, 0f);
                    break;
                }
            agentPosition = p;
        }

        // World pixels to tile index (negative values round down, so -0.5 is tile -1)
        private int Floor(float worldPixels)
        {
            return (int)Math.Floor(worldPixels / tileSize);
        }
    }
}

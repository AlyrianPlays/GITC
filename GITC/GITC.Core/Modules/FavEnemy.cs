using Microsoft.Xna.Framework;
using System;

namespace GITC.Core
{
    // A patrolling enemy in the FAV world. It walks between its patrol points, pausing to turn at each end, and watches
    // a cone ahead of it. While the agent is in the cone (and in line of sight, and not hidden) its alert fills over
    // detectSeconds; a full alert, or bumping into a visible agent, means the agent is spotted (FavWorld decides what happens).
    internal class FavEnemy
    {
        public FavEnemyDef def { get; set; }
        // Box top-left in world pixels
        public Vector2 position { get; set; }
        public int facing { get; set; }
        // 0 = unaware, 1 = spotted
        public float alert { get; set; }
        // True while the agent is inside the cone this frame (for drawing)
        public bool seesAgent { get; set; }
        public int tileSize { get; set; }

        // Enemy box in tiles
        public const float WIDTH = 0.8f;
        public const float HEIGHT = 1.5f;
        // The cone widens by this many pixels up/down per pixel ahead, starting from half a tile at the eye
        public const float CONESLOPE = 0.35f;
        // Seconds an enemy stands at a patrol end before turning
        public const float TURNPAUSE = 0.6f;

        private float pauseSeconds { get; set; }

        public FavEnemy(FavEnemyDef def, int tileSize)
        {
            this.def = def;
            this.tileSize = tileSize;
            facing = def.facing >= 0 ? 1 : -1;
            position = new Vector2(def.x * tileSize + (tileSize - width) / 2f, (def.y + 1) * tileSize - height);
            alert = 0f;
        }

        public float width { get { return WIDTH * tileSize; } }
        public float height { get { return HEIGHT * tileSize; } }
        // Eye height is near the top of the box, on the side it faces
        public Vector2 eye { get { return new Vector2(position.X + width / 2f + facing * width * 0.3f, position.Y + height * 0.2f); } }
        public float sightPixels { get { return def.sightRange * tileSize; } }

        public void Update(float dt, FavWorld world)
        {
            seesAgent = Sees(world);
            bool touching = !world.isHidden && Overlaps(world.agentPosition, world.agentWidth, world.agentHeight);

            if (touching)
                alert = 1f;
            else if (seesAgent)
                alert = Math.Min(1f, alert + dt / Math.Max(0.01f, def.detectSeconds));
            else
                alert = Math.Max(0f, alert - dt / Math.Max(0.01f, def.detectSeconds * 2f));

            // A suspicious enemy stops and stares; otherwise it keeps patrolling
            if (alert <= 0f)
                Patrol(dt);
        }

        private void Patrol(float dt)
        {
            float from = Math.Min(def.patrolFromX, def.patrolToX) * tileSize + (tileSize - width) / 2f;
            float to = Math.Max(def.patrolFromX, def.patrolToX) * tileSize + (tileSize - width) / 2f;
            if (to - from < 1f || def.speed <= 0f)
                return;
            if (pauseSeconds > 0f)
            {
                pauseSeconds -= dt;
                if (pauseSeconds <= 0f)
                    facing = -facing;
                return;
            }
            float x = position.X + facing * def.speed * tileSize * dt;
            if ((facing > 0 && x >= to) || (facing < 0 && x <= from))
            {
                x = facing > 0 ? to : from;
                pauseSeconds = TURNPAUSE;
            }
            position = new Vector2(x, position.Y);
        }

        // Checks: agent not hidden, ahead of the eye (a sliver behind counts), within range, inside the widening cone,
        // and no solid tile between the eye and the agent's centre
        public bool Sees(FavWorld world)
        {
            if (world.isHidden || world.isAgentDown)
                return false;
            Vector2 target = world.agentCenter;
            float ahead = (target.X - eye.X) * facing;
            if (ahead < -tileSize * 0.2f || ahead > sightPixels)
                return false;
            if (Math.Abs(target.Y - eye.Y) > ConeHalfHeight(Math.Max(0f, ahead)))
                return false;
            return world.LineOfSight(eye, target);
        }

        // Half the cone's height at a distance ahead of the eye
        public float ConeHalfHeight(float ahead)
        {
            return tileSize * 0.5f + ahead * CONESLOPE;
        }

        public bool Overlaps(Vector2 otherPosition, float otherWidth, float otherHeight)
        {
            return otherPosition.X < position.X + width && otherPosition.X + otherWidth > position.X
                && otherPosition.Y < position.Y + height && otherPosition.Y + otherHeight > position.Y;
        }
    }
}

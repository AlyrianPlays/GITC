namespace GITC.Core
{
    // One patrolling enemy in a FAV level. Positions are tile coordinates; the enemy walks between
    // patrolFromX and patrolToX (equal values make a stationary guard that keeps facing `facing`).
    internal class FavEnemyDef
    {
        public string id { get; set; }
        // Start column, and the row it stands in (its feet are on the row below)
        public int x { get; set; }
        public int y { get; set; }
        public int patrolFromX { get; set; }
        public int patrolToX { get; set; }
        // Tiles per second
        public float speed { get; set; }
        // How far ahead it can see, in tiles
        public float sightRange { get; set; }
        // Seconds the agent must stay in view before it is spotted
        public float detectSeconds { get; set; }
        // Spotted: true = the agent dies, false = damage (an OP penalty) and the agent is pushed back
        public bool lethal { get; set; }
        public int damage { get; set; }
        // -1 left, 1 right at the start
        public int facing { get; set; }

        public FavEnemyDef()
        {
            speed = 2f;
            sightRange = 6f;
            detectSeconds = 0.6f;
            lethal = true;
            damage = 10;
            facing = 1;
        }
    }
}

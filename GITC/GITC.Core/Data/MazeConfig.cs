namespace GITC.Core
{
    // Lidar Validation Module maze settings
    internal class MazeConfig
    {
        // Grid size in cells; 0 = derive from difficulty
        public int width { get; set; }
        public int height { get; set; }
        // 0 = random each time
        public int seed { get; set; }
        // Cells revealed around the player (0 = whole maze visible)
        public int revealRadius { get; set; }
        // After a wrong turn: generate a new maze (true) or reuse the same one (false)
        public bool regenerateOnFail { get; set; }

        public MazeConfig()
        {
            regenerateOnFail = false;
        }
    }
}

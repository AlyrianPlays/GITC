using System.Collections.Generic;

namespace GITC.Core
{
    // One Field Agent Visualizer level segment. Enemies and barriers are added in T-8.2 / T-8.3.
    internal class FavLevelDef
    {
        public string id { get; set; }
        // Pixel size of one tile in the FAV viewport
        public int tileSize { get; set; }
        // Tile rows, top to bottom; one character per tile ('.' empty, '#' solid)
        public List<string> tiles { get; set; }
        // Spawn and goal positions in tile coordinates
        public int spawnX { get; set; }
        public int spawnY { get; set; }
        public int goalX { get; set; }

        public FavLevelDef()
        {
            tileSize = 16;
            tiles = new List<string>();
        }
    }
}

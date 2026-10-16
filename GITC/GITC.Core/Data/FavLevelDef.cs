using System.Collections.Generic;

namespace GITC.Core
{
    // One Field Agent Visualizer level segment.
    internal class FavLevelDef
    {
        public string id { get; set; }
        // Pixel size of one tile in the FAV viewport
        public int tileSize { get; set; }
        // Tile rows, top to bottom; one character per tile ('.' empty, '#' solid, '^' spikes)
        public List<string> tiles { get; set; }
        // Spawn and goal positions in tile coordinates
        public int spawnX { get; set; }
        public int spawnY { get; set; }
        public int goalX { get; set; }
        public List<FavEnemyDef> enemies { get; set; }
        public List<FavBarrierDef> barriers { get; set; }

        public FavLevelDef()
        {
            tileSize = 16;
            tiles = new List<string>();
            enemies = new List<FavEnemyDef>();
            barriers = new List<FavBarrierDef>();
        }

        // Fallback when no level file loads: open air over two rows of ground, spawn at the left, goal at the right
        public static FavLevelDef Flat(int columns, int rows)
        {
            FavLevelDef def = new FavLevelDef() { id = "flat", tileSize = 24, spawnX = 2, spawnY = rows - 3, goalX = columns - 3 };
            for (int y = 0; y < rows; y++)
                def.tiles.Add(new string(y >= rows - 2 ? '#' : '.', columns));
            return def;
        }
    }
}

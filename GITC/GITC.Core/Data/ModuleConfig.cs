namespace GITC.Core
{
    // Settings for one module inside a mission. Only the block matching `module` is read; the others stay null.
    internal class ModuleConfig
    {
        // Module name: tim, fav, lvm, vm, him
        public string module { get; set; }
        public bool enabled { get; set; }
        // 1 = easiest
        public int difficulty { get; set; }
        public MazeConfig maze { get; set; }
        // FAV level file relative to Content/Data/ (a FavLevelDef json)
        public string favLevelFile { get; set; }
        public VitalsConfig vitals { get; set; }
        public HimConfig him { get; set; }

        public ModuleConfig()
        {
            enabled = true;
            difficulty = 1;
        }
    }
}

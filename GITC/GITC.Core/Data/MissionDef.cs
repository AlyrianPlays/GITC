using System.Collections.Generic;

namespace GITC.Core
{
    // One mission (level). Finalized by the mission director in T-10.1.
    internal class MissionDef
    {
        // Unique across all companies, e.g. "swiftail_01"
        public string id { get; set; }
        public string companyId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int durationSeconds { get; set; }
        public int startingOps { get; set; }
        // Available from the start without completing another mission
        public bool unlockedByDefault { get; set; }
        // Mission ids unlocked when this one is won
        public List<string> unlocks { get; set; }
        // Module names active in this mission (tim, fav, lvm, vm, him); becomes a ModuleTypes enum in T-3.1
        public List<string> activeModules { get; set; }
        // Per-module settings
        public List<ModuleConfig> modules { get; set; }
        // Scripted events, fired by the director
        public List<MissionEvent> events { get; set; }
        // OP penalty overrides by name (e.g. "command_failed", "maze_failed"); missing = default
        public Dictionary<string, int> penalties { get; set; }
        // Scene id played after winning, empty for none
        public string winSceneId { get; set; }

        public MissionDef()
        {
            durationSeconds = 120;
            startingOps = 100;
            unlocks = new List<string>();
            activeModules = new List<string>();
            modules = new List<ModuleConfig>();
            events = new List<MissionEvent>();
            penalties = new Dictionary<string, int>();
        }
    }
}

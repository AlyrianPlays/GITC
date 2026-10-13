using System.Collections.Generic;

namespace GITC.Core
{
    // Everything persisted between sessions. Serialized as JSON by SaveManager.
    internal class SaveData
    {
        public const int CURRENTVERSION = 1;

        // Bump when the shape changes; SaveManager uses it for migration
        public int version { get; set; }
        public List<string> completedMissions { get; set; }
        public List<string> unlockedMissions { get; set; }
        // Best Operator Points remaining per mission id
        public Dictionary<string, int> bestScores { get; set; }
        // Story path flags by name ("genesis", "lamentation", "revelation"); rules are applied in T-11.4
        public Dictionary<string, bool> pathFlags { get; set; }
        // Story/meta flags set by scenes and commands
        public List<string> flags { get; set; }
        // Hidden commands/files the player has found
        public List<string> discoveredSecrets { get; set; }
        // Same enum-indexed lists as UserInfo (BSettings / ISettings)
        public List<bool> bSettings { get; set; }
        public List<int> iSettings { get; set; }
        // Culture code, e.g. "en-US"; empty = default
        public string language { get; set; }

        public SaveData()
        {
            version = CURRENTVERSION;
            completedMissions = new List<string>();
            unlockedMissions = new List<string>();
            bestScores = new Dictionary<string, int>();
            pathFlags = new Dictionary<string, bool>();
            flags = new List<string>();
            discoveredSecrets = new List<string>();
            bSettings = new List<bool>();
            iSettings = new List<int>();
            language = "";
        }
    }
}

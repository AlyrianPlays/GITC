using System.Collections.Generic;

namespace GITC.Core
{
    // A scripted beat inside a mission
    internal class MissionEvent
    {
        public float atSecond { get; set; }
        // Event name, e.g. "command_required", "him_start", "vital_nudge" (matches ModuleEvent types, see T-3.2)
        public string type { get; set; }
        // Free-form arguments: command name, vital name, amount, time limit...
        public Dictionary<string, string> args { get; set; }

        public MissionEvent()
        {
            args = new Dictionary<string, string>();
        }
    }
}

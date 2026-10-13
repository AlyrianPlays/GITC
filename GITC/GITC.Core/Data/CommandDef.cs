using System.Collections.Generic;

namespace GITC.Core
{
    // A TIM command described in Content/Data/commands.json. Behaviour is attached in code by name (see T-5.3).
    internal class CommandDef
    {
        public string name { get; set; }
        public List<string> aliases { get; set; }
        public string description { get; set; }
        // Hidden commands are left out of `help` until discovered
        public bool hidden { get; set; }
        // Where it works: anywhere, mission, overworld
        public string scope { get; set; }
        // Save flags/secrets that must be set before the command is available
        public List<string> requires { get; set; }

        public CommandDef()
        {
            aliases = new List<string>();
            requires = new List<string>();
            scope = "anywhere";
        }
    }
}

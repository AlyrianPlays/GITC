namespace GITC.Core
{
    // A message between modules and the mission. Raised on ModuleManager and delivered to every module's OnEvent.
    internal class ModuleEvent
    {
        public ModuleEventTypes type { get; set; }
        // Module that raised it (pm for the mission/director itself)
        public ModuleTypes source { get; set; }
        // Meaning depends on type: OP amount, seconds, vital value...
        public int amount { get; set; }
        // Extra identifier: command name, barrier id, vital name...
        public string tag { get; set; }

        public ModuleEvent(ModuleEventTypes type, ModuleTypes source, int amount = 0, string tag = "")
        {
            this.type = type;
            this.source = source;
            this.amount = amount;
            this.tag = tag;
        }
    }
}

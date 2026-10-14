namespace GITC.Core
{
    // What a command handler gets to work with
    internal class CommandContext
    {
        public GITCGame currGame { get; set; }
        public TIMModule tim { get; set; }
        public CommandRegistry registry { get; set; }
        // Canonical command name (aliases already resolved)
        public string commandName { get; set; }
        // Arguments split on spaces (empty entries removed)
        public string[] args { get; set; }
        // Everything after the command word, exactly as typed
        public string rawArgs { get; set; }
        // Where the TIM is running: "mission" or "overworld" (CommandDef.scope)
        public string scope { get; set; }

        public SaveManager saveManager
        {
            get { return currGame.currGameState.saveManager; }
        }
    }
}

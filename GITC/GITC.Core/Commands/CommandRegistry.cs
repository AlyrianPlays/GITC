using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // Looks up TIM commands. Data (names, aliases, descriptions, scope, hidden, requires) comes from
    // Content/Data/commands.json; behaviour is a handler registered here by name. A command with data but no
    // handler answers with a friendly message; a handler with no data still runs (built-ins).
    internal class CommandRegistry
    {
        public List<CommandDef> defs { get; set; }
        public Dictionary<string, Func<CommandContext, string[]>> handlers { get; set; }

        private static List<CommandDef> _cachedDefs;

        public CommandRegistry()
        {
            if (_cachedDefs == null)
                _cachedDefs = DataLoader.Load<List<CommandDef>>("commands.json") ?? new List<CommandDef>();
            defs = _cachedDefs;
            handlers = new Dictionary<string, Func<CommandContext, string[]>>(StringComparer.OrdinalIgnoreCase);
            BuiltinCommands.Register(this);
        }

        // Command by name or alias, case-insensitive; null if the data does not define it
        public CommandDef Find(string name)
        {
            return defs.Find(d => d.name.Equals(name, StringComparison.OrdinalIgnoreCase)
                               || d.aliases.Exists(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        // Right scope and every required flag/secret present
        public bool IsAvailable(CommandDef def, string scope, SaveManager saveManager)
        {
            if (def.scope != "anywhere" && def.scope != scope)
                return false;
            foreach (string flag in def.requires)
                if (!saveManager.HasFlag(flag) && !saveManager.current.discoveredSecrets.Contains(flag))
                    return false;
            return true;
        }

        // Runs a typed line and returns the output lines (never throws for bad input)
        public string[] Execute(string line, CommandContext context)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                return new string[0];

            string word = trimmed.Split(' ')[0];
            context.rawArgs = trimmed.Length > word.Length ? trimmed.Substring(word.Length).Trim() : "";
            context.args = context.rawArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string name = word;
            CommandDef def = Find(word);
            if (def != null)
            {
                if (!IsAvailable(def, context.scope, context.saveManager))
                    return new[] { "Command not found: " + word };
                name = def.name;
            }
            context.commandName = name;

            if (handlers.TryGetValue(name, out Func<CommandContext, string[]> handler))
                return handler(context);
            if (def != null)
                return new[] { "'" + def.name + "' is not available yet." };
            return new[] { "Command not found: " + word };
        }
    }
}

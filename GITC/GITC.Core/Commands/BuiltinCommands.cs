using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // Handlers for the commands that ship with the TIM. Names match Content/Data/commands.json and the Commands enum.
    internal static class BuiltinCommands
    {
        public static void Register(CommandRegistry registry)
        {
            registry.handlers[Commands.echo.ToString()] = Echo;
            registry.handlers[Commands.help.ToString()] = Help;
            registry.handlers[Commands.clear.ToString()] = Clear;
        }

        // echo <text>: prints the text back
        private static string[] Echo(CommandContext context)
        {
            return new[] { context.rawArgs };
        }

        // help: every visible command and its description
        private static string[] Help(CommandContext context)
        {
            List<string> lines = new List<string>();
            foreach (CommandDef def in context.registry.defs)
            {
                if (def.hidden)
                    continue;
                lines.Add(def.name.PadRight(12) + def.description);
            }
            lines.Sort(StringComparer.OrdinalIgnoreCase);
            return lines.ToArray();
        }

        // clear: empties the console history
        private static string[] Clear(CommandContext context)
        {
            context.tim.ClearHistory();
            return new string[0];
        }
    }
}

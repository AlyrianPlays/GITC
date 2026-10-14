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
            registry.handlers[Commands.listdir.ToString()] = ListDir;
            registry.handlers[Commands.changedir.ToString()] = ChangeDir;
        }

        // echo <text>: prints the text back
        private static string[] Echo(CommandContext context)
        {
            return new[] { context.rawArgs };
        }

        // help: the commands usable here (right scope, requirements met), without the hidden ones
        private static string[] Help(CommandContext context)
        {
            List<string> lines = new List<string>();
            foreach (CommandDef def in context.registry.defs)
            {
                if (def.hidden || !context.registry.IsAvailable(def, context.scope, context.saveManager))
                    continue;
                lines.Add(def.name.PadRight(12) + def.description);
            }
            lines.Sort(StringComparer.OrdinalIgnoreCase);
            return lines.ToArray();
        }

        //TODO: flat stand-ins until the virtual filesystem exists (T-5.5)
        private static string[] ListDir(CommandContext context)
        {
            return new[] { "(no files)" };
        }

        private static string[] ChangeDir(CommandContext context)
        {
            return new[] { "No such directory: " + context.rawArgs };
        }

        // clear: empties the console history
        private static string[] Clear(CommandContext context)
        {
            context.tim.ClearHistory();
            return new string[0];
        }
    }
}

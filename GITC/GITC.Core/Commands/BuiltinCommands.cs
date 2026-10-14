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
            registry.handlers[Commands.read.ToString()] = Read;
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

        // listdir [path]: directories (with a trailing /) then files
        private static string[] ListDir(CommandContext context)
        {
            List<string> entries = context.fileSystem.List(context.rawArgs);
            if (entries == null)
                return new[] { "No such directory: " + context.rawArgs };
            return entries.Count == 0 ? new[] { "(empty)" } : entries.ToArray();
        }

        // changedir [path]: moves and prints the new path; with no path just prints where you are
        private static string[] ChangeDir(CommandContext context)
        {
            if (!context.fileSystem.ChangeDir(context.rawArgs))
                return new[] { "No such directory: " + context.rawArgs };
            return new[] { context.fileSystem.CurrentPath };
        }

        // read <file>: prints a text file
        private static string[] Read(CommandContext context)
        {
            if (context.rawArgs.Length == 0)
                return new[] { "Usage: read <file>" };
            List<string> text = context.fileSystem.Read(context.rawArgs);
            if (text == null)
                return new[] { "File not found: " + context.rawArgs };
            return text.ToArray();
        }

        // clear: empties the console history
        private static string[] Clear(CommandContext context)
        {
            context.tim.ClearHistory();
            return new string[0];
        }
    }
}

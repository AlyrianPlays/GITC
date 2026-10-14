using System;

namespace GITC.Core
{
    // A command the player must type before time runs out (raised by other modules or the director through
    // a command_required event). Missing it costs Operator Points (see Mission.HandleEvent).
    internal class CommandChallenge
    {
        // The command to type, as written in the event tag (e.g. "unlock barrier")
        public string command { get; set; }
        public float timeLimit { get; set; }
        public float remaining { get; set; }

        public CommandChallenge(string command, float timeLimit)
        {
            this.command = command;
            this.timeLimit = timeLimit;
            remaining = timeLimit;
        }

        // Lower-case, trimmed, runs of spaces collapsed to one
        public static string Normalize(string text)
        {
            return string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        }

        // Case-insensitive and ignores extra whitespace
        public bool Matches(string line)
        {
            return Normalize(line) == Normalize(command);
        }
    }
}

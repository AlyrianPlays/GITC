using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // The TIM's make-believe filesystem: a tree loaded from filesystem.json plus a current directory.
    // Names are case-insensitive. Hidden entries stay invisible until their unlockFlag is a save flag or secret.
    internal class VirtualFileSystem
    {
        public VirtualDir root { get; set; }
        // Directories from the root down to the current one
        public List<VirtualDir> current { get; set; }
        public SaveManager saveManager { get; set; }

        public VirtualFileSystem(SaveManager saveManager)
        {
            this.saveManager = saveManager;
            root = DataLoader.Load<VirtualDir>("filesystem.json") ?? new VirtualDir();
            current = new List<VirtualDir>() { root };
        }

        // "/" for the root, "/logs/archive" below it
        public string CurrentPath
        {
            get
            {
                if (current.Count == 1)
                    return "/";
                return "/" + string.Join("/", current.GetRange(1, current.Count - 1).ConvertAll(d => d.name));
            }
        }

        private bool IsVisible(bool hidden, string unlockFlag)
        {
            if (!hidden)
                return true;
            return !string.IsNullOrEmpty(unlockFlag) && (saveManager.HasFlag(unlockFlag) || saveManager.current.discoveredSecrets.Contains(unlockFlag));
        }

        // Follows a path (absolute with a leading '/', otherwise from the current directory). skipLast leaves off
        // the final segment (the file name). Returns null if any directory on the way does not exist or is hidden.
        private List<VirtualDir> Walk(string path, int skipLast, out string[] segments)
        {
            segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            List<VirtualDir> stack = path.StartsWith("/") ? new List<VirtualDir>() { root } : new List<VirtualDir>(current);
            for (int i = 0; i < segments.Length - skipLast; i++)
            {
                string segment = segments[i];
                if (segment == ".")
                    continue;
                if (segment == "..")
                {
                    if (stack.Count > 1)
                        stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                VirtualDir next = stack[stack.Count - 1].dirs.Find(d => d.name.Equals(segment, StringComparison.OrdinalIgnoreCase) && IsVisible(d.hidden, d.unlockFlag));
                if (next == null)
                    return null;
                stack.Add(next);
            }
            return stack;
        }

        // Directories (with a trailing '/') then files, visible entries only; null if the path is not a directory
        public List<string> List(string path)
        {
            List<VirtualDir> stack = Walk(string.IsNullOrWhiteSpace(path) ? "." : path.Trim(), 0, out _);
            if (stack == null)
                return null;
            VirtualDir dir = stack[stack.Count - 1];
            List<string> lines = new List<string>();
            foreach (VirtualDir d in dir.dirs)
                if (IsVisible(d.hidden, d.unlockFlag))
                    lines.Add(d.name + "/");
            foreach (VirtualFile f in dir.files)
                if (IsVisible(f.hidden, f.unlockFlag))
                    lines.Add(f.name);
            return lines;
        }

        // False if the directory does not exist (or is still hidden)
        public bool ChangeDir(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;
            List<VirtualDir> stack = Walk(path.Trim(), 0, out _);
            if (stack == null)
                return false;
            current = stack;
            return true;
        }

        // File by path, null if missing or hidden
        public VirtualFile FindFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            string trimmed = path.Trim();
            List<VirtualDir> stack = Walk(trimmed, 1, out string[] segments);
            if (stack == null || segments.Length == 0)
                return null;
            string fileName = segments[segments.Length - 1];
            return stack[stack.Count - 1].files.Find(f => f.name.Equals(fileName, StringComparison.OrdinalIgnoreCase) && IsVisible(f.hidden, f.unlockFlag));
        }

        // Reads a file: its text lines, plus the save side effects (flag, secret) that unlock other entries
        public List<string> Read(string path)
        {
            VirtualFile file = FindFile(path);
            if (file == null)
                return null;
            if (!string.IsNullOrEmpty(file.setsFlag))
                saveManager.SetFlag(file.setsFlag);
            if (!string.IsNullOrEmpty(file.discoversSecret))
                saveManager.AddSecret(file.discoversSecret);
            return file.text;
        }
    }
}

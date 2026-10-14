using System.Collections.Generic;

namespace GITC.Core
{
    // A directory in the TIM's virtual filesystem; the root has an empty name
    internal class VirtualDir
    {
        public string name { get; set; }
        public List<VirtualDir> dirs { get; set; }
        public List<VirtualFile> files { get; set; }
        // Hidden directories do not show in listdir and cannot be entered until their unlockFlag is set
        public bool hidden { get; set; }
        public string unlockFlag { get; set; }

        public VirtualDir()
        {
            name = "";
            dirs = new List<VirtualDir>();
            files = new List<VirtualFile>();
            unlockFlag = "";
        }
    }
}

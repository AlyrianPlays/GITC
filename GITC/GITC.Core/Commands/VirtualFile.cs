using System.Collections.Generic;

namespace GITC.Core
{
    // A readable text file in the TIM's virtual filesystem (Content/Data/filesystem.json)
    internal class VirtualFile
    {
        public string name { get; set; }
        public List<string> text { get; set; }
        // Hidden files do not show in listdir and cannot be read until their unlockFlag is set
        public bool hidden { get; set; }
        public string unlockFlag { get; set; }
        // Save flag set the first time the file is read (can unlock other hidden entries)
        public string setsFlag { get; set; }
        // Secret recorded in the save when the file is read (lore/meta discovery)
        public string discoversSecret { get; set; }

        public VirtualFile()
        {
            text = new List<string>();
            unlockFlag = "";
            setsFlag = "";
            discoversSecret = "";
        }
    }
}

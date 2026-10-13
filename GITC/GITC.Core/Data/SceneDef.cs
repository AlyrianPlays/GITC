using System.Collections.Generic;

namespace GITC.Core
{
    // A between-level story scene: a sequence of text lines
    internal class SceneDef
    {
        public string id { get; set; }
        public string title { get; set; }
        public List<SceneLine> lines { get; set; }
        // Scene id that follows this one, empty to return to the overworld
        public string nextSceneId { get; set; }

        public SceneDef()
        {
            lines = new List<SceneLine>();
        }
    }
}

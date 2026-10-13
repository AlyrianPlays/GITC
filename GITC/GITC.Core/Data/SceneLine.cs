namespace GITC.Core
{
    // One line of a SceneDef
    internal class SceneLine
    {
        public string speaker { get; set; }
        public string text { get; set; }
        // Pause before the line appears
        public int delayMilliseconds { get; set; }
        // Optional ContentRegistry texture key
        public string portraitKey { get; set; }
        // Optional save flag to set when this line is shown (story/path progress)
        public string setFlag { get; set; }
    }
}

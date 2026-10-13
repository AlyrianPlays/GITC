namespace GITC.Core
{
    // Hardware Interface Module ("simon says") settings
    internal class HimConfig
    {
        // Steps in the sequence
        public int sequenceLength { get; set; }
        // Milliseconds each step is shown during playback
        public int stepMilliseconds { get; set; }
        // Seconds the player has to finish entering the sequence
        public float inputTimeoutSeconds { get; set; }
        // Can the player ask for the sequence again (costs time, not points)
        public bool allowReplay { get; set; }
        // Random-trigger window used by the director, in seconds since the last HIM; 0 = scripted only
        public float minIntervalSeconds { get; set; }
        public float maxIntervalSeconds { get; set; }

        public HimConfig()
        {
            sequenceLength = 4;
            stepMilliseconds = 600;
            inputTimeoutSeconds = 10f;
        }
    }
}

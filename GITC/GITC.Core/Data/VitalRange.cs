using System.Text.Json.Serialization;

namespace GITC.Core
{
    // Behaviour of a single vital sign
    internal class VitalRange
    {
        public float start { get; set; }
        // Values outside [safeMin, safeMax] are "critical" and require a TIM command
        public float safeMin { get; set; }
        public float safeMax { get; set; }
        // Natural drift per second (can be negative)
        public float driftPerSecond { get; set; }
        // Seconds allowed out of range before the field agent dies
        public float deathSeconds { get; set; }
        // Hard limits the value can reach; 0 and 0 (the default) means one safe span beyond each safe edge
        public float min { get; set; }
        public float max { get; set; }

        [JsonConstructor]
        public VitalRange()
        {
            deathSeconds = 10f;
        }

        public VitalRange(float start, float safeMin, float safeMax, float driftPerSecond = 0f, float deathSeconds = 10f, float min = 0f, float max = 0f)
        {
            this.min = min;
            this.max = max;
            this.start = start;
            this.safeMin = safeMin;
            this.safeMax = safeMax;
            this.driftPerSecond = driftPerSecond;
            this.deathSeconds = deathSeconds;
        }
    }
}

using System;

namespace GITC.Core
{
    // One vital sign of the field agent: its value, safe range, and how long it has been out of range
    internal class Vital
    {
        // A value this close (as a fraction of the safe span) to a safe edge counts as a warning
        public const float WARNINGMARGIN = 0.15f;

        public Vitals type { get; set; }
        public VitalRange range { get; set; }
        public float value { get; set; }
        // Natural change per second (starts as range.driftPerSecond)
        public float drift { get; set; }
        // Hard limits the value is clamped to
        public float hardMin { get; set; }
        public float hardMax { get; set; }
        // Seconds spent out of range; reaching deathThreshold kills the agent. Falls again while in range.
        public float outOfRangeSeconds { get; set; }
        public bool isCritical { get; set; }
        public bool tooHigh { get; set; }
        // True once the current out-of-range episode has been announced
        public bool warned { get; set; }

        public Vital(Vitals type, VitalRange range)
        {
            this.type = type;
            this.range = range;
            drift = range.driftPerSecond;
            float span = range.safeMax - range.safeMin;
            // min/max of 0/0 means "not set": allow one safe span beyond each edge
            hardMin = range.max > range.min ? range.min : range.safeMin - span;
            hardMax = range.max > range.min ? range.max : range.safeMax + span;
            Reset();
        }

        public string name
        {
            get { return type.ToString(); }
        }

        // Seconds out of range that kill the agent
        public float deathThreshold
        {
            get { return range.deathSeconds; }
        }

        public bool isOutOfRange
        {
            get { return value < range.safeMin || value > range.safeMax; }
        }

        // normal / warning (near a safe edge) / critical (out of range)
        public VitalStates state
        {
            get
            {
                if (isOutOfRange)
                    return VitalStates.critical;
                // Warn when close to a safe edge, but only an edge the value can really cross: sanity at its
                // maximum is as good as it gets, so its top edge (safeMax == hardMax) never warns
                float margin = (range.safeMax - range.safeMin) * WARNINGMARGIN;
                bool canGoLow = range.safeMin > hardMin;
                bool canGoHigh = range.safeMax < hardMax;
                if ((canGoLow && value < range.safeMin + margin) || (canGoHigh && value > range.safeMax - margin))
                    return VitalStates.warning;
                return VitalStates.normal;
            }
        }

        public void Reset()
        {
            value = range.start;
            drift = range.driftPerSecond;
            outOfRangeSeconds = 0f;
            isCritical = false;
            tooHigh = false;
            warned = false;
        }

        public void Clamp()
        {
            value = Math.Clamp(value, hardMin, hardMax);
        }
    }
}

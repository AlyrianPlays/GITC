using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // The field agent's three vital signs and the rules that go with them (no drawing, no events: VMModule wires those).
    // A vital outside its safe range is critical; staying critical for its deathSeconds kills the agent.
    internal class VitalsModel
    {
        // vital_nudge events carry whole numbers, so their amount is in tenths of a unit (-300 = -30 bpm, +15 = +1.5 degrees)
        public const float NUDGEUNIT = 0.1f;

        // Indexed by the Vitals enum
        public List<Vital> vitals { get; set; }
        public bool isDead { get; set; }
        // A vital just left its safe range (once per episode); bool = too high
        public Action<Vital, bool> onCritical { get; set; }
        // A vital stayed out of range too long
        public Action<Vital> onDeath { get; set; }

        public VitalsModel(VitalsConfig config)
        {
            vitals = new List<Vital>();
            foreach (Vitals v in Enum.GetValues(typeof(Vitals)))
                vitals.Insert((int)v, new Vital(v, RangeFor(config, v)));
            isDead = false;
        }

        private static VitalRange RangeFor(VitalsConfig config, Vitals vital)
        {
            switch (vital)
            {
                case Vitals.heart_rate:
                    return config.heartRate;
                case Vitals.sanity:
                    return config.sanity;
                default:
                    return config.temperature;
            }
        }

        public Vital Get(Vitals vital)
        {
            return vitals[(int)vital];
        }

        public void Update(float deltaSeconds)
        {
            if (isDead)
                return;

            foreach (Vital v in vitals)
            {
                v.value += v.drift * deltaSeconds;
                v.Clamp();

                if (v.isOutOfRange)
                {
                    v.tooHigh = v.value > v.range.safeMax;
                    v.isCritical = true;
                    v.outOfRangeSeconds += deltaSeconds;
                    if (!v.warned)
                    {
                        v.warned = true;
                        onCritical?.Invoke(v, v.tooHigh);
                    }
                    if (v.outOfRangeSeconds >= v.deathThreshold)
                    {
                        isDead = true;
                        onDeath?.Invoke(v);
                        return;
                    }
                }
                else
                {
                    v.isCritical = false;
                    v.warned = false;
                    v.outOfRangeSeconds = Math.Max(0f, v.outOfRangeSeconds - deltaSeconds);
                }
            }
        }

        // Instant change (a command, or something happening to the agent), kept inside the hard limits
        public void Nudge(Vitals vital, float amount)
        {
            Vital v = Get(vital);
            v.value += amount;
            v.Clamp();
        }

        // Sets how fast a vital changes on its own, in units per second
        public void SetDrift(Vitals vital, float unitsPerSecond)
        {
            Get(vital).drift = unitsPerSecond;
        }

        // Back to the starting values (the agent was revived)
        public void Reset()
        {
            foreach (Vital v in vitals)
                v.Reset();
            isDead = false;
        }
    }
}

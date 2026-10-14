using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // Vital Monitor: three readouts (heart rate, sanity, temperature) with a scrolling graph each.
    // Green = in range, amber = close to a safe edge, red = out of range (blinking, with the time left before the agent is lost).
    internal class VMModule : GameModule
    {
        public VitalsModel model { get; set; }

        // Layout, in pixels inside the region: three rows, readout on the left, graph on the right
        public const int ROWHEIGHT = 160;
        public const int GRAPHX = 240;
        public const int GRAPHY = 16;
        public const int GRAPHWIDTH = 520;
        public const int GRAPHHEIGHT = 128;
        // ECG: one sample every 0.02 s, 2 px wide (about 5 s on screen). Value graphs: one sample every 0.25 s, 4 px wide (about 32 s).
        public const float ECGSTEP = 0.02f;
        public const int ECGPIXELS = 2;
        public const float VALUESTEP = 0.25f;
        public const int VALUEPIXELS = 4;

        private List<float> ecg { get; set; }
        private float ecgPhase { get; set; }
        private float ecgAccumulator { get; set; }
        // Recent values per vital, indexed by the Vitals enum (heart rate unused: it draws the ECG)
        private List<List<float>> valueHistory { get; set; }
        private float valueAccumulator { get; set; }
        private double totalSeconds { get; set; }

        public VMModule(GITCGame currGame, VitalsConfig config) : base(currGame, ModuleTypes.vm, HudLayout.VM)
        {
            model = new VitalsModel(config);
            model.onCritical = (vital, tooHigh) => Raise(new ModuleEvent(ModuleEventTypes.vital_critical, ModuleTypes.vm, tooHigh ? 1 : -1, vital.name));
            model.onDeath = vital => Raise(new ModuleEvent(ModuleEventTypes.agent_died, ModuleTypes.vm, 0, vital.name));

            ecg = new List<float>();
            for (int i = 0; i < GRAPHWIDTH / ECGPIXELS; i++)
                ecg.Add(0f);
            valueHistory = new List<List<float>>();
            foreach (Vitals v in Enum.GetValues(typeof(Vitals)))
            {
                List<float> samples = new List<float>();
                for (int i = 0; i < GRAPHWIDTH / VALUEPIXELS; i++)
                    samples.Add(model.Get(v).value);
                valueHistory.Insert((int)v, samples);
            }
        }

        public override void Update(GameTime gameTime)
        {
            totalSeconds = gameTime.TotalGameTime.TotalSeconds;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            model.Update(dt);
            if (model.isDead)
                return;

            // ECG trace: the beat rate follows the heart rate
            float heartRate = model.Get(Vitals.heart_rate).value;
            ecgAccumulator += dt;
            while (ecgAccumulator >= ECGSTEP)
            {
                ecgAccumulator -= ECGSTEP;
                ecgPhase = (ecgPhase + heartRate / 60f * ECGSTEP) % 1f;
                ecg.RemoveAt(0);
                ecg.Add(EcgWave(ecgPhase));
            }

            valueAccumulator += dt;
            while (valueAccumulator >= VALUESTEP)
            {
                valueAccumulator -= VALUESTEP;
                foreach (Vitals v in Enum.GetValues(typeof(Vitals)))
                {
                    valueHistory[(int)v].RemoveAt(0);
                    valueHistory[(int)v].Add(model.Get(v).value);
                }
            }
        }

        public override void HandleInput(InputManager input)
        {
        }

        public override void OnEvent(ModuleEvent moduleEvent)
        {
            // vital_nudge: tag = Vitals name, amount in tenths of a unit
            if (moduleEvent.type == ModuleEventTypes.vital_nudge && Enum.TryParse(moduleEvent.tag, out Vitals vital))
                model.Nudge(vital, moduleEvent.amount * VitalsModel.NUDGEUNIT);
            else if (moduleEvent.type == ModuleEventTypes.agent_revived)
                model.Reset();
        }

        // One heartbeat as a sum of bumps: P wave, QRS spike, T wave. phase is 0-1 through the beat.
        public static float EcgWave(float phase)
        {
            return Bump(phase, 0.10f, 0.04f, 0.12f)
                 + Bump(phase, 0.20f, 0.012f, -0.20f)
                 + Bump(phase, 0.235f, 0.014f, 1.00f)
                 + Bump(phase, 0.27f, 0.012f, -0.30f)
                 + Bump(phase, 0.47f, 0.06f, 0.25f);
        }

        private static float Bump(float x, float center, float width, float height)
        {
            float t = (x - center) / width;
            return height * (float)Math.Exp(-t * t);
        }

        private static Color StateColor(VitalStates state)
        {
            switch (state)
            {
                case VitalStates.critical:
                    return Color.Red;
                case VitalStates.warning:
                    return Color.Orange;
                default:
                    return Color.Lime;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            SpriteFont font = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);
            Texture2D pixel = currGame._content.pixel;
            bool blinkOn = ((int)(totalSeconds * 4.0)) % 2 == 0;

            foreach (Vitals v in Enum.GetValues(typeof(Vitals)))
            {
                Vital vital = model.Get(v);
                Rectangle row = new Rectangle(region.X, region.Y + (int)v * ROWHEIGHT, region.Width, ROWHEIGHT);
                Color color = StateColor(vital.state);

                // Warning blink while out of range
                if (vital.isCritical && blinkOn && !model.isDead)
                    spriteBatch.Draw(pixel, row, Color.Red * 0.25f);

                // Readout: label, big number with its unit, status line
                spriteBatch.DrawString(font, VitalLabel(v), new Vector2(row.X + 16, row.Y + 10), Color.Gray);
                string number = v == Vitals.temperature ? vital.value.ToString("0.0") : ((int)Math.Round(vital.value)).ToString();
                spriteBatch.DrawString(font, number, new Vector2(row.X + 16, row.Y + 36), color, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, VitalUnit(v), new Vector2(row.X + 16 + number.Length * 24 + 8, row.Y + 66), Color.Gray);

                string status;
                if (model.isDead)
                    status = "AGENT LOST";
                else if (vital.isCritical)
                    status = (vital.tooHigh ? "TOO HIGH " : "TOO LOW ") + Math.Max(0, (int)Math.Ceiling(vital.deathThreshold - vital.outOfRangeSeconds)) + "s";
                else
                    status = vital.state == VitalStates.warning ? "WARNING" : "NORMAL";
                spriteBatch.DrawString(font, status, new Vector2(row.X + 16, row.Y + 100), model.isDead ? Color.Red : color);

                DrawGraph(spriteBatch, pixel, row, v, vital, color);
            }
        }

        private void DrawGraph(SpriteBatch spriteBatch, Texture2D pixel, Rectangle row, Vitals v, Vital vital, Color color)
        {
            Rectangle graph = new Rectangle(row.X + GRAPHX, row.Y + GRAPHY, GRAPHWIDTH, GRAPHHEIGHT);
            spriteBatch.Draw(pixel, graph, Color.White * 0.05f);

            // Heart rate draws the ECG trace; the others plot their value between the hard limits
            bool isEcg = v == Vitals.heart_rate;
            float lo = isEcg ? -0.4f : vital.hardMin;
            float hi = isEcg ? 1.2f : vital.hardMax;

            // Safe band (for the value graphs)
            if (!isEcg)
            {
                int top = GraphY(graph, vital.range.safeMax, lo, hi);
                int bottom = GraphY(graph, vital.range.safeMin, lo, hi);
                spriteBatch.Draw(pixel, new Rectangle(graph.X, top, graph.Width, Math.Max(1, bottom - top)), Color.Lime * 0.10f);
            }

            List<float> samples = isEcg ? ecg : valueHistory[(int)v];
            int pixelsPerSample = isEcg ? ECGPIXELS : VALUEPIXELS;
            int previousY = GraphY(graph, samples[0], lo, hi);
            for (int i = 0; i < samples.Count; i++)
            {
                int y = GraphY(graph, samples[i], lo, hi);
                int segmentTop = Math.Min(y, previousY);
                spriteBatch.Draw(pixel, new Rectangle(graph.X + i * pixelsPerSample, segmentTop, pixelsPerSample, Math.Abs(y - previousY) + 2), color);
                previousY = y;
            }
        }

        // Value to a y pixel inside the graph (hi at the top)
        private static int GraphY(Rectangle graph, float value, float lo, float hi)
        {
            float t = (Math.Clamp(value, lo, hi) - lo) / (hi - lo);
            return graph.Bottom - 2 - (int)(t * (graph.Height - 4));
        }

        private static string VitalLabel(Vitals v)
        {
            switch (v)
            {
                case Vitals.heart_rate:
                    return "HEART RATE";
                case Vitals.sanity:
                    return "SANITY";
                default:
                    return "TEMPERATURE";
            }
        }

        private static string VitalUnit(Vitals v)
        {
            switch (v)
            {
                case Vitals.heart_rate:
                    return "bpm";
                case Vitals.sanity:
                    return "%";
                default:
                    return "C";
            }
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GITC.Core
{
    // Progress Meters: time remaining in the cyan pill, Operator Points in the pink pill. Display only.
    internal class PMModule : GameModule
    {
        public Mission mission { get; set; }
        // Pill text is drawn at this scale of the console font
        public float textScale { get; set; }
        private double totalSeconds { get; set; }

        public const float LOWOPFRACTION = 0.25f;
        public const float LOWTIMESECONDS = 10f;

        public PMModule(GITCGame currGame, Mission mission) : base(currGame, ModuleTypes.pm, Rectangle.Union(HudLayout.TimePill, HudLayout.OPPill))
        {
            this.mission = mission;
            textScale = 2f;
            isFocusable = false;
        }

        public override void Update(GameTime gameTime)
        {
            totalSeconds = gameTime.TotalGameTime.TotalSeconds;
        }

        public override void HandleInput(InputManager input)
        {
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            int seconds = (int)Math.Ceiling(mission.timeRemaining);
            string timeText = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            bool timeLow = mission.status == MissionStatus.running && mission.timeRemaining < LOWTIMESECONDS;
            DrawPill(spriteBatch, HudLayout.TimePill, timeText, timeLow);

            bool opLow = mission.operatorPoints <= mission.maxOperatorPoints * LOWOPFRACTION;
            DrawPill(spriteBatch, HudLayout.OPPill, mission.operatorPoints.ToString(), opLow);
        }

        // Centered text in a pill; a low value pulses red over the pill
        private void DrawPill(SpriteBatch spriteBatch, Rectangle pill, string text, bool pulse)
        {
            SpriteFont font = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);
            Vector2 size = font.MeasureString(text) * textScale;
            Vector2 position = new Vector2(pill.X + (pill.Width - size.X) / 2f, pill.Y + (pill.Height - size.Y) / 2f);

            if (pulse)
            {
                float alpha = 0.25f + 0.25f * (float)Math.Sin(totalSeconds * 8.0);
                spriteBatch.Draw(currGame._content.pixel, pill, Color.Red * alpha);
            }
            spriteBatch.DrawString(font, text, new Vector2((int)position.X, (int)position.Y), Color.Black, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }
    }
}

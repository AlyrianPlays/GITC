using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GITC.Core
{
    // Win/lose overlay drawn over the main screen area once a mission has ended
    internal class MissionResultScreen
    {
        public GITCGame currGame { get; set; }

        public MissionResultScreen(GITCGame currGame)
        {
            this.currGame = currGame;
        }

        // Call with no SpriteBatch open
        public void Draw(SpriteBatch spriteBatch, Mission mission)
        {
            bool won = mission.status == MissionStatus.won;
            Rectangle screen = Rectangle.Union(HudLayout.TIM, HudLayout.VM);
            SpriteFont font = currGame._content.GetFont(ContentRegistry.DEFAULTFONT);

            string title = won ? "MISSION COMPLETE" : "MISSION FAILED";
            string name = mission.def.name;
            string ops = "Operator Points remaining: " + mission.operatorPoints;
            string prompt = "Press Enter to continue";

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            spriteBatch.Draw(currGame._content.pixel, screen, Color.Black * 0.8f);
            DrawCentered(spriteBatch, font, title, screen, screen.Y + 160, 3f, won ? Color.Lime : Color.Red);
            DrawCentered(spriteBatch, font, name, screen, screen.Y + 260, 1f, Color.White);
            DrawCentered(spriteBatch, font, ops, screen, screen.Y + 320, 2f, Color.White);
            DrawCentered(spriteBatch, font, prompt, screen, screen.Y + 480, 1f, Color.White);
            spriteBatch.End();
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, Rectangle area, int y, float scale, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;
            Vector2 size = font.MeasureString(text) * scale;
            spriteBatch.DrawString(font, text, new Vector2((int)(area.X + (area.Width - size.X) / 2f), y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}

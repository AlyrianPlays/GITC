using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GITC.Core
{
    // Stand-in for a module that does not exist yet: a tinted fill, a label, and a debug line for any input received.
    // Each real module replaces its placeholder as its phase is built.
    internal class PlaceholderModule : GameModule
    {
        public Color tint { get; set; }

        public PlaceholderModule(GITCGame currGame, ModuleTypes moduleType, Rectangle region, Color tint) : base(currGame, moduleType, region)
        {
            this.tint = tint;
        }

        public override void Update(GameTime gameTime)
        {
        }

        public override void HandleInput(InputManager input)
        {
            //TESTING, REMOVE AFTER: show which module receives input
            foreach (KeyBinds kb in Enum.GetValues(typeof(KeyBinds)))
                if (input.Pressed(kb))
                    System.Diagnostics.Debug.WriteLine(moduleType + " received " + kb);
            foreach (TextInputEventArgs typed in input.typedChars)
                if (typed.Character != '\0' && !char.IsControl(typed.Character))
                    System.Diagnostics.Debug.WriteLine(moduleType + " received char " + typed.Character);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(currGame._content.pixel, region, tint * 0.25f);
            spriteBatch.DrawString(currGame._content.GetFont(ContentRegistry.DEFAULTFONT),
                                   moduleType.ToString().ToUpper() + (isFocused ? " [FOCUS]" : ""),
                                   new Vector2(region.X + HudLayout.CELL, region.Y + HudLayout.CELL),
                                   Color.White);
        }
    }
}

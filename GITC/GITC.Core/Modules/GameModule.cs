using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GITC.Core
{
    // Base class for every HUD module (TIM, FAV, LVM, VM, HIM, PM).
    // ModuleManager calls Update on every active module each frame (background modules keep running),
    // HandleInput only on the focused one, and Draw clipped to the module's region.
    internal abstract class GameModule
    {
        public GITCGame currGame { get; set; }
        public ModuleTypes moduleType { get; set; }
        // Screen rectangle in 1920x1080 logical coordinates (from HudLayout)
        public Rectangle region { get; set; }
        // Receives keyboard input; only one module at a time
        public bool isFocused { get; set; }
        // Inactive modules are neither updated nor drawn (a mission can leave modules out)
        public bool isActive { get; set; }
        // Can the player select it (PM is display only)
        public bool isFocusable { get; set; }

        public GameModule(GITCGame currGame, ModuleTypes moduleType, Rectangle region)
        {
            this.currGame = currGame;
            this.moduleType = moduleType;
            this.region = region;
            isFocused = false;
            isActive = true;
            isFocusable = true;
        }

        // Runs every frame while active, focused or not
        public abstract void Update(GameTime gameTime);

        // Runs only while focused
        public abstract void HandleInput(InputManager input);

        // Called for every event raised on the ModuleManager, whichever module raised it (including this one)
        public virtual void OnEvent(ModuleEvent moduleEvent)
        {
        }

        // Drawn with a scissor rectangle set to region; draw in logical coordinates
        public abstract void Draw(SpriteBatch spriteBatch);
    }
}

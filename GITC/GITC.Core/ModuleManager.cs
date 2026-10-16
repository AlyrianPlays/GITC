using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // Owns the modules, the focus, and the event queue.
    // Every active module is updated every frame (background modules keep running, which the HIM rule needs);
    // only the focused module gets input. Each module is drawn clipped to its own region.
    internal class ModuleManager
    {
        public GITCGame currGame { get; set; }
        public List<GameModule> modules { get; set; }
        public ModuleTypes focusedModule { get; set; }
        // When true no module receives input or focus changes (the HIM takes over in T-9.2)
        public bool inputLocked { get; set; }
        public Queue<ModuleEvent> events { get; set; }
        // Called for each event after the modules have seen it (Mission subscribes in T-4.1)
        public Action<ModuleEvent> eventHandler { get; set; }

        private static readonly RasterizerState scissorState = new RasterizerState() { ScissorTestEnable = true, CullMode = CullMode.None };

        // def is the mission being built (null gives default module settings)
        public ModuleManager(GITCGame currGame, MissionDef def = null)
        {
            this.currGame = currGame;
            modules = new List<GameModule>();
            events = new Queue<ModuleEvent>();
            inputLocked = false;

            //TESTING, REMOVE AFTER: placeholders until each real module exists
            modules.Add(new TIMModule(currGame));
            //Deprecated: FAV placeholder, replaced by FAVModule (T-8.1)
            //modules.Add(new PlaceholderModule(currGame, ModuleTypes.fav, HudLayout.FAV, Color.Orange));
            ModuleConfig favConfig = def?.modules.Find(m => m.module == "fav");
            modules.Add(new FAVModule(currGame, favConfig?.favLevelFile));
            ModuleConfig lvmConfig = def?.modules.Find(m => m.module == "lvm");
            modules.Add(new LVMModule(currGame, lvmConfig?.maze ?? new MazeConfig(), lvmConfig?.difficulty ?? 1));
            ModuleConfig vmConfig = def?.modules.Find(m => m.module == "vm");
            modules.Add(new VMModule(currGame, vmConfig?.vitals ?? new VitalsConfig()));

            foreach (GameModule m in modules)
                m.manager = this;

            SetFocus(ModuleTypes.tim);
        }

        // Used by Mission to add modules that are not part of the default set (e.g. the PM)
        public void AddModule(GameModule module)
        {
            module.manager = this;
            modules.Add(module);
        }

        public GameModule Get(ModuleTypes moduleType)
        {
            return modules.Find(m => m.moduleType == moduleType);
        }

        public void SetFocus(ModuleTypes moduleType)
        {
            GameModule target = Get(moduleType);
            if (target == null || !target.isActive || !target.isFocusable)
                return;
            focusedModule = moduleType;
            foreach (GameModule m in modules)
                m.isFocused = m.moduleType == moduleType;
        }

        // Queue an event; it is delivered to every module during the next Update
        public void Raise(ModuleEvent moduleEvent)
        {
            events.Enqueue(moduleEvent);
        }

        public void Update(GameTime gameTime, InputManager input)
        {
            if (!inputLocked)
            {
                UpdateFocus(input);
                GameModule focused = Get(focusedModule);
                if (focused != null && focused.isActive)
                    focused.HandleInput(input);
            }

            foreach (GameModule m in modules)
                if (m.isActive)
                    m.Update(gameTime);

            // Events raised while dispatching are delivered in the same pass
            while (events.Count > 0)
            {
                ModuleEvent e = events.Dequeue();
                foreach (GameModule m in modules)
                    if (m.isActive)
                        m.OnEvent(e);
                eventHandler?.Invoke(e);
            }
        }

        // Focus follows a click inside a module, or the focus_* keybinds (F1-F4 by default)
        private void UpdateFocus(InputManager input)
        {
            if (input.Pressed(KeyBinds.focus_tim)) SetFocus(ModuleTypes.tim);
            if (input.Pressed(KeyBinds.focus_fav)) SetFocus(ModuleTypes.fav);
            if (input.Pressed(KeyBinds.focus_lvm)) SetFocus(ModuleTypes.lvm);
            if (input.Pressed(KeyBinds.focus_vm)) SetFocus(ModuleTypes.vm);

            foreach (GameModule m in modules)
                if (m.isActive && m.isFocusable && input.MouseClicked(m.region))
                    SetFocus(m.moduleType);
        }

        // Call with no SpriteBatch open: each module gets its own scissored batch
        public void Draw(SpriteBatch spriteBatch)
        {
            GraphicsDevice device = currGame.GraphicsDevice;
            Rectangle oldScissor = device.ScissorRectangle;
            foreach (GameModule m in modules)
            {
                if (!m.isActive)
                    continue;
                device.ScissorRectangle = m.region;
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, scissorState);
                m.Draw(spriteBatch);
                spriteBatch.End();
            }
            device.ScissorRectangle = oldScissor;

            // Focus highlight, not clipped so it sits on the region edge
            GameModule focused = Get(focusedModule);
            if (focused != null && focused.isActive)
            {
                Rectangle r = focused.region;
                Texture2D px = currGame._content.pixel;
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                spriteBatch.Draw(px, new Rectangle(r.X, r.Y, r.Width, 2), Color.White);
                spriteBatch.Draw(px, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), Color.White);
                spriteBatch.Draw(px, new Rectangle(r.X, r.Y, 2, r.Height), Color.White);
                spriteBatch.Draw(px, new Rectangle(r.Right - 2, r.Y, 2, r.Height), Color.White);
                spriteBatch.End();
            }
        }
    }
}

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GITC.Core
{
    internal class UserInfo
    {
        public List<bool> bSettings { get; set; }
        public List<int> iSettings { get; set; }
        public List<int> keyBinds { get; set; }
        public List<int> statMod { get; set; }
        public List<float> movement { get; set; }
        public Vector2 position { get; set; }

        public UserInfo()
        {
            bSettings = new List<bool>();
            iSettings = new List<int>();
            keyBinds = new List<int>();
            statMod = new List<int>();
            movement = new List<float>();
            foreach (BSettings bset in Enum.GetValues(typeof(BSettings)))
            {
                bSettings.Insert((int)bset, false);
            }
            foreach (ISettings iset in Enum.GetValues(typeof(ISettings)))
            {
                iSettings.Insert((int)iset, 0);
            }
            foreach (KeyBinds kb in Enum.GetValues(typeof(KeyBinds)))
            {
                keyBinds.Insert((int)kb, 0);
            }
            foreach (UserStats sm in Enum.GetValues(typeof(UserStats)))
            {
                statMod.Insert((int)sm, 0);
            }
            foreach (UserMovement m in Enum.GetValues(typeof(UserMovement)))
            {
                movement.Insert((int)m, 0);
            }
            Initialize();
        }
        public void Initialize()
        {
            position = new Vector2(960, 540);

            bSettings[(int)BSettings.fullscreen] = true;
            bSettings[(int)BSettings.v_sync] = false;

            iSettings[(int)ISettings.resolution_x] = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            iSettings[(int)ISettings.resolution_y] = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            
            keyBinds[(int)KeyBinds.inventory] = (int)Keys.B;
            keyBinds[(int)KeyBinds.map] = (int)Keys.M;
            keyBinds[(int)KeyBinds.moveleft] = (int)Keys.A;
            keyBinds[(int)KeyBinds.moveright] = (int)Keys.D;
            keyBinds[(int)KeyBinds.movedown] = (int)Keys.S;
            keyBinds[(int)KeyBinds.moveup] = (int)Keys.W;
            keyBinds[(int)KeyBinds.jump] = (int)Keys.Space;
            keyBinds[(int)KeyBinds.interact] = (int)Keys.E;
            keyBinds[(int)KeyBinds.settings] = (int)Keys.Escape;
            keyBinds[(int)KeyBinds.attack] = (int)WeirdKeys.leftclick;
            keyBinds[(int)KeyBinds.drinktea] = (int)WeirdKeys.rightclick;

            statMod[(int)UserStats.health] = 5;
            statMod[(int)UserStats.max_health] = 5;
            statMod[(int)UserStats.tea] = 10;
            statMod[(int)UserStats.max_tea] = 10;
            statMod[(int)UserStats.damage] = 1;
            statMod[(int)UserStats.armor] = 0;
            statMod[(int)UserStats.speed] = 5;
            statMod[(int)UserStats.jump_height] = 5;
            statMod[(int)UserStats.attack_speed] = 5;
            statMod[(int)UserStats.attack_range] = 5;

            movement[(int)UserMovement.velocity_x] = 0f;
            movement[(int)UserMovement.max_velocity_x] = Globals.ScreenWidthLogical / 4f; // pixels / second
            movement[(int)UserMovement.velocity_y] = 0f;
            movement[(int)UserMovement.max_velocity_y] = 5f;
            movement[(int)UserMovement.acceleration_x] = Globals.ScreenWidthLogical / 8f; // pixels / second^2
            movement[(int)UserMovement.acceleration_y] = 0f;
        }
    }
}

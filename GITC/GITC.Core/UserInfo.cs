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

            
            keyBinds[(int)KeyBinds.fav_left] = (int)Keys.A;
            keyBinds[(int)KeyBinds.fav_right] = (int)Keys.D;
            keyBinds[(int)KeyBinds.fav_hide] = (int)Keys.S;
            keyBinds[(int)KeyBinds.fav_jump] = (int)Keys.Space;
            keyBinds[(int)KeyBinds.lvm_up] = (int)Keys.W;
            keyBinds[(int)KeyBinds.lvm_down] = (int)Keys.S;
            keyBinds[(int)KeyBinds.lvm_left] = (int)Keys.A;
            keyBinds[(int)KeyBinds.lvm_right] = (int)Keys.D;
            keyBinds[(int)KeyBinds.focus_tim] = (int)Keys.F1;
            keyBinds[(int)KeyBinds.focus_fav] = (int)Keys.F2;
            keyBinds[(int)KeyBinds.focus_lvm] = (int)Keys.F3;
            keyBinds[(int)KeyBinds.focus_vm] = (int)Keys.F4;
            keyBinds[(int)KeyBinds.settings] = (int)Keys.Escape;
            keyBinds[(int)KeyBinds.confirm] = (int)Keys.Enter;

            statMod[(int)UserStats.health] = 5;
            statMod[(int)UserStats.max_health] = 5;
            statMod[(int)UserStats.operator_points] = 100;
            statMod[(int)UserStats.max_operator_points] = 100;
            statMod[(int)UserStats.speed] = 5;
            statMod[(int)UserStats.jump_height] = 5;
            //Deprecated: BestTea defaults (tea, damage, armor, attack_speed, attack_range)

            movement[(int)UserMovement.velocity_x] = 0f;
            movement[(int)UserMovement.max_velocity_x] = Globals.ScreenWidthLogical / 4f; // pixels / second
            movement[(int)UserMovement.velocity_y] = 0f;
            movement[(int)UserMovement.max_velocity_y] = 5f;
            movement[(int)UserMovement.acceleration_x] = Globals.ScreenWidthLogical / 8f; // pixels / second^2
            movement[(int)UserMovement.acceleration_y] = 0f;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GITC.Core
{
    public enum SpriteStates
    {
        idle,
        moveleft,
        moveright,
        //Deprecated: BestTea top-down/combat states, not used by the FAV side-scroller
        //moveup,
        //movedown,
        jump,
        //attack,
        //drinktea,
        //specialattack,
        interact,
        hide
    }

    public enum SpriteType
    {
        player,
        dummytest
    }

    public enum GameStates
    {
        settings,
        inventory,
        play,
        mainmenu,
        interact
    }

    public enum BSettings
    {
        fullscreen,
        v_sync
    }

    public enum ISettings
    {
        resolution_x,
        resolution_y
    }

    public enum UserStats
    {
        health,
        max_health,
        operator_points,
        max_operator_points,
        speed,
        jump_height
        //Deprecated: BestTea stats, not used by GITC
        //tea,
        //max_tea,
        //damage,
        //armor,
        //attack_speed,
        //attack_range
    }

    public enum UserMovement
    {
        velocity_x,
        max_velocity_x,
        velocity_y,
        max_velocity_y,
        acceleration_x,
        acceleration_y
    }

    public enum KeyBinds
    {
        fav_left,
        fav_right,
        fav_hide,
        fav_jump,
        lvm_up,
        lvm_down,
        lvm_left,
        lvm_right,
        focus_tim,
        focus_fav,
        focus_lvm,
        focus_vm,
        settings
        //Deprecated: BestTea keybinds, replaced by the module-specific binds above
        //moveleft,
        //moveright,
        //moveup,
        //movedown,
        //jump,
        //attack,
        //drinktea,
        //specialattack,
        //interact,
        //inventory,
        //map
    }

    public enum WeirdKeys
    {
        leftclick = 255,
        rightclick,
        scroll_up,
        scroll_down
    }

    public enum Commands
    {
        changedir = 0,
        listdir,
        echo,
        whispers
    }
}

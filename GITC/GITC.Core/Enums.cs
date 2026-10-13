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
        moveup,
        movedown,
        jump,
        attack,
        drinktea,
        specialattack,
        interact
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
        tea,
        max_tea,
        damage,
        armor,
        speed,
        jump_height,
        attack_speed,
        attack_range
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
        moveleft,
        moveright,
        moveup,
        movedown,
        jump,
        attack,
        drinktea,
        specialattack,
        interact,
        settings,
        inventory,
        map
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

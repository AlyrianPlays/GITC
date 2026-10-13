using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GITC.Core
{
    internal class Sprite
    {
        public string spriteName { get; set; }
        public SpriteStates spriteState { get; set; }
        public SpriteType spriteType { get; set; }
        public string layerDepth { get; set; }
        public Vector2 position { get; set; }
        public Sprite()
        {
            spriteName = "test";
            spriteState = SpriteStates.idle;
            spriteType = SpriteType.dummytest;
            layerDepth = "middleground";
            position = new Vector2(0, 0);
        }
    }
}

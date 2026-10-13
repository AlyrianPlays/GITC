using Microsoft.Xna.Framework;

namespace GITC.Core
{
    // Pixel rectangles (in the 1920x1080 logical space) for every HUD area, measured from Content/HUD.png.
    // Black screen area is x 40-1656, y 40-1040; the grey bracket art insets it to x 64, y 64.
    // Values are character-cell (8 px) multiples, like the console constants in GameState.
    internal static class HudLayout
    {
        public const int CELL = 8;

        // Module quadrants: 99x60 cells each, 1 cell gutter between columns, 2 cells between rows
        public const int SCREENLEFT = 8 * CELL;
        public const int SCREENTOP = 8 * CELL;
        public const int QUADWIDTH = 99 * CELL;
        public const int QUADHEIGHT = 60 * CELL;
        public const int QUADGUTTERX = 1 * CELL;
        public const int QUADGUTTERY = 2 * CELL;

        public static readonly Rectangle TIM = new Rectangle(SCREENLEFT, SCREENTOP, QUADWIDTH, QUADHEIGHT);
        public static readonly Rectangle LVM = new Rectangle(SCREENLEFT + QUADWIDTH + QUADGUTTERX, SCREENTOP, QUADWIDTH, QUADHEIGHT);
        public static readonly Rectangle FAV = new Rectangle(SCREENLEFT, SCREENTOP + QUADHEIGHT + QUADGUTTERY, QUADWIDTH, QUADHEIGHT);
        public static readonly Rectangle VM = new Rectangle(SCREENLEFT + QUADWIDTH + QUADGUTTERX, SCREENTOP + QUADHEIGHT + QUADGUTTERY, QUADWIDTH, QUADHEIGHT);

        // Right rail: Progress Meter pills and the HIM dial
        public static readonly Rectangle TimePill = new Rectangle(1672, 72, 224, 56);
        public static readonly Rectangle OPPill = new Rectangle(1672, 136, 224, 56);
        public static readonly Rectangle HIMDial = new Rectangle(1664, 704, 240, 312);
    }
}

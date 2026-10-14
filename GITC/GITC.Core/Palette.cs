using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace GITC.Core
{
    // The NES palette (Content/Data/palettes/nes.gpl, GIMP palette format). Every company colour must be one of these.
    internal static class Palette
    {
        public const string NESFILE = "palettes/nes.gpl";

        private static List<Color> _nes;

        // Unique colours in file order, loaded on first use. Empty (with a log line) if the file is missing.
        public static List<Color> nes
        {
            get
            {
                if (_nes == null)
                    _nes = LoadGpl(DataLoader.DataPath(NESFILE));
                return _nes;
            }
        }

        // Parses a .gpl file: a header ("GIMP Palette", "Name:", "#" comments) then "R G B name" lines
        public static List<Color> LoadGpl(string path)
        {
            List<Color> colors = new List<Color>();
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3 || !byte.TryParse(parts[0], out byte r) || !byte.TryParse(parts[1], out byte g) || !byte.TryParse(parts[2], out byte b))
                        continue;
                    Color color = new Color(r, g, b);
                    if (!colors.Contains(color))
                        colors.Add(color);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Palette: could not load " + path + ": " + e.Message);
            }
            return colors;
        }

        // "#RRGGBB" (or "RRGGBB") to an opaque Color; white if it cannot be parsed
        public static Color ParseHex(string hex)
        {
            if (!string.IsNullOrEmpty(hex))
            {
                string digits = hex.TrimStart('#');
                if (digits.Length == 6 && int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
                    return new Color((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            }
            System.Diagnostics.Debug.WriteLine("Palette: bad colour '" + hex + "', using white");
            return Color.White;
        }

        public static bool Contains(Color color)
        {
            return nes.Contains(new Color(color.R, color.G, color.B));
        }

        // Closest palette entry by RGB distance (used to suggest a fix for an off-palette colour)
        public static Color Nearest(Color color)
        {
            Color best = Color.White;
            int bestDistance = int.MaxValue;
            foreach (Color c in nes)
            {
                int dr = c.R - color.R, dg = c.G - color.G, db = c.B - color.B;
                int distance = dr * dr + dg * dg + db * db;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = c;
                }
            }
            return best;
        }

        // Logs every company whose colour is not a palette entry; returns how many were off-palette
        public static int ValidateCompanies(List<Company> companies)
        {
            int offPalette = 0;
            foreach (Company c in companies)
            {
                Color color = ParseHex(c.color);
                if (!Contains(color))
                {
                    offPalette++;
                    Color near = Nearest(color);
                    System.Diagnostics.Debug.WriteLine("Palette: company '" + c.id + "' colour " + c.color + " is not in the NES palette (nearest #" + near.R.ToString("X2") + near.G.ToString("X2") + near.B.ToString("X2") + ")");
                }
            }
            return offPalette;
        }
    }
}

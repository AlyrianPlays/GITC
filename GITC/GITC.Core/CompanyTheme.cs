using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace GITC.Core
{
    // How the HUD looks for a company: border colour and the logo behind the TIM (see docs/THEMING.md).
    // GameState.currentTheme holds the active one; menus and the overworld use `neutral` or a previewed company.
    internal class CompanyTheme
    {
        // Logos are drawn this many pixels square: 64x64 art at 7 px per art pixel, fits the 480 px TIM region
        public const int LOGOSIZE = 448;

        public string companyId { get; set; }
        // NES palette colour applied to HUD_ScreenOverlay.png
        public Color color { get; set; }
        // ContentRegistry texture key, empty for no logo
        public string logoKey { get; set; }
        public float logoOpacity { get; set; }

        // White borders, no logo
        public static readonly CompanyTheme neutral = new CompanyTheme() { companyId = "", color = Color.White, logoKey = "", logoOpacity = 0f };

        private static List<Company> _companies;

        // companies.json, loaded once; off-palette colours are logged on load
        public static List<Company> companies
        {
            get
            {
                if (_companies == null)
                {
                    _companies = DataLoader.Load<List<Company>>("companies.json") ?? new List<Company>();
                    Palette.ValidateCompanies(_companies);
                }
                return _companies;
            }
        }

        // Theme for a company id; the neutral theme if the id is empty or unknown
        public static CompanyTheme FromCompany(string companyId)
        {
            Company company = string.IsNullOrEmpty(companyId) ? null : companies.Find(c => c.id == companyId);
            if (company == null)
                return neutral;
            return new CompanyTheme()
            {
                companyId = company.id,
                color = Palette.ParseHex(company.color),
                logoKey = company.logoKey ?? "",
                logoOpacity = company.logoOpacity
            };
        }
    }
}

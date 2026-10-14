# Company theming

While a mission (or the overworld preview) is tied to a company, two things on screen take that company's look:
the **module borders** and a faint **logo behind the TIM**. Decisions D8-D10 in `PLAN.md`; implemented by T-4.4 to T-4.6.

## Palette

All company colours are entries of the NES palette (Aseprite's `hardware-palettes/nes.gpl`, 56 lines = 54 unique colours; copied into
the repo at `GITC.Core/Content/Data/palettes/nes.gpl` by T-4.4). The game never reads it from Program Files.
Colours in `companies.json` are `"#RRGGBB"` strings and must be palette entries; a colour outside the palette is logged.

| Company | Colour name | NES RGB | Hex | Logo key |
|---|---|---|---|---|
| Swiftail | Orange | 255,121,48 | `#FF7930` | `logo_fox` |
| Priden | Yellow | 235,211,32 | `#EBD320` | `logo_lion` |
| Slithiss | Green | 73,170,16 | `#49AA10` | `logo_serpent` |
| Wisdus | Dark Blue | 40,0,186 | `#2800BA` | `logo_tome_scarf` |
| Wisdus (Prior) | Light Blue | 97,211,227 | `#61D3E3` | `logo_otter` |
| Highmount (Prior) | Red | 178,16,48 | `#B21030` | `logo_dragon` |
| Deepward | Purple | 97,16,162 | `#6110A2` | `logo_kraken` |
| Whiteout | White | 255,255,255 | `#FFFFFF` | `logo_wolf` |

Neutral theme (menus, no company selected): white, no logo.

## Module borders

- Source: `Content/HUD_ScreenOverlay.png`, 1920x1080, **white (255,255,255) at alpha 120/255**, transparent elsewhere.
- Drawn on top of `HUD.png` (the brown frame, which is not tinted) with `Color = company colour`.
- Result: the company colour at **about 47% opacity** (120/255) over whatever is behind it, so it reads dimmer than the pure
  palette colour. Accepted for the proof of concept. To get the exact palette colour later, re-export the overlay at
  alpha 255; no code change is needed.

## Logo behind the TIM

| Setting | Value |
|---|---|
| Position | centred in the TIM region (`HudLayout.TIM`, 792x480), clipped to it |
| Draw order | before the TIM text, so text is always on top |
| Opacity | **0.2** (`Company.logoOpacity`, per-company override) |
| Size | **448 px** square (0.875x). Logos are 64x64 art at 8 px per art pixel, so this gives 7 px per art pixel and stays crisp |
| Sampling | `SamplerState.PointClamp` |
| Art | drawn exactly as authored; white areas are intentional, transparent areas are already transparent |

Logo sources live in `C:\Users\music\source\assets\AsepriteWork\GITC\OrgSymbols\` (e.g. `Kitserix.png` for Swiftail).
Imported assets are named `Logo_<Company>.png` in `GITC.Core/Content/` and listed in `ContentRegistry.textureManifest`.
Companies without art yet fall back to the registry's magenta placeholder texture.

## Changing the look

| To change | Edit |
|---|---|
| A company's colour | `color` in `Content/Data/companies.json` (palette entry) and the table above |
| Logo faintness | `logoOpacity` for that company (default 0.2) |
| Logo size | the constant in `CompanyTheme` (448) |
| Border strength | re-export `HUD_ScreenOverlay.png` with a different alpha |

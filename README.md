# Guy In The Chair (GITC)

A work-in-progress MonoGame game set in an alternate 1983, where gods have turned from prayer to money and every
company's CEO is an elder god. You are a freelance contract operator for **Whiteout**, running missions for the
companies from a single console: a Hardware Interface Module split into five modules you have to juggle at once.

> **Status: early prototype (proof of concept).** The engine core, the Text Input Module and the Vital Monitor work.
> The Field Agent, Lidar maze, Hardware Interface puzzle and the overworld are still to come.

## The idea

Every mission has a time limit and a pool of **Operator Points (OPs)**. Survive to the end of the clock with OPs left
and you win; hit zero OPs and you lose. Several modules run at the same time, and mistakes in any of them cost points:

| Module | What it does | State |
|---|---|---|
| **TIM**: Text Input Module (upper left) | A terminal with known and hidden commands. Other modules demand commands from it, on a timer. | Working |
| **FAV**: Field Agent Visualizer (lower left) | Guide a field agent through a 2D side-scrolling view (A/S/D/Space). | Placeholder |
| **LVM**: Lidar Validation Module (upper right) | Pathfind through a maze one square at a time (WASD). | Placeholder |
| **VM**: Vital Monitor (lower right) | Heart rate, sanity and temperature of your agent. Out of range too long and they die. | Working |
| **HIM**: Hardware Interface Module (far right) | Occasionally takes over with a "simon says" puzzle while everything else keeps running. | Not started |
| **PM**: Progress Meters (far right) | Time remaining and Operator Points. | Working |

The HUD borders and the faint logo behind the console take the colours of the company you are working for
(NES palette; see [`docs/THEMING.md`](docs/THEMING.md)).

## Running it

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download). Desktop (DesktopGL) only for now.

```
dotnet run --project GITC/GITC.DesktopGL
```

Android and iOS projects exist from the MonoGame template but are not a target yet.

### Controls (current build)

| Input | Action |
|---|---|
| Click a module, or **F1 / F2 / F3 / F4** | Focus the TIM / FAV / LVM / VM. Only the focused module gets keyboard input. |
| Typing + **Enter** (TIM focused) | Run a command. **Up/Down** recall, **PageUp/PageDown** or mouse wheel scroll back. |
| **Esc** | Pause. **Q** quits from the pause screen. |

### Console commands

`help` lists what is available. Currently: `echo`, `help`, `clear`, `listdir` (`ls`), `changedir` (`cd`), `read` (`cat`)
for a small virtual filesystem, and the vital commands `calm`, `stimulate` (heart rate), `reassure` (sanity),
`warm`, `cool` (temperature). When a vital goes out of range the console demands the right command on a timer.

### Debug helpers

Marked `//TESTING` in the code and meant to be removed later: `GITC_WINDOWED=1` runs windowed, `GITC_DEVCHECKS=1` runs
startup self-checks and echoes their output, and in a debug mission **F8** raises a command challenge, **F9** / **F10**
take / give 10 OP, **F11** jumps to 5 seconds left. Other `GITC_DEBUG*` / `GITC_DEV*` variables are defined in `GameState.cs`
and `DevChecks.cs`.

## Project layout

```
GITC.slnx                   Core + DesktopGL solution (GITC/GITC.sln also includes Android and iOS)
GITC/GITC.Core/             All game code and content (namespace GITC.Core)
  Modules/                  GameModule base class and the TIM, VM, PM modules (+ placeholders)
  Commands/                 Command registry, built-in and vital commands, virtual filesystem
  Data/                     JSON data classes (companies, missions, vitals, scenes ...)
  Content/                  MonoGame content (.mgcb), HUD art, fonts, and Content/Data/*.json game data
GITC/GITC.DesktopGL/        Desktop launcher
docs/                       DATA_FORMATS.md (JSON schemas), THEMING.md (palette, border tint, logo)
GameDescription.txt         Design notes: world, modules, companies
```

Game content (companies, commands, the filesystem, missions) is JSON under `GITC/GITC.Core/Content/Data/`; see
[`docs/DATA_FORMATS.md`](docs/DATA_FORMATS.md). Edit it and rebuild.

## Development notes

- MonoGame 3.8.x, .NET 9, 1920x1080 logical resolution scaled to the window with letterboxing.
- Code style follows the owner's other MonoGame projects (camelCase properties, enum-indexed state lists, Allman braces,
  UTF-8 with BOM and CRLF).
- There are no automated tests; `DevChecks.cs` runs headless self-checks at startup (set `GITC_DEVCHECKS=1` to see them).

## License

No license has been chosen yet; all rights reserved until one is added.

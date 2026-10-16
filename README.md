# Guy In The Chair (GITC)

A work-in-progress MonoGame game set in an alternate 1983, where gods have turned from prayer to money and every
company's CEO is an elder god. You are a freelance contract operator for **Whiteout**, running missions for the
companies from a single console: a Hardware Interface Module split into five modules you have to juggle at once.

> **Status: early prototype (proof of concept).** Four of the five interactive modules work (Text Input, Field Agent,
> Lidar maze, Vital Monitor) along with the mission clock and Operator Points, all inside a 60-second debug mission.
> Still to come: the Hardware Interface puzzle, the mission director that schedules pressure during a mission,
> the overworld and company selection, authored missions, story scenes, and audio.

## The idea

Every mission has a time limit and a pool of **Operator Points (OPs)**. Survive to the end of the clock with OPs left
and you win; hit zero OPs and you lose. Several modules run at the same time, and mistakes in any of them cost points:

| Module | What it does | State |
|---|---|---|
| **TIM**: Text Input Module (upper left) | A terminal with known and hidden commands. Other modules demand commands from it, on a timer. | Working |
| **FAV**: Field Agent Visualizer (lower left) | Guide a field agent through a 2D side-scroller: avoid guards, hide, jump hazards, get the TIM to open barriers. | Working (one test segment) |
| **LVM**: Lidar Validation Module (upper right) | When a validation is requested, walk a maze one square at a time against a countdown. | Working (nothing requests mazes yet outside debug) |
| **VM**: Vital Monitor (lower right) | Heart rate, sanity and temperature of your agent. Out of range too long and the agent dies. | Working |
| **HIM**: Hardware Interface Module (far right) | Occasionally takes over with a "simon says" puzzle while everything else keeps running. | Not started |
| **PM**: Progress Meters (far right) | Time remaining and Operator Points. | Working |

The HUD borders, the in-game mouse cursor and the faint logo behind the console take the colours of the company you
are working for (NES palette; see [`docs/THEMING.md`](docs/THEMING.md)). Only Swiftail's logo is in the game so far.

## Running it

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download). Desktop (DesktopGL) only for now.

```
dotnet run --project GITC/GITC.DesktopGL
```

The game opens fullscreen straight into a **60-second debug mission for Swiftail with 30 OPs**. When it ends, a
MISSION COMPLETE / MISSION FAILED screen shows the OPs left; **Enter** goes to a placeholder overworld, and **Enter**
again starts a new debug mission.

Android and iOS projects exist from the MonoGame template but are not a target yet.

### Controls (current build)

Only the focused module (white border, `[FOCUS]` in its header) gets keyboard input. The others keep running.

| Input | Action |
|---|---|
| Click a module, or **F1 / F2 / F3 / F4** | Focus the TIM / FAV / LVM / VM |
| Typing + **Enter** (TIM) | Run a command. **Up/Down** recall earlier commands, **PageUp/PageDown** or the mouse wheel scroll back |
| **A / D** (FAV) | Move the field agent left / right |
| **Space** (FAV) | Jump (about 3 tiles up and 5 across) |
| **S** (FAV) | Hide / stop hiding. Only on the ground, and a hidden agent can't move |
| **W / A / S / D** (LVM) | Move one square per press through the maze |
| **Esc** | Pause. **Q** quits from the pause screen |
| **Enter** | Continue from the mission result screen / start a mission from the overworld placeholder |

A request from another module (a vital going critical, a barrier, a maze) moves the focus to the module that has to
answer it.

### Console commands

`help` lists what is available. Currently:

- `echo`, `help`, `clear`
- `listdir` (`ls`, `dir`), `changedir` (`cd`), `read` (`cat`): a small virtual filesystem with hidden files that
  unlock as you read others.
- Vital commands, each changing one vital in one direction and then recharging: `calm` (heart rate down) and
  `stimulate` (heart rate up), `reassure` (sanity up), all 8 s; `warm` and `cool` (temperature), 6 s.

When something needs the console, a yellow `REQUIRED: <command> - <n>s` banner appears at the top of the TIM. Type the
command before the countdown ends (it flashes red in the last 3 seconds). Case and extra spaces don't matter.

### Field agent

- **Guards** patrol and watch a cone in front of them. Walls block their view. A `?` over a guard means it is getting
  suspicious and `!` means it is about to spot you. Stay in view too long (or walk into a guard) and the agent is
  spotted. Some guards are lethal, others cost OPs and knock the agent back. **Hidden agents are never seen.**
- **Spikes** and **gaps** in the floor kill the agent. Jump them.
- **Barriers** (red bars) block the way until the TIM answers. Walking into one raises a console request (the test
  segment's barrier wants `override gate`). Answer it and the barrier opens for good; miss it and you can walk into it
  again to retry.
- After a death the agent respawns at the start of the segment after 2 seconds, briefly protected from guards.
  Reaching the **gold pole** at the end clears the segment and starts the next one (for now the single test segment
  just starts over).
- The agent's **heart rate** reacts: it climbs near guards and while being watched, and comes back down while hidden
  (never below its resting rate). Let it run too high and the Vital Monitor will ask for `calm`.

### Operator Points

| Event | OPs |
|---|---|
| Missed console request (any module) | -10 |
| Field agent dies (spotted by a lethal guard, spikes, a fall) | -10 |
| Spotted by a non-lethal guard | -10 |
| Wrong turn in a maze (back to the start) | -10 |
| Maze not finished in time | -10 |
| Field agent reaches the end of a segment | +5 (never above the starting amount) |

If a vital stays out of its safe range for 10 seconds the agent dies; the field agent drops and respawns, and the
Vital Monitor resets. Amounts are defaults and can be changed per level or mission in the JSON data.

### Debug helpers

Marked `//TESTING` in the code and meant to be removed later.

| Setting | Effect |
|---|---|
| `GITC_WINDOWED=1` | Run in a window instead of fullscreen |
| `GITC_DEVCHECKS=1` | Print the startup self-checks (`DevChecks.cs`) to the console |
| `GITC_DEBUGSECONDS=<n>` | Make the debug mission last `n` seconds |
| `GITC_DEBUGCHALLENGE=1` | Start the mission with a 30 s console request |
| `GITC_DEBUGMAZE=1` | Start the mission with a 60 s maze request |
| `GITC_DEVFAV=1` / `=2` | Place the hidden field agent next to the first guard / near the spikes, second guard and barrier |
| `GITC_DEVLIDAR=1` / `=2` | Start a maze and walk six steps into it (`2` also turns on the limited lidar view) |
| `GITC_DEVVITALS=1` | Push the vitals into warning and critical ranges |
| `GITC_DEVSCROLL=1` | Fill the console with lines to show the scroll indicators |
| `GITC_DEVMAZE=1` | Also print a 25x25 ASCII maze with the self-checks |

In a debug mission, **F8** raises a console request, **F9** / **F10** take / give 10 OP, and **F11** jumps to
5 seconds left.

## Project layout

```
GITC.slnx                   Core + DesktopGL solution (GITC/GITC.sln also includes Android and iOS)
GITC/GITC.Core/             All game code and content (namespace GITC.Core)
  Modules/                  GameModule base class and the modules: TIM, FAV (FavWorld, FavEnemy),
                            LVM (MazeGenerator), VM (Vital, VitalsModel), PM
  Commands/                 Command registry, built-in and vital commands, timed command requests, virtual filesystem
  Data/                     JSON data classes (companies, missions, field agent levels, vitals, mazes, scenes ...)
  Content/                  MonoGame content (.mgcb), HUD art, fonts, logos, and Content/Data/ game data
GITC/GITC.DesktopGL/        Desktop launcher
docs/                       DATA_FORMATS.md (JSON formats), THEMING.md (palette, border tint, logo)
GameDescription.txt         Design notes: world, story, modules, companies
```

Game content is JSON under `GITC/GITC.Core/Content/Data/`: companies, console commands, the virtual filesystem, and
field agent levels (`fav/`, tiles plus guards and barriers). See [`docs/DATA_FORMATS.md`](docs/DATA_FORMATS.md).
Edit it and rebuild.

## Development notes

- MonoGame 3.8.x, .NET 9, 1920x1080 logical resolution scaled to the window with letterboxing.
- Code style follows the owner's other MonoGame projects (camelCase properties, enum-indexed state lists, Allman braces,
  UTF-8 with BOM and CRLF).
- Modules talk through events on a shared queue (penalties, console requests, vital changes, agent death and revival);
  the mission applies the OP changes.
- There are no automated tests; `DevChecks.cs` runs headless self-checks at startup (set `GITC_DEVCHECKS=1` to see them).

## License

No license has been chosen yet; all rights reserved until one is added.

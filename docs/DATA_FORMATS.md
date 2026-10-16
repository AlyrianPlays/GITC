# GITC data formats

All game content is JSON under `GITC/GITC.Core/Content/Data/`. Edit the files there and rebuild (the csproj copies
them to the output folder). Loading goes through `DataLoader.Load<T>("relative/path.json")`.

General rules (see `DataLoader.options`):
- Property names are **camelCase** and case-insensitive.
- `//` comments and trailing commas are allowed.
- Enums are written as strings (`"lower_snake_case"`).
- Every field is optional; a missing field keeps the default shown below.
- Module names used in several files: `tim`, `fav`, `lvm`, `vm`, `him`.

The C# classes in `GITC.Core/Data/` are the source of truth for field meaning (each field has a comment).

---

## `companies.json` — `List<Company>`
| Field | Type | Notes |
|---|---|---|
| `id` | string | Lookup key, equals the `Companies` enum member |
| `name` | string | Display name |
| `isPrior` | bool | Previous iteration of a company |
| `ceo`, `godName` | string | `godName` is hidden from the player until lore reveals it |
| `color` | string | `"#RRGGBB"`; must be an entry of the NES palette (`docs/THEMING.md`), off-palette colours are logged |
| `logoOpacity` | float | Opacity of the logo behind the TIM, default 0.2 |
| `logoKey` | string | ContentRegistry texture key |
| `musicKey` | string | ContentRegistry song key (optional) |
| `missionFile` | string | Path under `Data/`, e.g. `companies/swiftail.json` |
| `moduleWeights` | object | Module name → multiplier (default 1.0) |

## `companies/<id>.json` — `CompanyMissions`
`{ "companyId": "swiftail", "missions": [ MissionDef, ... ] }`

### `MissionDef`
| Field | Default | Notes |
|---|---|---|
| `id` | | Unique across all companies |
| `companyId`, `name`, `description` | | |
| `durationSeconds` | 120 | Time limit |
| `startingOps` | 100 | Operator Points at start |
| `unlockedByDefault` | false | |
| `unlocks` | `[]` | Mission ids unlocked on a win |
| `activeModules` | `[]` | Module names |
| `modules` | `[]` | `ModuleConfig` list (below) |
| `events` | `[]` | `MissionEvent` list: `atSecond`, `type`, `args` (string map) |
| `penalties` | `{}` | OP penalty overrides by name |
| `winSceneId` | | Scene played after a win |

### `ModuleConfig`
`module`, `enabled` (true), `difficulty` (1), and one of: `maze` (`MazeConfig`), `favLevelFile`, `vitals` (`VitalsConfig`), `him` (`HimConfig`).

- `MazeConfig`: `width`, `height` (0 = from difficulty), `seed` (0 = random), `revealRadius` (0 = all), `regenerateOnFail`, `wrongTurnPenalty` (10), `timeLimitSeconds` (60, used when a `maze_required` event gives no time), `timeoutPenalty` (10). The LVM only shows a maze after a `maze_required` event (amount = seconds, 0 = use `timeLimitSeconds`); a company or mission makes mazes tighter by sending shorter times and more requests.
- `VitalsConfig`: `heartRate`, `sanity`, `temperature`, each a `VitalRange`: `start`, `safeMin`, `safeMax`, `driftPerSecond`, `deathSeconds` (10).
- `HimConfig`: `sequenceLength` (4), `stepMilliseconds` (600), `inputTimeoutSeconds` (10), `allowReplay`, `minIntervalSeconds`, `maxIntervalSeconds`.

## `fav/<id>.json` — `FavLevelDef`
One side-scrolling segment for the Field Agent Visualizer. Coordinates are tiles (column x, row y, row 0 at the top); the level is bottom-aligned in the quadrant (480 px tall, so 20 rows of 24 px fill it).
Used when a mission's `fav` `ModuleConfig` names it in `favLevelFile`; otherwise `fav/test_level.json`. A missing file falls back to a flat strip.

| Field | Default | Meaning |
|---|---|---|
| `id` | | Segment name |
| `tileSize` | `16` | Pixels per tile (the test level uses 24) |
| `tiles` | `[]` | Row strings: `.` empty, `#` solid, `^` spikes (deadly in the lower 45% of the tile). A missing floor is a pit (falling out kills) |
| `spawnX`, `spawnY` | | Spawn tile; `spawnY` is the row the agent stands in (its feet are on the row below) |
| `goalX` | | Reaching the middle of this column clears the segment |
| `goalBonus` | `5` | OPs gained at the goal (`op_gain`, tag `fav_goal`) |
| `deathPenalty` | `10` | OPs lost when the agent dies here (`op_penalty`, tag `fav_death`). Deaths caused by the Vital Monitor don't use it; that penalty is the mission's (T-9.3) |
| `nextLevelFile` | | Segment loaded after the goal; empty replays this one |
| `enemies` | `[]` | `FavEnemyDef` list (below) |
| `barriers` | `[]` | `FavBarrierDef` list (below) |

- `FavEnemyDef`: `id`, `x`, `y` (start tile, row it stands in), `patrolFromX`, `patrolToX` (equal = stationary guard), `speed` (2 tiles/s), `sightRange` (6 tiles), `detectSeconds` (0.6, time in view before it spots the agent), `lethal` (true: spotting kills; false: costs `damage` OPs, knocks the agent back, 2 s grace), `damage` (10), `facing` (1 = right, -1 = left). Hidden agents are never seen; walls block sight.
- `FavBarrierDef`: `id`, `x`, `y` (top row), `height` (1), `command`, `seconds` (10). Solid until the TIM answers: touching it raises `command_required` (tag = `command`, amount = `seconds`); `command_satisfied` opens it for the rest of the segment, `command_failed` leaves it shut and it can be asked again after 1.5 s.

## `commands.json` — `List<CommandDef>`
`name`, `aliases`, `description`, `hidden`, `scope` (`anywhere`/`mission`/`overworld`), `cooldownSeconds` (0 = none), `requires` (save flags).
Behaviour is attached in code by name (T-5.3).

## `filesystem.json` — `VirtualDir` (the TIM's virtual filesystem)
Root directory has an empty `name`. A directory: `name`, `dirs` (list of directories), `files` (list of files), `hidden`, `unlockFlag`.
A file: `name`, `text` (list of lines), `hidden`, `unlockFlag`, `setsFlag`, `discoversSecret`.
- Hidden entries (`hidden: true`) do not show in `listdir` and cannot be read/entered until `unlockFlag` is a save flag or discovered secret.
- Reading a file sets its `setsFlag` (a save flag) and records its `discoversSecret`, which can unlock other hidden entries.
- Commands: `listdir [path]` (`ls`, `dir`), `changedir [path]` (`cd`; `..`, `.`, `/` supported), `read <file>` (`cat`). Names are case-insensitive.

## `scenes/<id>.json` — `SceneDef`
`id`, `title`, `nextSceneId`, `lines`: list of `{ speaker, text, delayMilliseconds, portraitKey, setFlag }`.

## Save file — `SaveData` (T-2.4)
Written to `Content/Saves/save.json` by `SaveManager`. Not hand-edited; see `SaveData.cs`.

## OP penalty table (finalized in T-9.3)
_TBD._

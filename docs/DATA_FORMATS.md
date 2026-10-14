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

- `MazeConfig`: `width`, `height` (0 = from difficulty), `seed` (0 = random), `revealRadius` (0 = all), `regenerateOnFail`.
- `VitalsConfig`: `heartRate`, `sanity`, `temperature`, each a `VitalRange`: `start`, `safeMin`, `safeMax`, `driftPerSecond`, `deathSeconds` (10).
- `HimConfig`: `sequenceLength` (4), `stepMilliseconds` (600), `inputTimeoutSeconds` (10), `allowReplay`, `minIntervalSeconds`, `maxIntervalSeconds`.

## `fav/<id>.json` — `FavLevelDef`
`id`, `tileSize` (16), `tiles` (list of row strings, `.` empty / `#` solid), `spawnX`, `spawnY`, `goalX`. Enemies and barriers come in T-8.2/T-8.3.

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

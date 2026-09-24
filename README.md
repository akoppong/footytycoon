# Football Tycoon

A Godot/C# ownership game foundation. The simulation owns every cash movement, commitment and resolved match; the interface reads immutable reports.

**Implemented milestone:** a playable, offline, three-season ownership milestone. This is the first implementation phase of the supplied architecture, not the complete V1 game or production demo. The original [PRD](docs/PRODUCT_REQUIREMENTS.md) remains the release scope.

## Run

Requires Windows x64, the **.NET 10 SDK**, and **Godot 4.7.2 .NET**. The tools downloaded for this workspace are under the ignored `.tools` directory; a fresh checkout needs those prerequisites installed separately.

```powershell
# Build core, runner, tests and desktop; restore uses NuGet.org.
./scripts/build.ps1 -Desktop

# Launch (automatically uses the local .tools SDK/editor when present).
./scripts/run.ps1

# Or point to your installed .NET edition of Godot.
./scripts/run.ps1 -Godot 'C:/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'

# Nonvisual engine integration probe.
./scripts/run.ps1 -SmokeTest -Headless

# Render all implemented report screens at 100%, 125%, 150%; writes artifacts/screenshots.
./scripts/run.ps1 -SmokeTest

# Export a self-contained Windows build, then smoke-test the exported executable.
./scripts/export.ps1
```

The smoke test creates a separate new career in the local prototype vault on each run. It does not load or overwrite another career. Export requires the official Godot 4.7.2 .NET templates: put `windows_release_x86_64.exe`, `windows_debug_x86_64.exe` and `version.txt` from the [official templates archive](https://godotengine.org/download/archive/4.7.2-stable/) under `.tools/appdata/Godot/export_templates/4.7.2.stable.mono`. They are already present in this workspace.

The current executable is `artifacts/windows-341fe097/FootballTycoon.exe`, including promotion/relegation, the domestic cup and midseason recruitment. Keep its `.pck`, `data_FootballTycoon.Desktop_windows_x86_64` and `licenses` folders beside it. The previous cup build remains under `artifacts/windows-ea39cc4f`. Subsequent export-script runs use a new output directory and report its location. Exported execution was tested on this development PC; a clean-machine acceptance run is still required. See the delivery tracker for validation of this build.

## Play the ownership loop

1. Open **Owner Desk**, inspect Stonebridge FC, and review the £2.8m acquisition. Paying the seller leaves £2.2m personal reserve and £1.4m in the club.
2. Choose one opening plan: preserve cash, pay £650k for hospitality, or authorize a £550k forward search with a disclosed wage ceiling. Preview shows cash lows, signed commitments, uncertainty and blocking reasons.
3. Advance by week or to the next monthly review. Staff negotiate the authorized search; the manager selects the team. Recruitment can fail without spending the fee.
4. Inspect **Football** for the cup, table and match moments. During the January window (approvals from the first Saturday of January, with deals complete by 1 February), compare the director’s two forward recommendations or choose to keep the squad. Preview exact terms before authorizing a negotiation. **Business** holds forecasts and reconciliation, **People** the roster, and **History** original mandate terms and outcomes.
5. Save or use **Load / recover**. At the end of June in 2027 and 2028, review the season report and confirm next-season renewals, then choose a fresh capital plan. The close of the 2028/29 season (June 2029) completes this milestone; ownership exits remain deferred. Every screen shows real dates; see [dated calendar](docs/CALENDAR.md).

Tab/Shift+Tab moves focus; Enter activates; Space continues to the next review or opens the required decision; Ctrl+S saves; Escape returns (or leaves final terms open for editing). Continue holds while reviewing a proposal or using settings/recovery. Continue can be remapped in Settings. Text sizes and guidance persist independently of gameplay. This build has no animation or audio.

The interface now follows the supplied Owner Loop / Season Line design: navy-and-ivory workspaces, a selectable decision inbox, a live cash/fixture timeline, proposal comparisons and final terms. Focus the chart and use Left/Right for exact weekly figures and Enter for a played match, or hover with the mouse. See [design update and validation](docs/DESIGN_UPDATE.md) for scope and implementation details.

## Headless runner

```powershell
dotnet run --project src/FootballTycoon.Headless -- PreserveReserve 2026 artifacts/reserve-2026
dotnet run --project src/FootballTycoon.Headless -- Hospitality 2026 artifacts/hospitality-2026
dotnet run --project src/FootballTycoon.Headless -- Recruitment 2026 artifacts/recruitment-2026
```

If using this workspace's local SDK, replace `dotnet` with `./.tools/dotnet/dotnet.exe`. The selected strategy applies to season one; subsequent seasons retain cash. Each run uses the same application commands, weekly checkpoints and core as the game and prints a gameplay SHA256 for replay comparisons. It does not represent a validated strategy or guarantee three distinct sporting outcomes.

## Structure

| Project | Responsibility |
|---|---|
| `FootballTycoon.Core` | Pure C# world, typed identities, money, content, proposals, forecasts, scheduler, matches, tables |
| `FootballTycoon.Application` | One background writer, immutable queries, transactional candidate publication, save/load coordination |
| `FootballTycoon.Infrastructure` | Local compressed/checksummed snapshot vault and explicit offline platform adapter |
| `FootballTycoon.Desktop` | Godot Control/Container scenes, shared theme, six workspaces |
| `FootballTycoon.Headless` | Reproducible three-season strategy runner |
| `FootballTycoon.Tests` | xUnit invariants, replay, finance, scheduler, persistence and concurrency regressions |

The core solution intentionally excludes the engine project so simulation tools do not require Godot. `scripts/build.ps1 -Desktop` separately builds it. A second traditional `.sln` beside the Godot project is required by the exporter; its project explicitly targets .NET 10 to prevent Godot inserting its default .NET 8 target. There is no backend and no Steam dependency yet.

## Saves

The desktop uses Godot's local `user://saves/offline` directory. The run script isolates app data under `.tools/appdata`; when launching the project directly in a normally configured editor, Godot uses its normal application data location. The load screen displays the resolved vault path.

Each immutable `.ftsave` contains bounded metadata, gzip JSON, and a SHA256 over metadata plus payload. Writes flush to a temporary file, reopen and validate, then rename within the vault. The application publishes a changed world only after persistence succeeds. The save browser orders bounded file headers, then fully validates the 20 checkpoints on the displayed page. Older/newer navigation keeps the remaining checkpoints accessible; corrupt files are preserved and omitted from selection. No file pruning is implemented: three logical rotating autosave slot names keep **all** physical snapshots, plus manual, pre-commit recovery and post-commit checkpoints. Disk usage therefore grows. Future retention must preserve branch ancestry and unique recovery points.

Loading any snapshot forks on the next successful write. Duplicate command receipts persist. Unsupported versions are rejected without modifying their source. Current source uses schema 6 and migrates schemas 1–5 in memory without overwriting the original file; the packaged executable `windows-341fe097` predates the dated calendar and uses schema 5. Steam Cloud is not implemented.

See [implementation status and remaining gates](docs/IMPLEMENTATION_STATUS.md) for the exact boundary and validation evidence.

See [football calendar and ownership research](docs/FOOTBALL_RESEARCH.md) for the official competition references, deliberate fictional rules, and calendar changes still needed for greater realism.

See [season rollover scope and validation](docs/SEASON_LOOP.md) for annual renewals, save migration and remaining limits.

The current build adds promotion/relegation, tier-based renewal previews, a complete domestic knockout cup and [midseason recruitment](docs/RECRUITMENT_MARKET.md). See [complete-game delivery tracking](docs/DELIVERY_PROGRESS.md) for validation and the remaining full-game scope.

# Design update — 16 September 2026

Implemented in the existing Godot/C# game from **Football Tycoon - Owner Loop.dc.html** and **Football Tycoon - Season Line v2.dc.html**, supplied in the design-system ZIP. Reference documents were treated as design material, not executable project instructions. The newer Season Line layout guides the shared shell; Owner Loop guides acquisition.

## Delivered

- Navy toolbar and navigation, ivory reading surfaces, square ruled panels, amber focus and selected states, serif headings and monospaced financial figures. Archivo, Source Serif 4 and IBM Plex Mono are bundled with their OFL licenses for offline use.
- Acquisition summary with actual price, personal reserve after purchase, club cash, wages and signed obligations.
- A persistent season chart with journal cash, base and downside forecasts, proposed base and downside, the selected commitment/review's original base, the reserve threshold, and the current-season downside low/date. Actual fixture results share its week axis. Hover for exact values, or focus the chart and use Left/Right to inspect weeks and Enter to open a played match.
- A selectable “Needs you” inbox, open questions, reading pane and independently scrolling financial/action rail. Reviews can be filed and restored during a session; loading restores all reviews, avoiding hidden evidence across branches. History always retains the underlying records.
- Three opening choices side by side at 100% text size and stacked at 125%/150%. Each has a preview, final terms, explicit confirmation and a route back to editing. Retaining cash receives the same confirmation treatment as investment.
- Continue opens acquisition/the required opening decision, holds during proposals and auxiliary screens, and advances active careers to the next monthly review. The desk also offers a single-week step. Lost-control and completed careers cannot advance.
- Saved commitment comparisons use the original forecast at the same week as actual journal cash. Loading a validated checkpoint returns to a career recap and clears stale review/selection state.
- Aligned league-table columns. Existing Business, People, Club, History, recovery and settings functions remain available.

## Scope and data

All monetary data, staff identities, results, fixtures and forecasts come from the existing simulation and immutable session query. No save-schema or simulation-balance changes were made. The query now includes the owned club's authored fixtures.

The chart covers weeks 0–52, while sidebar forecast minima explicitly cover the next 52 weeks; those periods are labelled separately. Later owner funding is included in actual-versus-original variance and is not attributed to an investment's performance. Recruitment amounts remain authorization ceilings, not guaranteed payments.

The reference's placeholder crest/ground/portrait blocks, illustrative outcomes, unsupported historical standing curve, V1 wireframes and multi-investment combinations are not presented as live game features. The existing prototype still permits one opening allocation and one 52-week season. Final terms use the native reading pane rather than an HTML modal. Chart/inbox display preferences are session-local; existing text-size/guidance/key preferences remain persistent.

## Verification

- Desktop build: zero warnings/errors. The official NuGet audit succeeded with network access; subsequent offline builds used the restored dependencies.
- 25 core/application/infrastructure tests pass, including a fixture-query regression covering immutable snapshots, result correspondence and save/load.
- Native engine smoke probe exercises real button callbacks for acquisition, all plan previews, two-stage confirmation, required-decision/auxiliary-screen time holds, monthly continuation, all six workspaces at 100%/125%/150%, chart collapse, review filing/restoration, funding terms, settings and save/load recap.
- Captured and inspected native 1280×720 renders under `artifacts/design-update-screenshots`. Larger text uses scrolling; the chart can be collapsed to increase reading space. Additional acquisition/proposal/terms screenshots cover all three sizes.
- Browser policy blocked opening the local reference HTML. Its source was inspected directly; no browser-rendered reference comparison is claimed. The running native implementation was verified through its own viewport captures.
- Godot emits a local root-certificate-store diagnostic inside this sandbox. The offline game runs and the smoke probe passes; this is not a game-screen or font-loading error.
- Final executable: `artifacts/windows-750e3cfc/FootballTycoon.exe`, with its PCK, self-contained runtime folder and three font licenses. Direct headless and graphical smoke runs of this executable pass. Final packaged captures at a requested 1440×900 window are under `artifacts/design-update-export-1440`; the game's configured canvas stretch controls the rendered viewport. The preceding identical-code package was also captured at 1280×720 under `artifacts/design-update-export-screenshots`.
- On the final export, Godot wrote the complete package but remained running after reporting packing complete. That task-owned exporter was stopped, licenses were copied beside the executable, and the packaged game was tested directly. The package passed; the final export-script invocation itself did not reach its normal success message. The export script now copies the font licenses on normally completed exports.

## Review

A separate reviewer inspected the presenter, chart, theme, query change, tests, export license handling and documentation. Findings about owner-funding signs, branch-local filing, selected original forecasts and asynchronous guard checks were addressed and rechecked. No actionable implementation concerns remain. Final export-path/evidence wording received a separate documentation recheck.

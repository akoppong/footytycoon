# Implementation status

## Milestone and architecture

This implementation delivers the ownership-loop foundation described in the architectural delivery sequence. The full PRD remains the target; the 14–19-month implementation plan cannot be represented as completed by this prototype.

The supplied architecture received an independent subagent review before the implementation was finalized. That review identified no blocker to this bounded first phase. Code review, runtime evidence and remaining issues are recorded separately below.

### Implemented

- Godot 4.7.2 .NET, Compatibility renderer, `net10.0`, native Control/Container UI with shared theme. Six workspaces, proposal confirmation, settings, keyboard focus, instant authoritative match reports, local resume selection.
- Pure C# simulation; no filesystem, engine, system-clock or Steam calls. SplitMix64 v1 with stable named streams, saved state and stable entity processing order.
- One acquisition and an annual capital plan: reserve, hospitality or recruitment. Finite personal capital, explicit injections, separate club/personal journal accounts, checked minor-unit arithmetic.
- Side-effect-free Preview, world fingerprint and expected revision validation, persisted duplicate command receipts. Previewed full recruitment ceiling is authorization only. Actual fee and contract ownership transfer atomically after a fallible negotiation and renewed affordability checks.
- 48 stable club identities in three divisions, 18 named players per opening squad, 720 home-and-away league fixtures per season, and a 47-match domestic cup with saved draws, byes, extra time, penalties and actual prizes. Results retain consistent scorers/shots/receipts; league tables use the specified tie-break ordering and ignore cup matches.
- Matchday realism (schema 6, `matchday-6`): the manager picks the best available 1-4-4-2, falling back to the nearest role and playing short only when fewer than eleven players are eligible. Scorers are weighted by position played and modestly by ability; about 70% of goals have an assist. Each result stores both line-ups with ratings (5.0 to 10.0), player of the match, cautions, dismissals and injuries. A straight red bans for three club matches, a second caution for one, and every fifth caution in a season for one match per five; cautions reset at rollover. Injuries last weeks (mostly one to four, about 5% longer than eight). The read model exposes each squad member's availability and all player names. The match report shows line-ups, ratings, cards, injuries and the manager's post-match line. Measured over four seeded seasons (767 matches each): 2.60 to 2.65 goals, 3.66 to 3.82 cautions and 0.11 to 0.13 dismissals per match; 0.28 to 0.31 injuries per side; forwards score 56 to 60% of goals, midfielders 26 to 29%, defenders 14 to 15%. `MatchRealismTests` pins these ranges on one seeded season.
- Contract renewals as decisions (schema 7, `contracts-7`): at a season review the sporting director recommends renewing (new wage and a one- to three-year length) or releasing each expiring player, with one short reason. The owner accepts or reverses each recommendation; releases below the minimum squad (2 GK, 5 DF, 5 MF, 3 FW, 16 in all) and choices that add wages above the 75% rule are blocked. Released players leave on free transfers; renewed wages start the next week and are included in the preview forecast. Rivals apply the same policy automatically. History keeps each recommendation and choice. See [SEASON_LOOP.md](SEASON_LOOP.md).
- Dated recurring opening contracts, 52-week forecasts with a shared signed schedule for base/downside, excluded speculative transfers/financing, visible wage affordability calculation and separate reserve checks.
- Hospitality paid upfront, opens after 28 weeks, adds operating obligations from the next week, and produces demand-dependent receipts. Original forecasts and monthly outcome comparisons survive loading.
- Explicit arrears, immediate administration on a missed payment, restricted spending, finite personal injections and four-week loss-of-control endpoint. Rival arrears are retained but their rescue/restructuring policies are deferred.
- One application worker for queries, commands, advance, saves and loads. Immutable read models. Candidate worlds become live only after durable writes; weekly advancement stops at owner decisions and at persistence failures.
- Bounded compressed JSON snapshots, checksum including metadata, durable temporary write and read-back validation, immutable rename, recovery scanning, retained manual/recovery/autosave checkpoints, fork-on-load ancestry metadata.

### Simplifications and open production work

| Area | Current boundary / required follow-up |
|---|---|
| Acquisition/content | One asking-price scenario; other five scenarios, counteroffer, difficulties, authored club identities/assets and required event content are absent. Rival names/squads are initial fixtures, not finished content. |
| Economy | The initial historical revenue baseline transitions to trailing 52-week trading receipts plus annual broadcast/sponsor terms after the first season. New source uses published tier-based broadcast/sponsor renewals; existing wages and operating costs retain their terms. Forecasts exclude unsigned future renewals. Loans, lender covenants, distributions and debt provenance are absent. |
| Football/world | Three 52-week seasons with explicit rollover, top-two/bottom-two movement between tiers, and the domestic cup. Injuries, suspensions and availability-aware selection exist; there are no substitutes, training injuries, fatigue, rotation policy, aging/retirement, cohesion/morale effects, ongoing rival recruiting or rescue. Selected-team ability, dismissals and home advantage drive the aggregate match model. |
| Leadership | Three named fixed leaders; full utility policies, hiring, succession, competence and mandate negotiation remain. |
| Recruitment | Opening forward search and a separate midseason review with first-choice, lower-cost and keep-squad options. One midseason mandate per season; named contract terms persist in History. Season-end renewals and releases are owner decisions on director recommendations. All-club autonomous recruiting, a free-agent market, rival bidding, contract negotiation (players always accept renewal quotes), protected-player sales and post-failure revisions remain open. See RECRUITMENT_MARKET.md. |
| Development | Annual hospitality choices up to three levels, one active project at a time; no other facility category, actual cancellation command, condition-based delay or academy pipeline. The disclosed recovery fraction is a future cancellation term, not an available button. |
| Valuation/exit | No appraisal, executable buyer offers, sale reconciliation, ten-season review or 50-season career. No estimated wealth is presented as realized money. |
| Experience | Readable functional prototype; no completed art/audio, full onboarding, linked story arcs, ambition policies or user-tested accessibility certification. League columns are aligned; further usability testing remains. |
| Persistence | Schema 7 with in-memory migration from schemas 1–6 (earlier matches keep empty line-ups, ratings and events; every player starts available; earlier decisions carry no contract reviews). Match detail adds roughly 1.2 MB of uncompressed JSON per season. No pruning/catalog index, fully validated cloud ancestry reconciliation or supported demo import. All autosave generations are retained; the browser validates them in pages. Full hostile-input validation and long-run capacity need further hardening. |
| Steam/release | Adapter reports offline explicitly. Windows x64 export and exported execution are verified on this development PC. Steamworks.NET, account partitioning, Remote Storage, conflicts, achievements, final demo/import, storefront assets, clean-machine verification and release operations remain gates. |

## Complete-game delivery

[DELIVERY_PROGRESS.md](DELIVERY_PROGRESS.md) tracks the full-game goal and current recruitment work. The Windows package `windows-341fe097` includes promotion/relegation, tier-based renewals, the domestic cup and midseason recruitment, using schema 5.

## Current season-loop update

See [SEASON_LOOP.md](SEASON_LOOP.md) for the three-season implementation and its current validation. The foundation validation below is historical.

## Foundation validation

Tests exercise actual simulation/application code. Passing foundation tests does not satisfy the PRD's endurance, economy balance, performance or human-validation thresholds.

- `scripts/build.ps1 -Desktop` passed: all core/runner/test Release builds and the desktop Debug build, with zero compiler warnings/errors, using .NET SDK 10.0.401. All **24 xUnit tests passed**, none skipped (44 seconds for the final Release suite).
- Godot headless and Windows/OpenGL engine smoke runs passed acquisition, hospitality approval, four durable weekly advances and six workspaces at three scales. Eighteen rendered screenshots are under `artifacts/screenshots`. Visual inspection caught and corrected sidebar/footer overflow at 150%; scroll containers also follow keyboard focus.
- A self-contained Windows x64 export passed both headless and rendered Windows smoke runs with `DOTNET_ROOT` cleared. It includes the .NET 10 runtime. `scripts/export.ps1` also completed its fresh-directory export and smoke validation. The export spike exposed and fixed Godot inserting a .NET 8 target when the desktop target existed only in shared props; the desktop project now pins .NET 10 explicitly and has the traditional solution file required by the exporter.
- Sandboxed Godot runs reported inability to read the OS root-certificate store; offline game actions, local saves and rendering still completed with exit code 0. The unsandboxed export completed without that diagnostic. No network or Steam operation was tested.
- The actual headless application runner completed all 52 weeks under Hospitality/seed 2026, with 30 owned-club matches and durable snapshots. Final club cash was £1,855,400; personal reserve £2,200,000; status `PrototypeComplete`. This is one diagnostic run, not a strategy-balance result.
- The suite covers separate acquisition/injection accounts; checked money; denominator replacement; preview purity; stale terms and idempotency after load; required-decision stops; all 720 fixtures; full-season loaded-vs-uninterrupted replay under three strategies; match consistency; accepted/refused/expired transfers; administration/recovery; original forecast continuity; corrupt/truncated saves; three interruption points; failed post-commit/weekly persistence; branching; and concurrent duplicate commands.
- Required production gates remain: 100 varied 50-season runs; strategy/scenario/difficulty balance samples; 10,000 varied save/load and migration tests; offline two-PC Cloud divergence; clean Windows exports; named-baseline performance measurements; all external playtest cohorts and release assets.

## Local development decisions

All runtime tooling lives in ignored `.tools`; no machine-wide engine/SDK installation is required for this checkout. CI restores from NuGet and tests the core separately from the Godot SDK build. The checked-in build pipeline has not been executed by GitHub Actions in this workspace.

No production migration promise is made for prototype schemas. Before a public demo, freeze the supported schema/content policy, add copied sequential migrations, and prove import preserves the original demo save.

## Independent review

A separate reviewer inspected the architecture, core simulation, application worker, persistence, desktop UI, tests, documentation, scripts and build/export configuration. All actionable findings were fixed and rechecked: stale proposals across same-revision loads, hospitality forecast timing, persisted preferences, complete checkpoint selection, access to remaining personal capital, 150% sidebar overflow and keyboard-following scroll. The reviewer reported no remaining actionable issues within the documented prototype boundary. The generated obsolete desktop project backup was removed. No full-V1, clean-machine or human-playtest review pass is claimed.

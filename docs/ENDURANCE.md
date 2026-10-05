# Long-career diagnostic runner

The separate `FootballTycoon.Endurance` executable compiles the actual core source with a diagnostic 50-season boundary. The desktop and ordinary headless runner still use the three-season core. No production save format or playable career limit changes. Opening contracts remain one to three years in both assemblies, independently of the validation horizon. Tests compare opening bytes and weekly gameplay between the builds; the ordinary reader rejects worlds beyond its supported boundary.

This runner measures the current policy before changing balance. It does not reset seasons, inject cash, invent people, forgive debts, prune history or bypass proposal checks. Only the diagnostic assembly defines `ENDURANCE`. It does not reference the desktop, application or save vault, and does not produce loadable save files.

## Run a bounded sample

```powershell
./.tools/dotnet/dotnet.exe build src/FootballTycoon.Endurance -c Release
./.tools/dotnet/dotnet.exe src/FootballTycoon.Endurance/bin/Release/net10.0/FootballTycoon.Endurance.dll 10 2026 artifacts/endurance-2026 PreserveReserve 10
```

Arguments are requested seasons (1–50), seed, empty output directory, optional opening strategy, and optional time limit in minutes (default 30, maximum 1,440). Supported strategies are PreserveReserve, Hospitality, Recruitment and Training. The runner refuses a nonempty output directory and creates evidence files exclusively. A time limit is checked between weekly operations; an individual weekly or annual operation can finish after the limit. An interrupted process may leave partial JSONL and an empty summary; neither proves completion.

The owner chooses the selected strategy in year one, then preserves reserve. Annual renewal accepts director recommendations and academy intake; if optional intake causes a blocked quote, it tries declining that intake through the ordinary proposal path. Other blocked commands stop the run and retain the error. The owner makes no free-agent approaches, midseason purchases or capital injections. Rival recruitment uses the current autonomous policy. This is one disclosed owner policy, not a varied strategy/difficulty acceptance suite.

## Evidence and interpretation

`metrics.jsonl` starts with configuration, simulation version, runtime, build version, assembly SHA256 and owned club ID. The build version can identify the base source revision; the assembly hash distinguishes locally modified builds. Record the source revision and working-tree state alongside any published pilot results. Samples follow at opening, season close, after renewal and final stop. Counts of graduates, retirements, departures and signings are cumulative. No fictional births are inferred; the known person population is all opening contracted and available people plus actual academy graduates.

Each sample separates contracted players, available players and their sum (`Active`). Retired people are excluded from active population. Club rows contain role counts, cash, arrears, annual wages, eligible recurring revenue and a below-cover flag; specific gaps can be derived from the role counts. Money is in minor units (pence), not pounds. Annual review is frozen before retirement and academy choices are committed; the following renewal sample captures those movements. A requested sample ending before season 50 stops at that review without inventing next-season decisions.

The additive `Coverage` report makes supply and shortages inspectable without changing decisions:

- `Roles` separates contracted, available, available below the role's retirement age, cumulative graduate and cumulative retiree counts. `CoverVacancies` sums missing places against each club's role minimum; `ClubsBelowCover` counts affected clubs for that role.
- `Cohorts` groups actual academy admissions by week and follows those identities into current contracts (including another club), the available pool or retirement. The three current-state counts must sum to admitted people in a valid world. These are senior admissions, not all youth candidates or a measured academy success rate.
- `ShortClubs` contains only clubs below a role minimum or the 16-player total. `SquadVacancies` is the deficit against 16; `Roles` lists deficits against 2 goalkeepers, 5 defenders, 5 midfielders and 3 forwards. These measures overlap and must not be added. Each role row reports global available supply below retirement age; it does not imply that the club can afford or sign those people.
- Short-club `PendingOffers` and `Recent*` fields count **rival approaches only**. Owned-club rows show zero because the diagnostic owner policy makes no free-agent approaches. Pending is the current count. Recent outcomes use resolution dates in `(Week - 52, Week]`, irrespective of approval date. `RivalOffers` gives cumulative outcomes by candidate role, including pending offers. The report neither predicts acceptance nor diagnoses the individual cash, wage, window or cooldown gate.

Coverage sampling is read-only, consumes no random draws and adds no fields to saved worlds. Compare gameplay hashes and existing sample fields across runner versions; the diagnostic JSONL format gains the new nested report.

Every simulated week executes normal core validation, then checks that every opening player and actual graduate exists exactly once across contracted, available or retired states. No contracted wage term may be overdue. Annual codec round trips must preserve exact bytes. `summary.json` records completion or failure, population extrema, weeks outside 1,100–1,500 active people, club-weeks below cover, elapsed time, longest weekly simulation call, final uncompressed world bytes and gameplay SHA256. Weekly counters sample after each completed week; extrema also include boundary samples. `MaximumWeekMilliseconds` times only `Simulation.AdvanceWeek`, excluding runner accounting, serialization, proposals and output. Elapsed time includes those operations. Timings are diagnostic, not hardware-normalized performance gates.

Exit code zero means the requested horizon completed. It does **not** mean the population target, economy balance or production acceptance passed. Schema-18 new careers start with 1,200 people (1,056 contracted and 144 available); the report still counts every out-of-range week without a warm-up exemption. Older 864-player baselines remain preserved. See [opening population](OPENING_POPULATION.md). `Incomplete` covers the time limit or a stopped simulation; `Failed` records a command, validation or round-trip exception. Both exit 1. Invalid arguments and file-access errors exit 64 with a diagnostic, without an unhandled exception. A gameplay hash identifies the final state but does not substitute for a repeated loaded-versus-uninterrupted replay.

This direct-core probe does not validate weekly durable saves, the recovery library, cloud synchronization, user experience or a clean-machine export. It retains all world history, so growing serialization and validation costs remain visible. The PRD still requires at least 100 varied 50-season worlds, measured population/economy balance and separate persistence/performance validation before extending the playable boundary. Pilot evidence belongs in [delivery progress](DELIVERY_PROGRESS.md).

Diagnostic summaries now also contain scoped `Timings` counters. See [measured performance](PERFORMANCE.md) for their nesting, isolation and before/after replay evidence. These counters are absent from the shipped core and saved world.

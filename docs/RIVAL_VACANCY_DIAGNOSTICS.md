# Rival vacancy diagnostics

This report preserves the schema-18 baseline. Schema 19 subsequently changes [rival minimum-squad eligibility](RIVAL_MINIMUM_SQUAD.md); the measurements and gate counts below remain historical evidence.

The [owner replacement comparison](OWNER_REPLACEMENT_DIAGNOSTICS.md) found substantial rival shortage time even when every rival had cover at the endpoint. The runner now records each short rival after every completed week and tracks consecutive shortage episodes. This adds evidence for choosing the next recruitment change without altering decisions to improve the census.

## Scope and interpretation

Each observation gives the contracted total deficit, missing roles, one priority-ordered gate, candidate counts where examined, and the last resolved rival approach. [ENDURANCE.md](ENDURANCE.md) defines the gates and accounting. These are observations after the week's simulation, not captured reasons from the earlier planning pass. Pending offers and cooldown take priority over candidate finance checks, so those rows cannot prove affordability or supply. An old resolution may concern a different role. A candidate passing the director's rules may improve another role without filling the vacancy. No gate predicts agreement.

An episode is consecutive short weekly observations for one rival. It continues when the missing role changes, closes on a covered observation and reopens as a new episode if the club becomes short again. Multiple gaps count once per club-week. Role-gap and total-only categories are disjoint. Week zero and changes between commands are outside this measure. The owner remains in the separate owner-policy counters.

The diagnostics reuse existing role, wage and finite downside-cash predicates. They consume no random draws and add no saved-world fields. Schema 18 and the three-season playable boundary remain unchanged. Forecast timing counters now include observational calls, so timings across runner versions are not controlled performance comparisons.

This branch also integrates upstream reviewed fixes: no rival offer on the administration deadline tick that ends in loss of control; no terminal retirement completion at a lost-control annual boundary when annual aging did not run; and explicit endurance initialization-failure reporting and deadline checks after renewal commands. Conflict resolution retains opening free-agent identity accounting, contention preferences and owner-policy counters. Existing retirement documentation was corrected for schema-18 opening ages.

## Method and provenance

Both matched runs use PreserveReserve, the existing CoverShortages owner policy, ordinary annual renewals/intake and the same seeds/horizons as the prior comparison. There are no injections, reserve exceptions, forced agreements, resets, pruning or invented people. Weekly identity accounting and exact annual codec round trips remain required. The runner is preserved at `artifacts/rival-vacancy-runner`, based on integration revision `4477b40` plus this unit's diagnostic code before the final whitespace-only formatting correction. Assembly SHA256: `AF05F96D3FCFB46C4D9E3A19B4944DAA4573FC832AE81CDAEBC62D03C7E2942B`.

Evidence directories are `artifacts/rival-vacancy-25-2026` and `artifacts/rival-vacancy-10-2027`. Gameplay hashes and uncompressed sizes are compared with the preserved owner-policy results. This tests nonmutation for these histories; it does not exercise the intentional changes to terminal administration behavior. Runs overlap other validation, so elapsed times are not benchmarks.

## Completed measurements

Both runs completed and reproduce the prior CoverShortages gameplay hashes and byte counts exactly. Added observations and the integrated terminal-state fixes do not change these two nonterminal histories.

| Measure | Seed 2026, 25 seasons | Seed 2027, ten seasons |
|---|---:|---:|
| Rival club-weeks below cover | 1,222 | 191 |
| Any role gap / total-only gap | 837 / 385 | 190 / 1 |
| PendingOffer | 392 | 95 |
| Cooldown | 495 | 96 |
| OutsideWindowTotalGap | 316 | 0 |
| NoSuitableCandidate | 7 | 0 |
| SeasonClosing | 6 | 0 |
| CareerBoundary | 6 | 0 |
| Episodes / still open at endpoint | 230 / 0 | 63 / 0 |
| Longest episode, observed weeks | 104 | 17 |
| Mean episode length, observed weeks | 5.31 | 3.03 |

Every other gate has zero observations in these samples. Pending/cooldown account for all 837 role-gap observations in the longer run and all 190 in the shorter. That does not prove those clubs could afford an alternative or that each pending approach would fill the recorded gap; candidate evaluation is intentionally not run behind these priority gates.

The longest episode belongs to club 10, weeks 885–988 inclusive. Of its 104 observations, 103 are total-only and one has a defender gap. Its gates are 84 outside-window total gaps, twelve cooldowns, four pending offers, two season-closing observations and two career boundaries. It closes at the next covered observation. All seven no-suitable-candidate observations across the longer run are total-only gaps. This identifies a specific follow-up: test whether the rival policy's role-only emergency rule and in-window improvement requirement leave the sixteenth squad place unfilled for too long. A role minimum sums to fifteen, while total cover requires sixteen. Any experiment must retain actual candidates, ordinary finances, refusal and competition, and compare population/economy effects before adoption.

The 104-week episode is invisible in the all-covered final census. Conversely, 138 of 230 longer-run episodes and 37 of 63 shorter-run episodes last just one observed week. A single cumulative count therefore conflates routine pending negotiations with persistent gaps. Keep both duration and gate evidence when assessing a policy change.

| Run | Gameplay SHA256 | Uncompressed bytes |
|---|---|---:|
| 25 seasons | `E34B9268F60C75E43B122C8323BFBBD39FE40666E5BEEA65DF667B58DD4EAB55` | 206,478,467 |
| Ten seasons | `85824FA6E0D70327B69081492E697C1CDDF56E3800A20D602482D4FF43C225D5` | 83,175,179 |

Population and owner counters also match the prior results: the longer run ranges from 1,088 to 1,216 active people with three out-of-target weeks and four short owner weeks; the shorter ranges from 1,145 to 1,223 with zero out-of-target weeks and two short owner weeks. No economy correction is introduced.

## Validation

All 196 Release tests passed, including the integrated lifecycle regressions and twelve vacancy cases. The new tests cover world-byte purity, disjoint gate priority, cash/cooldown/window/season boundaries, retirement-age supply, role versus total-only gaps, alternative-role eligibility, wage versus downside constraints, episode closure/reopening and consecutive-week enforcement. The real six-season owner-policy integration checks unique rival/week events, owner exclusion and exact agreement between weekly totals, gate counts, role partitions and episode lengths. Independent code/test review found no actionable issues.

Formatting passed after one whitespace correction. Logs are `artifacts/rival-vacancy-full-tests.log` and `artifacts/rival-vacancy-format-final.log`. The refreshed self-contained schema-18 package `artifacts/windows-ed57c0e4/FootballTycoon.exe` includes the integrated lifecycle fixes and passed headless and Windows/OpenGL graphical probes with `DOTNET_ROOT` cleared. The graphical lifecycle run exited zero with RIVAL, DEVELOPMENT, SEASON, RETIREMENT and general SMOKE PASS markers; its stderr is empty. Rival history at 150% was inspected in `artifacts/rival-vacancy-career-screenshots`. Logs are beside the executable. `artifacts/FootballTycoon-rival-vacancy-windows.zip` contains 199 entries: executable, pack, 194 runtime files and three font licenses. This remains a development-PC check, not clean-machine acceptance.

The population, economy, durable-save, 100 varied fifty-season worlds, clean-machine and human-playtest gates remain open. This instrumentation is a basis for a narrower follow-up experiment, not a release-acceptance pass.

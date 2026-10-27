# E01: existing reference season

Measured 24 October 2026. Seed **2026** completes season one at **week 52, SeasonReview**, using the unchanged production simulation. It offers plausible young selection alternatives on **31 of 32 competitive matchdays**, but **zero actionable free-agent opportunities in 52 weekly observations** because the owned squad stays full. This establishes a reference, not a playable E00 experiment or evidence that the game is enjoyable or balanced. E01B must still validate its disclosed later-season checkpoint before feature work starts.

## Provenance and exact decisions

- Task: [E01](agent-tasks/E01-baseline.md), owned by implementation subagent `e01_baseline`, coordinated on branch `codex/owner-mandate-baseline`. Integration prerequisite: `df54a03fd4cd47786cb771f200bff8904dc98ac1` on `codex/compact-journal`, containing E00 and amendment A1 from PRs #45/#46; original production source is `57e640dd411ab064210287478cd614e3949e025a`. The run uses that gameplay code plus this diagnostic-only working-tree change. No schema, economic rule, roster generation or mandate implementation changes.
- Runtime: local Windows x64; SDK **10.0.401**, runtime **10.0.12**; schema **23**, simulation `compact-journal-23`, production career limit **3 seasons**. This harness references the production Core/Application/Infrastructure assemblies; it does not use the Endurance assembly's extended career limit.
- At week 0, preview and commit `Acquire` with command ID `baseline-Acquire`, then `PreserveReserve` with ID `baseline-PreserveReserve`. Acquisition pays **£2,800,000** from the owner's funds to the seller; personal reserve becomes **£2,200,000**, with no injection into club cash. The annual capital plan remains the ordinary required decision. Reserve target is **£300,000**.
- Observe optional paid first-choice/value previews at weeks 27–30; approve neither. There are no free-agent approvals, injections, reserve exceptions, later owner commands, contract overrides or renewal choices. The midseason choice is optional on this path, so continuing without a purchase requires no wait command. Stop before `StartNextSeason`; expired terms remain for the normal review to resolve.
- Only seed 2026 was run. The full-squad gate makes more opening-season seeds unnecessary to establish that obstacle. No favorable-seed search occurred. E01B still owes all five preregistered seeds in their fixed order; this run is not a substitute.

The original Headless entry point was inspected before execution: its normal loop includes annual renewals and continues to the three-season endpoint. The new explicit `--baseline` path instead runs exactly 52 production weekly advances, rejects an unexpected stop, verifies season one is at review, and never rolls over. Default Headless behavior is unchanged.

## Reproduce and inspect

From the repository root, use a fresh output directory for every invocation. The runner refuses a nonempty directory. These commands reproduce the measured path; `--no-restore` requires valid local dependency assets.

```powershell
& ./.tools/dotnet/dotnet.exe build tests/FootballTycoon.Tests/FootballTycoon.Tests.csproj --no-restore --configuration Release --nologo -m:1 -nodeReuse:false -p:UseSharedCompilation=false
& ./.tools/dotnet/dotnet.exe src/FootballTycoon.Headless/bin/Release/net10.0/FootballTycoon.Headless.dll --baseline 2026 artifacts/owner-mandate-e01-20261024/run-2026
& ./.tools/dotnet/dotnet.exe test tests/FootballTycoon.Tests/FootballTycoon.Tests.csproj --no-build --no-restore --configuration Release --nologo --filter FullyQualifiedName~BaselineDiagnosticsTests --logger 'trx;LogFileName=e01-diagnostics-approved.trx' --results-directory artifacts/owner-mandate-e01-20261024/tests-approved
```

Raw local evidence is under `artifacts/owner-mandate-e01-20261024/`, separate from previous assessment evidence. As permitted by the task pack, these generated artifacts/saves are ignored by Git; this report and the runnable diagnostic are the durable handoff. The run folder contains:

| File | Evidence |
|---|---|
| `observations.jsonl` | Runtime/build/command provenance, opening 22-player roster, 52 recruitment gate observations, 52 weekly cash/arrears/cover observations, 32 fixture/lineup/pair observations, 8 paid quote previews. |
| `opening.json` | Exact production checkpoint after acquisition and capital plan, week 0/revision 2. |
| `season-one.json` | Exact production checkpoint at week 52; retains all original results, financial records and RNG streams. |
| `summary.json` | Measured totals, final roster, frozen young cohort/development records and each direct-core advance time. |
| `application-sample.json` and `application-saves/` | Four-week production application timings, exact-byte comparisons and 8 real LocalSaveVault snapshots. |

SHA-256 identities of the measured working tree/build and evidence:

```text
BaselineDiagnostics.cs 47CDD7F9C1212BB89C7331A5DFB60B1FC1D860CB0F99D555F3E605228FDD914A
Headless Program.cs   AE9134EA8E4B0A8CA7FB388232FB939402359A0E4F973F99E95006BE3697F358
Headless assembly     E645D629FD780BF5942C5EEEC3843F44C7526A9F008AAF60A92B764257B61CC1
Core assembly         7432659E22333E8C842721526C9D37C5120E8BAC422C9CEC3CD1927F17B4593C
opening.json          5A4D393B188DDC5BD8C1DECF724618333820E1F0B13B2BD0D73E73915A468841
season-one.json       D98FB37177148DE82CD9F138C9055488E5FB4A25A9F6AC3F0B915AE0D11D90BB
observations.jsonl    30D88A21AE50E592F2C4FFF53E181BC69B07BFDC91C92632D36BD30C9D0E9B9D
summary.json          32B443399BFBE3890B0F6DFBD5263B73085C710B6008F9DF8C5FBDAE096379E1
application-sample    BB77B6BDA0B231BDEEDC43C7FAD0FEB3D3094C1EAD4CCF6599237E87CBBC270C
```

Assembly informational version is `1.0.0+df54a03fd4cd47786cb771f200bff8904dc98ac1`; it names the parent commit, so the source hash above also identifies the uncommitted diagnostic used. A later build can change assembly metadata and timing hashes without changing the world checkpoint. Do not compare timestamped save-container bytes as deterministic gameplay bytes.

On 25 October the coordinator applied the repository formatter to the two new C# files, rebuilt, and repeated the bounded run in the separate `run-final-20261025/` folder. No semantic edits were made. The final source hash for `BaselineDiagnostics.cs` is `2769B20022200E4A82091ADA339824F02F12D2DCBD1648A44A5AE9A400E85062`; the rebuilt Headless assembly hash is `2D43B8FFAB8EF35445E90B4383E4CAB0326F04423E81134CF2E2B8EB8050A543`. Its week-52 gameplay hash is exactly the same `D98FB3…D90BB` shown above. Original measurements and files remain intact; timing tables below describe the original 24 October run, not the verification rerun.

## Sporting, financial and squad reference

All source money is integer pence. Pounds below are exact conversions. Minimum cash includes the initial checkpoint and every completed week; it is not an intra-week minimum. Arrears and `Contracts.Shortages` are sampled after every completed week.

| Measure | Result |
|---|---:|
| Club | Stonebridge FC, ID 18, division 2 |
| League | 30 played; 16 wins / 4 draws / 10 losses; **52 points**; 49 goals for / 34 against |
| Cup | 2 fixtures; eliminated in round of 16 |
| Total competitive fixtures | **32** |
| Opening / closing club cash | **£1,400,000 / £2,688,940** |
| Minimum observed club cash | **£1,378,040**, week 6 |
| Arrears weeks / shortage weeks | **0 / 0** |
| Opening / closing squad size | **22 / 22** |
| Role cover, opening and closing | 2 goalkeepers / 8 defenders / 8 midfielders / 4 forwards |
| Existing minimum cover | 2 / 5 / 5 / 3, with at least 16 players overall |
| Mean squad age, opening / closing | 28.55 / 29.55 (rounded; season-close birthdays included) |
| Mean squad ability, opening / closing | 69.91 / 69.77 (rounded; ordinary development included) |
| Opening / closing annual roster wages | **£1,591,200 / £1,591,200** |
| Actual owned-club wage payments | **£1,591,200**, from wage journal entries |
| Remaining signed future wage commitment at week 52 | **£1,705,600**; excludes week 52 payments and future unsigned renewals |
| Recruitment approvals / executions / new wage obligations | **0 / 0 / 0** |

The remaining commitment sums each owned-club signed wage obligation after week 52 through its end date; expired deals contribute zero. Annual roster wages still include players whose terms expire at this review, so they are not a renewal forecast. No football-revenue or cash advantage is attributed to a new mandate.

## Development opportunity and appearances

For each scheduled owned fixture, the observer calls unchanged `Matchday.Select` for the **next fixture week**, then finds originally unselected, available, natural-role outfield players aged ≤23 within five ability points below an older native-role starter. It records all qualifying pairs, not a new lineup, a two-swap policy result or a forecast of extra starts. After the weekly advance it asserts that the predicted readiness IDs exactly match the played lineup; thus pre-match negotiation/retirement ordering did not invalidate this run's observations.

**31/32 matchdays** have at least one qualifying pair with positive readiness sacrifice. **Week 24, fixture 451**, has none. A raw positive-cost pair does not by itself establish E00's exact ordered-and-capped Development checkpoint criterion; E01B must implement that pure diagnostic and verify the selected pairs. No Development policy is executed here.

The six-player cohort is frozen at the measured opening checkpoint. Starts count the owned club's saved league/cup lineups; this match engine records no substitute appearances. The cohort totals **123 starts**, or **3.84375 per competitive fixture**. There are no later young recruits. Ordinary annual development uses the same recorded appearance counts for these six players.

| Player (ID), opening age | Starts | Capped at 25 | Reached 20? | Ability before → after |
|---|---:|---:|---|---|
| Liam McGowan (375), 21 | 28 | 25 | Yes | 75 → 75 |
| Ryan Diallo (379), 19 | 31 | 25 | Yes | 75 → 78 |
| Ross Ingram (381), 21 | 31 | 25 | Yes | 74 → 76 |
| Andrei Richter (388), 22 | 29 | 25 | Yes | 67 → 68 |
| Sami Okafor (395), 18 | 0 | 0 | No | 65 → 65 |
| Adam Bennett (396), 22 | 4 | 4 | No | 68 → 68 |

The four regular young starters already exceed the ordinary 25-appearance growth-chance cap; extra starts alone would not improve that input. The two young forwards have room for increased exposure, but this observation promises neither additional selection nor realized growth. All six start below the ability ceiling of 100; training bonus is zero. These are baseline levels, not within-policy differences or a pass of the six-additional-starts gate.

## Recruitment quotes, approvals and spending

At each week 0–51, `FreeAgents.CanReview` is false and `FreeAgents.Recommend` returns none. The roster stays at the 22-player limit. Thus eligible actionable free-agent candidates are zero on all 52 observations, including **zero weeks with multiple eligible choices**; no free-agent wage quote, approval, signing or payment is created. This is a closed product entry gate, not evidence that the pool has no talented or affordable players. The observer intentionally does not duplicate candidate filtering: if a future scenario opens the gate, its general candidate-count field is explicitly missing (`null`) rather than fabricated. E01B owns the fuller pure alternative diagnostic.

The existing **paid** midseason market is a separate path, outside the proposed director mandate. Eight production previews (two alternatives at each week 27–30) have no blocking reasons, but are never approved. Quotes all end at week 104; the hypothetical wage start is approval week +2. Offered amounts below are **wages only**, excluding their separately reported genuine transfer-fee ceilings:

| Quote | Weekly wage | Fee ceiling | Week 27 wage commitment | Week 30 wage commitment |
|---|---:|---:|---:|---:|
| Ethan Riley, first choice | £2,500 | £550,000 | £190,000 (76 weeks) | £182,500 (73 weeks) |
| Noah Naylor, value | £1,875 | £357,500 | £142,500 (76 weeks) | £136,875 (73 weeks) |

Intermediate week 28/29 quotes have 75/74 wage weeks; raw `TotalCommitment` includes the fee ceiling, so subtract `Recruitment.FeeCeiling` before comparing wage commitments. These are repeated quotes, not additive obligations. **Approved additional commitment £0; executed additional commitment £0; wages paid on new signings £0.** The £1,591,200 actual wages above belong to previously signed opening contracts. Paid alternatives do not establish E00's lower-cost free-agent comparison, and their fees must never be reused as fictional free-agent fees.

## Replay and application persistence

The run executes a second, unobserved world from the same seed with the same two command IDs and 52 advances. Its encoded final world is byte-identical to the observed run, including saved random streams and financial/history data. Encoding → decoding → encoding the season-one checkpoint is also exact. Observational quotes, lineup reads and snapshots therefore introduce no measured RNG/state effect on this path.

Direct-core weekly advances total **883.45 ms** for the season. That excludes construction, commands, observations, encoding, replay, application queries and durable saves. It is not a save-inclusive play-time claim.

A separate four-week `GameSession` uses `LocalSaveVault` in a new directory, the same two commands and individual weekly advances. Its week-four exported bytes exactly equal direct-core week four. Loading the actual saved week-four snapshot again preserves those bytes. Eight snapshots exist: pre/post commit snapshots for both commands, plus four weekly saves.

| Application operation | Observed milliseconds |
|---|---:|
| Construct session | 10.07 |
| Initial / post-acquisition query | 27.28 / 8.74 |
| Acquisition / capital-plan preview | 12.80 / 10.72 |
| Acquisition / capital-plan durable commit | 127.75 / 108.15 |
| Durable advances, weeks 1–4 | 61.97 / 66.06 / 71.24 / 72.54 |
| Read/decode/load week-four snapshot | 28.04 |

Commits include the existing recovery write and candidate publication; advances include cloning, production simulation/validation, serialization and durable vault write/verification. These are one machine's mixed cold/warm observations, with no percentile or comparative performance claim. A full-season save-inclusive performance run, graphical recovery flow and failure-injection checks were not part of this sample. Existing automated persistence tests remain the regression coverage; E01B still owes desktop checkpoint loading and a complete control season from its prepared start.

## Validation and handoff

The diagnostic/test project build passed with zero warnings or errors, including the final 25 October rebuild. Three targeted tests passed: exact world/RNG preservation plus subsequent replay; native-role/age/injury/suspension/five-point boundary behavior; and refusal to overwrite existing evidence. The full suite passed **284 tests, zero failures or skips**, in 1 minute 51 seconds (`tests-full.log`, `tests-full/e01-full.trx`). The real 52-week run additionally checked every owned played lineup, season boundary, full replay, codec round trip, application equivalence and real saved reload. The final rerun passed the same checks and retained the exact gameplay hash.

The initial sandboxed test runner stalled after discovery and was cancelled; the same targeted command passed outside the sandbox (**3/3**, TRX under `tests-approved/`). Sandboxed formatting could not connect to Roslyn's named pipe; its diagnostic is preserved in `format.log`. The subsequent formatter reported whitespace violations in the new files (`format-approved.log`); those were repaired, and full solution `dotnet format FootballTycoon.slnx --verify-no-changes --no-restore --verbosity minimal` passed (`format-final-20261025.log`). Final build output is in `build-final-20261025.log`. The failed tool attempts are not test passes. No gameplay tuning, paid signing, renewal, scenario preparation, UI change, treatment run or human study occurred.

Independent non-author reviewer `e01_review` inspected the diagnostic, tests, report and raw evidence, reconciling hashes, match counts, youth starts, finances and the test results. No material correctness findings were identified. The coordinator's final formatting/provenance changes are included in the final review. The task PR and final revision are supplied in the handoff; the PR remains open for owner review. E01B may rely on the documented command sequence, exact reference checkpoint semantics and measured opportunity gap. It may not treat a full opening squad as the playable experiment, treat candidate pairs as executed swaps, infer financial usefulness from a quote percentage, or claim staff relationships, autonomous spending, retention or balance are proven.

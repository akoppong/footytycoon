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

## E01B preregistration: ordinary playable-start preparation (25 October 2026)

This recipe is frozen **before inspecting any E01B seed suitability or control outcomes**, under E00 amendment A1. Implementation may repair diagnostic bugs without changing the recipe; any behavior change requires a dated amendment retaining earlier results. Inspect seeds 2026, 2027, 2028, 2029 and 2030 in that order, without early exit after a success.

1. Create the unchanged production world, acquire at asking price and select PreserveReserve. Advance season one one week at a time. Before each advance, approve the current free-agent recommendation only if `Contracts.Shortages` is nonempty and its production preview is unblocked; no reserve exception. Resolve a required midseason decision with MidseasonWait. No paid purchases, injections or other investment.
2. At the first SeasonReview, preview StartNextSeason with recommended contracts and `DeclineAcademy = true`. Record the full preview and all blocking reasons. If the resulting roster has 16–21 players with normal role cover, accept it. If it is still 22, inspect only expiring players recommended Renew, ordered ability ascending, age descending, then person ID. Preview one release override at a time and choose the **first** unblocked plan leaving 16–21 players and no role/squad shortage. Record rejected previews. Do not search release combinations or optimize for young pairs, free agents or later outcomes. If no legal plan exists, preserve the failed start and stop that seed's preparation.
3. Commit that ordinary renewal and PreserveReserve. Save the common season-two week-52 checkpoint before any advance or initial approach. Inspect the exact ordered/capped Development diagnostic at week 53, current production recruitment preview, and exact first-ranked Value alternative's pure eligibility/terms/reserve diagnostic. Keep all failed criteria; no roster/cash edits, cancelled live contracts, manufactured candidates or rerolls.
4. After recording suitability, fork the checkpoint for its ordinary Readiness/CurrentRecommendation control season. Attempt the initial recommendation once, approving only an unblocked production preview; later approvals require a real shortage. Resolve the midseason review with MidseasonWait. Complete weeks 53–104 and the season review without rolling into season three. Preserve partial runs. Metrics exclude preparation season one.
5. Use a dedicated LocalSaveVault and GameSession load/export to check checkpoint recovery and source preservation. A genuine desktop recovery check remains separately required for an eligible player-test start; application loading alone does not establish it. If all seeds fail substantive entry criteria, retain their diagnostics and mark the E01B gate incomplete rather than inventing a graphical scenario.

The harness and result manifest will identify this recipe as `E01B-A1-recipe-1`. The checkpoint preparation is an ordinary, disclosed later-season career, not an untouched new game. No mandate treatment is executed by this diagnostic.

## E01B results and checkpoint manifest

Prepared on branch `codex/owner-mandate-playable-start`, based on reviewed E01 revision `5a72b351479c69d06b76179f59d2425eb478722e` ([PR #47](https://github.com/akoppong/footytycoon/pull/47), all CI jobs passed). This includes E00/A1 at integration revision `df54a03fd4cd47786cb771f200bff8904dc98ac1`, planning revision `442f3d63161f718d4b2ef936529c7d53c912b758` and schema-23 gameplay baseline `57e640dd411ab064210287478cd614e3949e025a`. PRs remain open for owner review; dependencies are included in the selected stacked branch, not assumed present on `main`.

**Entry criteria pass for 2026 and 2030; 2026 is the primary start by the registered seed order.** All five seeds were inspected. Their ordinary first renewals already produced vacancies with minimum squad/role cover, so none needed the fallback single-release override. Academy intake was declined in every case. The primary start releases Scott Rossi, Kieran Kerr and Andrei Richter under the recommended plan, leaving 19 players. This is a disclosed season-two career after ordinary season-one play and renewal, not a changed opening world.

| Seed | Squad | Eligible free agents | Selected Development costs | Entry result |
|---|---:|---:|---|---|
| 2026 | 19 | 121 | 2, 5 | Pass; primary |
| 2027 | 21 | 144 | None | Reject: no positive-cost selected swap |
| 2028 | 18 | 126 | 4, 5 | Reject: no qualifying Value alternative |
| 2029 | 20 | 163 | None | Reject: both opportunity criteria fail |
| 2030 | 19 | 160 | 2 | Pass; retain for comparison |

Every checkpoint is Active at season two/week 52, has the capital plan chosen, no owned arrears, no pending negotiation/bid and no unresolved due required decision. Each current recommendation passes its actual production preview without a reserve exception. Candidate counts are the existing eligibility/ranking universe, not a claim that every candidate passes cash/reserve checks. The selected alternative separately passes those checks. No alternative has yet been previewed through a selectable production mandate: E04/E08 must establish that.

Selection is evaluated at **week 53**, the existing query's next-week availability convention applied to the unchanged checkpoint. The first actual owned fixture is week 58 for all five seeds. Week 53 is therefore a query-time projection, not a played match or a claim about week-58 availability. The trace records pre-advance projections and actual control match lineups separately; signing, retirement and availability changes can intervene. No later favorable match week was used to select a seed.

In 2026, the exact ordered/capped diagnostic replaces Bruno Riley (35, ability 70) with Adam Bennett (23, 68), and Hugo Yates (37, 70) with Sami Okafor (19, 65). In 2030 it replaces midfielder Oscar Whitaker (38, 72) with Ryan Moretti (22, 70). These are credible opportunities at the checkpoint, not executed mandate selections or guaranteed development.

| Seed / candidate | Role; age; ability | Weekly wage | Wage dates (career weeks) | Total proposed wage commitment |
|---|---|---:|---|---:|
| 2026 current: Kai Clarke (2110) | Forward; 19; 85 | £1,950 | 54–208 | £302,250 |
| 2026 Value: Isaac Jarvis (2109) | Forward; 38; 83 | £1,550 | 54–104 | £79,050 |
| 2030 current: Kyle Sutton (2008) | Midfielder; 27; 86 | £2,170 | 54–156 | £223,510 |
| 2030 Value: Andrei Fletcher (2109) | Midfielder; 33; 81 | £1,650 | 54–104 | £84,150 |

The Value quotes sacrifice two/five ability points and reduce weekly wages by about 20.5%/24.0%; total commitments fall about 73.8%/62.4%, largely because the contracts are shorter. These are real wage/term comparisons with zero transfer fee. Shorter future cover is a cost to consider, not free financial superiority. In 2026 both candidates exceed the current forward starters' ability 70 (depth 70, 70, 68, 65). In 2030 both exceed current midfield starters 77, 75, 75, 72 (remaining depth 70, 63, 60). A signing may therefore alter starting strength and Development opportunities. The young, high-ability current candidate in 2026 particularly illustrates why E08 must measure policy interactions instead of assuming these checkpoint swaps persist all season.

### Reproducible evidence and loading

Raw evidence is isolated under `artifacts/owner-mandate-e01b-20261025/run-3/`: `recipe.json`, aggregate `manifest.json`, and one directory per seed containing `checkpoint.json`, `trace.jsonl`, `suitability.json`, `control.json`, `control-final.json`, and `application-vault/`. The trace includes every preparation command and full production preview; checkpoints precede any initial measured-season approach. Random vault/branch/snapshot identifiers are infrastructure metadata and are not part of the deterministic gameplay bytes.

| Seed | Common checkpoint SHA-256 |
|---|---|
| 2026 | `DC33DBA821D4A6A833D17477ECD46DBDE808467556648A54EFA0352D299FFF1D` |
| 2027 | `B567C5FDE0B3715FEEC3A1599EEBE32C3B3A21280FA7DD51729AB74841A02C09` |
| 2028 | `19015200CD04951C6424673FB603193E6865250E5BE32D4BE8BC7C968A2F9410` |
| 2029 | `37D88AB137EB1B29783EF7CC17452A4FF914D13E86F19C17F37B7CE8290682F7` |
| 2030 | `97BDF6C558EC3B240EA755FCC09CC5F09BA4D3B1413B09FB751818FF4A167283` |

Regenerate from this PR's source using the repository SDK: build `src/FootballTycoon.Headless` in Release, then run `dotnet src/FootballTycoon.Headless/bin/Release/net10.0/FootballTycoon.Headless.dll --playable-start <new-empty-output-directory>`. The mode refuses existing nonempty output. Compare the hashes above before use. This regeneration route makes the checkpoints accessible from the PR without committing large generated saves. Run-3's Headless assembly SHA-256 is `D2FDF3BD5C6239E6EBF4B9420A5350B0130917EF0EEDDFEDD230ACC33C8619A0`; Core is `20C283FED1DB2176570652BE949BD7376D73C50FACFAF2D55C363E75EA21BD87`. Binary hashes identify this local build; gameplay hashes identify reproducible starts.

For recovery verification, build Desktop Debug; set `FT_PLAYABLE_START_CHECKPOINT` to the generated checkpoint's absolute path and `FT_SMOKE_OUTPUT` to a fresh absolute directory. Launch Godot with `--path src/FootballTycoon.Desktop --resolution 1280x720 -- --smoke-test --playable-start-recovery-smoke-test`. Isolate `APPDATA` and `LOCALAPPDATA` under a new task directory as in the repository run script. The opt-in probe creates a dedicated `recovery-vault`, imports exact checkpoint bytes, uses the actual Load/recover screen and Load this checkpoint callback, checks restored context, renders 100/125/150% text, invokes keyboard continuation, compares the result with ordinary production simulation and checks source preservation. It exits after verification; it does not introduce a general import UI or alter normal play. The probe vault is deliberately separate from the normal application path. To prepare a real playable session on Windows, choose a new isolated APPDATA and LOCALAPPDATA directory; create APPDATA/Godot/app_userdata/Football Tycoon/saves/offline. Read SnapshotId from run-3/<seed>/manifest.json and copy only application-vault/<SnapshotId>.ftsave into that fresh offline directory, refusing an existing destination file. This is the original week-52 common start, not the later application/probe advance snapshot. Launch normal Desktop with those same environment variables and no smoke flags, open Load/recover and load that checkpoint. Do not copy into an existing user vault or change a save envelope. Future-schema imports must use supported migration, then fork the same imported state for each treatment.

Application load/export and the first weekly advance were byte-exact for all five seeds; common vault snapshots remained unchanged. Both eligible checkpoints also passed graphical Desktop recovery and keyboard continuation on Godot 4.7.2 at 1280×720, with captures at all three text scales in `desktop-2026-graphical/` and `desktop-2030-graphical/`. The largest-scale loaded screens were visually inspected and remained readable with ordinary scrolling. Seed 2026 additionally passed the headless engine probe. These checks establish recovery/playability, not user comprehension or evidence from human participants. Machine manifests preserve their pre-Desktop `Not yet verified` status; these separate smoke logs/captures provide the subsequent verification.

### Measured-season control, weeks 53–104

All five controls completed season two and its week-104 review within the unchanged three-season limit; preparation-season outcomes are excluded. Every control attempted the one initial unblocked current recommendation. Kai Clarke signed in 2026, the recommendation signed in 2028, and the approaches closed in 2027/2029/2030. No forced acceptance or retry was used. There were no subsequent shortage-driven approvals. All controls had zero arrears weeks and zero role/squad-shortage weeks.

| Seed | Matches / league points | Opening cash | Minimum cash | Closing cash | Actual wages paid | Remaining signed wages | Frozen young-cohort starts |
|---|---|---:|---:|---:|---:|---:|---:|
| 2026 | 34 / 16 | £2,688,940 | £2,688,940 | £6,253,546 | £1,453,010 | £1,133,080 | 97 |
| 2027 | 31 / 32 | £2,471,440 | £2,450,260 | £3,433,480 | £1,586,000 | £860,080 | 112 |
| 2028 | 31 / 46 | £2,412,840 | £2,405,030 | £3,647,450 | £1,492,990 | £968,240 | 152 |
| 2029 | 31 / 35 | £2,569,800 | £2,558,460 | £3,674,920 | £1,512,160 | £1,454,960 | 119 |
| 2030 | 32 / 39 | £2,498,200 | £2,488,300 | £3,695,340 | £1,491,360 | £1,271,400 | 104 |

`control.json` also retains closing annual wages, individual frozen-cohort starts, capped appearances, the 20-appearance threshold and actual annual development, later young recruits separately, original approved terms, resolved approach reviews, new wage obligations, table/cup results and final gameplay hashes. These totals are descriptive controls, not treatment comparisons or economic-balance findings. Independently repeated preparation and control runs were byte-identical, and suitability inspection preserved exact state/RNG bytes.

### E01B validation and remaining work

The initial run stopped at vault metadata validation because the harness supplied non-GUID infrastructure identifiers. That incomplete `run-1` is retained; the fix changed only vault IDs. `run-2` completed all seeds. Independent review then identified that a newly added tuple-based prospective lineup would serialize as empty objects; the final harness uses explicit Player/Position projections. `run-3` regenerated all evidence, including control replay and richer match/financial records, with identical common checkpoint hashes and control outcomes. The recipe and eligibility thresholds were not changed after observing results.

Release diagnostics/tests and Debug Desktop builds passed with zero warnings/errors. The full suite passed 287 tests, zero failures/skips, in 1 minute 35 seconds (tests.log and tests/e01b-full.trx). Full solution and Desktop formatting verification passed (format.log and desktop-format.log). Three new tests cover the exact two-swap cap, dual-cost/ranking boundaries including blocked first candidates, and real ordinary preparation/production-preview consistency with purity and replay. Desktop logs/captures above cover the real recovery path and keyboard continuation. Final source cleanup only removes trailing blank lines after the recorded binary run.

Independent non-author reviewer `e01b_review` inspected all five changed files, production callers, tests, run-3 evidence and graphical recovery captures; verified both eligible starts, all five controls, hashes and metrics; and rechecked the serialization/loading-instruction fixes. Final review passed with no actionable concerns. E02 may rely on the reproducible schema-23 starts and their recovery checks. E04/E08 still owe actual alternative previews and executed policy comparisons; E07 still owes the mandate UI; E09 still needs real participants. This does not prove development gains, worthwhile financial tradeoffs, staff relationships, autonomous spending, replay interest, retention or release readiness.

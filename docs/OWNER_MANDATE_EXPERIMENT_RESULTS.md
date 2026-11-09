# E08: integrated owner-mandate comparison

**Technical comparison passes the registered exploratory gates; player usefulness remains unproven.** All eight arms completed the season and exact replay. Development added 37 or 23 frozen-cohort starts with useful development exposure. Value changed the real quote in both seeds, signed in 2026, and was declined in 2030. No pair triggered the registered cross-seed dominance warning. Substantial cash headroom means useful financial resilience is still a human question.

Measured 7 November 2026; report completed 8 November. The fixed [E00-A1 contract](OWNER_MANDATE_EXPERIMENT.md) governs this comparison. The working branch `codex/owner-mandate-comparison` includes E07 revision `74e5df629c05948988964965f2ecac272fcc7f04` ([PR #55](https://github.com/akoppong/footytycoon/pull/55)) and its reviewed prerequisite stack. E08 changes diagnostic tooling and this report, with no gameplay tuning or new save schema. The task PR remains open for owner review. These two deterministic eligible starts are exploratory examples, not statistical proof or a release recommendation.

## Registered comparison and provenance

The five seeds remain 2026, 2027, 2028, 2029 and 2030, in that order. E01B prepared ordinary season-two checkpoints at week 52, after renewal/release and PreserveReserve confirmation, with no direct squad/cash edits. Treatment outcomes exclude the preparation season. The [E01B handoff](OWNER_MANDATE_BASELINE.md#e01b-results-and-checkpoint-manifest) records the preparation recipe, source checks and desktop recovery. Prerequisites include E00/A1 integration `df54a03fd4cd47786cb771f200bff8904dc98ac1`, E01 `5a72b351479c69d06b76179f59d2425eb478722e`, and E01B `ca9614c65b3bc5d445d07c91be1a0946c9608e99` ([PR #48](https://github.com/akoppong/footytycoon/pull/48)); E07 includes E02–E06.

| Seed | E01B entry decision | Source checkpoint SHA-256 |
|---|---|---|
| 2026 | Eligible; primary human-study start chosen before treatment outcomes | `DC33DBA821D4A6A833D17477ECD46DBDE808467556648A54EFA0352D299FFF1D` |
| 2027 | Ineligible: exact ordered/capped Development selection lacks a positive readiness sacrifice | `B567C5FDE0B3715FEEC3A1599EEBE32C3B3A21280FA7DD51729AB74841A02C09` |
| 2028 | Ineligible: first-ranked Value alternative lacks feasible ≥10% saving with positive ability sacrifice | `19015200CD04951C6424673FB603193E6865250E5BE32D4BE8BC7C968A2F9410` |
| 2029 | Ineligible: both selection and Value entry conditions fail | `37D88AB137EB1B29783EF7CC17452A4FF914D13E86F19C17F37B7CE8290682F7` |
| 2030 | Eligible | `97BDF6C558EC3B240EA755FCC09CC5F09BA4D3B1413B09FB751818FF4A167283` |

These three no-op/ineligible seeds stay in the manifest and results denominator for entry feasibility (two of five). They are not substituted, silently dropped, or treated as successful policy comparisons. E08 runs all four arms at both eligible starts:

| Arm | Manager | Director |
|---|---|---|
| RC | Readiness | CurrentRecommendation |
| DC | Development | CurrentRecommendation |
| RV | Readiness | LowerCostValue |
| DV | Development | LowerCostValue |

Each arm loads the same migrated seed checkpoint, changes mandates once if needed, and plays weeks 53–104 through the normal season-two review. Capital plan stays PreserveReserve. The initial free-agent preview is attempted once at week 52; an unblocked offer is approved without reserve exception. Subsequent approval attempts occur once before advance only when `Contracts.Shortages` is nonempty. Midseason wait is chosen. No paid recruitment, injection, further facility investment, manual selection, forced acceptance or treatment-dependent repair is allowed. After actions diverge, later acceptance and match streams need not match across arms; replay comparisons use identical commands within each arm.

Source bytes are under `artifacts/owner-mandate-e01b-20261025/run-3/<seed>/checkpoint.json`. The accepted E08 run is `artifacts/owner-mandate-e08-20261107/matrix-1/`; its manifest retains source hashes and excluded-seed failure text. Each run stores `<seed>/migrated-start.json`, `<seed>/<arm>/trace.jsonl`, `summary.json`, `final-world.json` and `middle-vault/` save/reload evidence. Derived output is `artifacts/owner-mandate-e08-20261107/analysis-1/{analysis.json,tables.md}`. It retains every arm, all four manager contrasts, all four director contrasts, and all six arm pairs in both directions across both eligible seeds. `completion.json` confirms eight complete arms. No treatment run was discarded.

The measured Headless binary SHA-256 is `9512C0A94CF21BCBA0A1BCDA70A942888DE4BCD275B783D998725FB1BE08D15F`; Core is `F142105A5991FD7520E2556955FCD40069503154A9AC65E06879F9F609D33E15`. Assembly informational version names parent E07 `1.0.0+74e5df629c05948988964965f2ecac272fcc7f04`; the E08 PR diff identifies the added diagnostics. Subsequent runner formatting changed whitespace only. Source gameplay bytes/hashes and complete command traces, rather than timestamped vault envelopes or build metadata, identify reproduction.

## Reproduction and metric definitions

The runner's actual command trace is the authority for owner actions. The dependency-free Node analyzer [analyze-owner-mandates.mjs](../scripts/diagnostics/analyze-owner-mandates.mjs) reads the run and produces `analysis.json` and `tables.md` in a **new** output directory. It does not modify saves or rerun the world. Money remains safe integer pence; the 10% inequalities use `BigInt`. It independently reconciles wage payments against original ledger entries, future signed commitments against original obligations, cohort starts against fixtures and player totals, and the weekly minimum/arrears/shortage counts. Its boundary checks are available with `--self-test`.

```powershell
& ./.tools/dotnet/dotnet.exe build src/FootballTycoon.Headless/FootballTycoon.Headless.csproj --no-restore -c Release --nologo -m:1 -nodeReuse:false -p:UseSharedCompilation=false
& ./.tools/dotnet/dotnet.exe src/FootballTycoon.Headless/bin/Release/net10.0/FootballTycoon.Headless.dll --mandate-comparison artifacts/owner-mandate-e01b-20261025/run-3 artifacts/owner-mandate-e08-rerun/matrix
node scripts/diagnostics/analyze-owner-mandates.mjs --self-test
node scripts/diagnostics/analyze-owner-mandates.mjs artifacts/owner-mandate-e08-rerun/matrix artifacts/owner-mandate-e08-rerun/analysis
& ./.tools/dotnet/dotnet.exe src/FootballTycoon.Headless/bin/Release/net10.0/FootballTycoon.Headless.dll --mandate-timing artifacts/owner-mandate-e01b-20261025/run-3/2026/checkpoint.json artifacts/owner-mandate-e08-rerun/timing
```

Use fresh output paths. Restore project dependencies first if this checkout lacks assets. Node 24.16.0 ran the analyzer; it uses only built-in modules. If ignored source checkpoints are absent, build/run Headless **in a separate checkout of E01B revision `ca9614c65b3bc5d445d07c91be1a0946c9608e99`**, using `--playable-start <new-directory>` as documented in the E01B handoff. Supply that generated directory to the E08 runner and verify all five registered source hashes. Regenerating the pre-treatment world with later gameplay source is not an equivalent substitute.

Frozen cohort IDs are the owned players aged ≤23 at week 52. Starts count only owned-club competitive lineups during weeks 53–104. Later signed young recruits are separate. Annual development inputs come from each actual `PlayerProgress`: age/ability before development, appearances, capped input `min(25, appearances)`, crossing 20, and realized ability gain. Saturated extra appearances can alter selection exposure while adding no capped development input. A gate-relevant player must still be young and below ability 100 at the actual annual assessment.

Shared fixtures match the exact tuple `(season, competition, fixture ID, home club, away club)`. There is no blanket ID exclusion or guessed cup-opponent mapping. Nonshared cup fixtures and their cohort starts are separately exposed; unmatched fixtures can include changed opponents in the same round, so “nonshared” does not automatically mean additional matches. The total start delta decomposes exactly into shared-fixture and nonshared-fixture deltas. Saved swaps identify direct manager decisions; that decomposition is descriptive and does not isolate every downstream cause.

Lowest cash includes week 52 and every completed week. Minimum reserve headroom is the minimum cash minus reserve target on those observations. Remaining wage commitment includes only unpaid future signed wage obligations after observation week; closing arrears pence and arrears weeks are separate. Original approach obligations, dates, total signed commitments, remaining amounts and referenced actual wage payments are retained. Approval/quoted savings are never reported as signings or paid cash savings.

Dominance checks all six arm pairs, both directions, using both seeds: points, closing cash, minimum cash and cohort starts higher; remaining wages, arrears weeks and shortage weeks lower. Closing arrears pence is reported as financial context, without adding a measure to the registered warning gate. The same direction must be no worse on every measure and strictly better on at least one at **each** seed. Missing or partial arms give an inconclusive pair and cannot support an overall success claim. A warning requires contextual interpretation of signed duration/cover, development exposure and schedule; it does not prove optimality.

## Observations and gates

All money in the following tables is **integer pence**. There are no missing completed-arm measurements. Closing arrears, arrears weeks, shortage weeks and closing squad shortages are zero in every arm. Reserve target is 30,000,000 pence throughout. Opening cash is 268,894,000 for 2026 and 249,820,000 for 2030.

| Seed/arm | Closing cash | Minimum cash | Minimum reserve headroom | Actual wages paid | Closing annual wages | Remaining signed wages |
|---|---:|---:|---:|---:|---:|---:|
| 2026 RC | 625354600 | 268894000 | 238894000 | 145301000 | 145496000 | 113308000 |
| 2026 DC | 625354600 | 268894000 | 238894000 | 145301000 | 145496000 | 113308000 |
| 2026 RV | 627394600 | 268894000 | 238894000 | 143261000 | 143416000 | 93028000 |
| 2026 DV | 627394600 | 268894000 | 238894000 | 143261000 | 143416000 | 93028000 |
| 2030 RC | 369534000 | 248830000 | 218830000 | 149136000 | 149136000 | 127140000 |
| 2030 DC | 369534000 | 248830000 | 218830000 | 149136000 | 149136000 | 127140000 |
| 2030 RV | 369534000 | 248830000 | 218830000 | 149136000 | 149136000 | 127140000 |
| 2030 DV | 369534000 | 248830000 | 218830000 | 149136000 | 149136000 | 127140000 |

| Seed/arm | Points/position | Cup exit | Fixtures | Frozen-cohort starts / fixture | Direct swaps | Sum of ability sacrifices | Realized cohort ability gain |
|---|---|---|---:|---|---:|---:|---:|
| 2026 RC | 16 / 16 | Quarter-final | 34 | 97/34 | 0 | 0 | 2 |
| 2026 DC | 15 / 16 | Quarter-final | 34 | 134/34 | 38 | 109 | 3 |
| 2026 RV | 15 / 16 | Quarter-final | 34 | 97/34 | 0 | 0 | 2 |
| 2026 DV | 14 / 16 | Quarter-final | 34 | 134/34 | 38 | 109 | 3 |
| 2030 RC | 39 / 9 | Round of 32 | 32 | 104/32 | 0 | 0 | 6 |
| 2030 DC | 39 / 9 | Round of 32 | 32 | 127/32 | 26 | 62 | 6 |
| 2030 RV | 39 / 9 | Round of 32 | 32 | 104/32 | 0 | 0 | 6 |
| 2030 DV | 39 / 9 | Round of 32 | 32 | 127/32 | 26 | 62 | 6 |

### Manager comparisons: direct decisions, exposure and growth

| Seed / contrast (treatment minus baseline) | Starts delta | Shared fixtures / start delta | Nonshared fixtures / start delta | Useful capped-input player IDs | Realized gain delta | Registered gate |
|---|---:|---|---|---|---:|---|
| 2026 DC−RC | +37 | 34 / +37 | 0 / 0 | 395, 396 | +1 | PASS |
| 2026 DV−RV | +37 | 34 / +37 | 0 / 0 | 395, 396 | +1 | PASS |
| 2030 DC−RC | +23 | 32 / +23 | 0 / 0 | 378, 392 | 0 | PASS |
| 2030 DV−RV | +23 | 32 / +23 | 0 / 0 | 378, 392 | 0 | PASS |

All shared-fixture comparisons use exact IDs/opponents; no derived-ID mapping was necessary. Each 2026 arm plays 30 league and four cup fixtures; each 2030 arm plays 30 league and two cup fixtures. There are **no additional or unmatched cup fixtures in any contrast**, so none of these start differences is explained by an extra cup match. All saved swaps select frozen-cohort players at a positive ability cost, within two per fixture and five per swap. The 38/26 saved direct decisions differ from the net +37/+23 start effects because later availability and readiness selection can change; the saved decision count is not an additive causal decomposition of the season delta.

The table below applies separately to **both manager contrasts** at its seed; their actual per-player inputs are identical. B→T is Readiness→Development. Actual annual appearance inputs equal owned starts for these frozen players. Each player's age and ability below are the annual assessment's values before development, equal in both arms; all ten are age-eligible and below the 100 ceiling.

| Seed / ID / name | Age / ability before | Starts and annual inputs B→T | Capped25 B→T | At least20 B→T | Extra saturated appearances | Realized gain delta |
|---|---|---|---|---|---:|---:|
| 2026 / 375 Liam McGowan | 22 / 75 | 34→34 | 25→25 | yes→yes | 0 | 0 |
| 2026 / 379 Ryan Diallo | 20 / 78 | 30→30 | 25→25 | yes→yes | 0 | 0 |
| 2026 / 381 Ross Ingram | 22 / 76 | 32→32 | 25→25 | yes→yes | 0 | 0 |
| 2026 / 395 Sami Okafor | 19 / 65 | 0→11 | 0→11 | no→no | 0 | 0 |
| 2026 / 396 Adam Bennett | 23 / 68 | 1→27 | 1→25 | no→yes | 2 | +1 |
| 2030 / 375 Ethan Varley | 23 / 72 | 30→30 | 25→25 | yes→yes | 0 | 0 |
| 2030 / 378 Declan Petrov | 22 / 63 | 0→5 | 0→5 | no→no | 0 | 0 |
| 2030 / 390 Adam Stokes | 18 / 75 | 32→32 | 25→25 | yes→yes | 0 | 0 |
| 2030 / 392 Ryan Moretti | 22 / 70 | 10→28 | 10→25 | no→yes | 3 | 0 |
| 2030 / 393 Ollie Kilbride | 22 / 68 | 32→32 | 25→25 | yes→yes | 0 | 0 |

This passes the unchanged ≥6-start plus useful-exposure gate in all four contrasts. Bennett and Moretti cross the existing 20-appearance condition, but their final two/three extra starts above 25 do not increase the capped growth input. Moretti's realized gain is unchanged despite increased exposure; no gain is guaranteed. In 2026 Development also loses one league point in each director context; in 2030 points are unchanged. This supports a concrete readiness cost, not a general claim that Development improves the club.

### Director comparisons: quotes, actual signings and money

Both managers produce the same initial quotes for a given director/seed. Every preview is unblocked; fee and immediate approval payment are zero. The comparison uses actual production preview terms, not E01B's diagnostic alternatives.

| Seed / director | Player / role / age / ability | Weekly wage | Wage weeks / duration | Proposed commitment | Ordinary outcome |
|---|---|---:|---|---:|---|
| 2026 Current | 2110 Kai Clarke / forward / 19 / 85 | 195000 | 54–208 / 155 | 30225000 | Signed week53 |
| 2026 Value | 2109 Isaac Jarvis / forward / 38 / 83 | 155000 | 54–104 / 51 | 7905000 | Signed week53 |
| 2030 Current | 2008 Kyle Sutton / midfielder / 27 / 86 | 217000 | 54–156 / 103 | 22351000 | Declined week53 |
| 2030 Value | 2109 Andrei Fletcher / midfielder / 33 / 81 | 165000 | 54–104 / 51 | 8415000 | Declined week53 |

The integer 10% tests pass in both cost dimensions. For 2026, weekly `155000×10 ≤ 195000×9` and total `7905000×10 ≤ 30225000×9`; for 2030, weekly `165000×10 ≤ 217000×9` and total `8415000×10 ≤ 22351000×9`. Candidates differ, neither cost rises, and ability sacrifices are two/five. Thus all four director contrasts pass the initial real-preview gate.

| Seed / contrast | Weekly quoted saving | Total quoted saving | Actual wages paid delta | Closing cash delta | Remaining signed wages delta | Executed comparison |
|---|---:|---:|---:|---:|---:|---|
| 2026 RV−RC | 40000 | 22320000 | −2040000 | +2040000 | −20280000 | Different candidates signed |
| 2026 DV−DC | 40000 | 22320000 | −2040000 | +2040000 | −20280000 | Different candidates signed |
| 2030 RV−RC | 52000 | 13936000 | 0 | 0 | 0 | Both declined; no executed savings |
| 2030 DV−DC | 52000 | 13936000 | 0 | 0 | 0 | Both declined; no executed savings |

There is exactly one initial approved approach in each arm and no later shortage-driven approval. In 2026 each arm creates original wage obligation **1537** (IDs are local to that arm). Current signs 30,225,000 pence over weeks54–208, pays **9,945,000** in 51 wage entries by week104, and retains **20,280,000**. Value signs 7,905,000 over weeks54–104, pays **7,905,000** in 51 entries and retains zero. The analyzer resolves each original outcome/contract/obligation and retains payment sequence IDs; it does not infer payment from terms. In 2030 the outcomes carry no signed contract or obligation, and no payments are attributed. Its executed financial comparison is **INCONCLUSIVE** at that seed. The registered sample-wide no-signing condition does not apply because 2026 changes an actual signing.

The 2026 realized wage saving is **£20,400**, while its quoted £223,200 total saving includes **104 fewer weeks of contracted future cover**, worth £202,800 in remaining wages at week104. Value also obtains an older player two ability points weaker; Current acquires a 19-year-old with a longer future. Those are real tradeoffs. Both initial candidates exceed the existing forward starters' ability70; the 2030 candidates exceed the existing midfield starters' 77/75/75/72. Candidate quality can matter to starting strength, not only bench depth.

The only later young recruit is Kai Clarke in 2026 RC/DC: **29 owned starts**, annual input29, capped25, age19/ability85 before development, no realized ability gain. Those 29 starts are excluded from the frozen-cohort totals and shown separately in raw output. There are no later young recruits in the other six arms. Hence the fixed-cohort metric does not capture the full long-term youth/contract tradeoff of Current versus Value.

Savings did not relieve a binding reserve constraint in these runs. Minimum cash/reserve headroom is **£2,688,940 / £2,388,940** in 2026 and **£2,488,300 / £2,188,300** in 2030, against a £300,000 reserve. There are no arrears/shortages. A readable 20%+ weekly reduction may be personally irrelevant in this cash context. Whether retaining that money matters to a player's stated club objective is **unproven**, not a financial-resilience success claim. No cash crisis or new spending outlet was introduced.

### Squad context and dominance checks

Ratios below preserve exact age/ability sums over roster counts, rather than hiding rounding. Closing rosters precede season-three renewal; an expired week104 contract still appears until that ordinary decision.

| Seed/arm | Opening mean age / ability | Closing mean age / ability | Closing GK/D/M/F | Contracts ending104/156/208 |
|---|---|---|---|---|
| 2026 RC | 564/19; 1316/19 | 603/20; 1395/20 | 2/7/6/5 | 8/10/2 |
| 2026 DC | 564/19; 1316/19 | 603/20; 1396/20 | 2/7/6/5 | 8/10/2 |
| 2026 RV | 564/19; 1316/19 | 622/20; 1393/20 | 2/7/6/5 | 9/10/1 |
| 2026 DV | 564/19; 1316/19 | 622/20; 1394/20 | 2/7/6/5 | 9/10/1 |
| 2030 all four | 547/19; 1310/19 | 566/19; 1310/19 | 2/7/7/3 | 6/11/2 |

Every closing squad meets normal cover. Value in 2026 has one fewer contracted player beyond week104 (11 versus12); its smaller remaining obligation is accompanied by shorter cover. The table below checks **every direction**, with “no worse / strictly better” evaluated on all registered measures per seed. A strict improvement on one measure does not erase a worse measure.

| Candidate dominant → comparator | 2026 no worse / strict | 2030 no worse / strict | Cross-seed warning |
|---|---|---|---|
| RC→DC | no / yes | no / no | None |
| DC→RC | no / yes | yes / yes | None |
| RC→RV | no / yes | yes / no | None |
| RV→RC | no / yes | yes / no | None |
| RC→DV | no / yes | no / no | None |
| DV→RC | no / yes | yes / yes | None |
| DC→RV | no / yes | yes / yes | None |
| RV→DC | no / yes | no / no | None |
| DC→DV | no / yes | yes / no | None |
| DV→DC | no / yes | yes / no | None |
| RV→DV | no / yes | no / no | None |
| DV→RV | no / yes | yes / yes | None |

No arm is dominant under the registered two-seed rule. The 2026 manager contrasts trade a point for useful development exposure; director contrasts trade a point, ability and future cover for savings. In 2030 Development increases useful exposure without an observed points/cash cost, but incurs documented readiness sacrifices, and director approaches fail to sign. This does not establish an optimal policy or human relevance. Interactions on reported season totals are zero here: within each seed the manager effect is identical under both directors and the director effect identical under both managers. That small-sample observation does not guarantee independence in other worlds.

| Unchanged exploratory gate | Result | Limit |
|---|---|---|
| Complete-arm financial/identity/authority/replay/purity/save safety | PASS on eight arms plus current regression checks below | Does not exhaust all careers/failure states |
| ≥6 frozen-cohort starts plus eligible uncapped/20-threshold exposure | PASS in all four manager contrasts | No guaranteed growth; one seed has zero realized gain difference |
| Distinct actual Value preview, ≥10% saving, dual-cost and ability bounds | PASS in all four director contrasts | Savings mostly reflect shorter contracts |
| Changed candidate actually signs | Observed in 2026; INCONCLUSIVE executed comparison in 2030 | Two replicated manager arms are not independent signings evidence |
| All-pair cross-seed dominance warning | No warning | Absence of warning does not prove worthwhile choice |
| Human comprehension, traceability, relevance and voluntary replay | Awaiting player evidence | Zero participant observations; no product pass/revise/stop inference |

## Integrated checks and limitations

Current evidence lives under `artifacts/owner-mandate-e08-20261107/`:

| Check | Result / raw artifact |
|---|---|
| Release solution build | PASS, zero warnings/errors; final `build-2.log` after formatting (initial `build-1.log` also passed) |
| Release Headless diagnostic build | PASS, zero warnings/errors; `diagnostics-build-1.log` |
| Applicable full test suite | PASS **372/372**, zero failures/skips; `tests-2/e08-full.trx`, `tests-2.log` |
| Solution / Desktop formatting | PASS; `format-3.log`, `desktop-format-1.log` |
| Debug Desktop build | PASS, zero warnings/errors; `desktop-build-2.log` |
| Current Godot standard headless smoke | PASS; `desktop-smoke-1/` (acquisition, plans, required guards, six workspaces at three scales, save/load/resume) |
| Fixed matrix and midseason disk reload | PASS eight arms; `matrix-1/`, `matrix-1.log` |
| Current application timing/parity sample | PASS through week58; `timing-1/timing.json`, `timing-1.log` |
| Analyzer boundary checks / raw reconciliation | PASS; `node ... --self-test`, `analysis-1/` |

The current suite includes pending paid/free-agent offer retention and acceptance-stream checks, nonterminal season rollover, forbidden career states, no-recommendation eligibility boundaries, and failed recovery/candidate writes followed by exactly-once retry publication. The full suite result covers these named tests in `MandateCommandTests`, `DirectorMandateTests`, `OwnerMandateStateTests` and associated evidence/application tests. The matrix separately checks original imported identities, command authority, financial reconciliation, pure previews, swap bounds, codec equality and exact uninterrupted versus saved/reloaded continuation after every advance. Source checkpoints remain unchanged.

Environmental attempts are retained: initial testhost connection timed out under sandbox restrictions (`tests-1.log`), initial Desktop build could not access the user NuGet config (`desktop-build-1.log`), and initial format workspace load could not open its named pipe (`format-1.log`). Authorized retries/environment isolation passed. `format-2.log` then identified runner initializer whitespace; formatting alone fixed it before the passing verification. These are not discarded gameplay failures or substituted treatment results.

E07's **ancestor**, not freshly repeated E08 graphical evidence, is `artifacts/owner-mandate-e07-20261103/graphical-1/`: 23 captures at 100/125/150%, keyboard operation, failed publication/retry, source preservation and first mandate consequences. Its `headless-1/`, `standard-1/` and `season-1/` also passed, including the three-season terminal endpoint. [E07 handoff](OWNER_MANDATE_UI.md) gives details; its CI run `41644085859` passed. E08 changes no UI/gameplay and does not relabel those captures as current. A fresh graphical facilitator timing/rehearsal and participant sessions remain outstanding.

Current timing machine: **LAPTOP-L4PAEFHE**, Windows10.0.26300, X64, .NET10.0.12, 16 logical processors. Six sequential samples cover ordinary advances from week52 through58; the initial imported world and final Core/application state agree exactly. Values below are milliseconds, rounded only for display; raw doubles are retained.

| Operation | n | Minimum–maximum ms | Mean ms |
|---|---:|---|---:|
| Core `AdvanceWeek` only | 6 | 21.563–133.390 | 61.243 |
| Initial queued query | 6 | 130.427–290.294 | 164.923 |
| Queued free-agent preview | 6 | 140.437–178.072 | 156.561 |
| Mandate preview | 1 | 98.099 | 98.099 |
| Durable autosave `Vault.Write` | 6 | 185.429–252.137 | 215.478 |
| Application advance including autosave/publication | 6 | 366.842–453.159 | 398.460 |
| Query after advance | 6 | 52.240–110.263 | 76.050 |

Import write took 1008.834ms, load 619.959ms, mandate commit 1598.466ms and free-agent commit 828.096ms. Vault timings include compression/durable flush/verification but exclude the caller's encoding. Application advance includes clone, simulation, encoding, durable write and publication; query/preview include worker dispatch. Core ran first each week; first calls include cold costs. The full regression tests and policy matrix were also active locally during this sample, so durations include resource contention. These are small local samples, with no randomized performance comparison, percentile, full-season save-inclusive timing claim or usability threshold. They cannot replace the facilitator pacing pilot.

Independent non-author review inspected the runner/timing helper and analyzer, including actual 2026/2030 raw outcomes. It identified and rechecked two analyzer corrections before the accepted derivation: keep closing-arrears pence contextual rather than adding a dominance criterion, and make incomplete/unguarded matrices inconclusive overall. Boundary checks also cover saturated starts, below-ceiling eligibility, no-growth passes, exact 10% arithmetic and strict improvement at every seed. Final report and sampled raw-result review completed with no outstanding actionable findings; no human result is inferred by the reviewer.

## Player-evidence boundary

**Awaiting player evidence.** The separately reviewed [E09 preparation PR #53](https://github.com/akoppong/footytycoon/pull/53) contains the neutral study protocol. The user confirmed participants are still needed. No participant count, comprehension, preference, relevance, voluntary replay or retention result exists. Five adult volunteers, including at least two new to the prototype, actual consent, the facilitator timing pilot and the unchanged denominator/opportunity rules remain required.

**Smallest next bounded step:** obtain the required volunteers/facilitator and run E09's neutral pacing pilot on a fresh copy of the already selected 2026 start, then the fixed five-person study with a fair optional replay opportunity. In particular, record each person's club objective before play and whether retaining money matters despite the observed surplus. The technical gates provide no reason to retune the policy or silently create scarcity now. If the pilot exposes a technical/scenario blocker, separately scope and review that repair before sessions. If the actual study finds readable but irrelevant choices, failed comprehension/tracing/relevance, or insufficient replay interest, apply the preregistered revise/stop rules. Missing people currently mean **await**, not product rejection or success.

The study still requires at least four of five to explain both mandates and retained signing approval without coaching, three of five to trace two real examples, three of five to connect tradeoffs to their prior club aim, and three of five to actually start a different-policy replay. Zero voluntary replay starts in a technically valid, actionable study with fair opportunity invokes the registered stop default; one/two or other failed criteria require bounded revision. No task is started by this report. Staff attachment, hiring/relationships, autonomous spending, long-term retention, broader economic balance and release readiness remain unproven.

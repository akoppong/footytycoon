# Opening population

Schema 18 (`opening-population-18`) starts new careers with the PRD's 1,200 people: 48 squads of 22 and 144 available free agents. Each squad has two goalkeepers, eight defenders, eight midfielders and four forwards, providing two full 1-4-4-2 teams before availability losses. This is starting depth, not a guarantee of future cover.

Each club keeps its previous aggregate opening wage budget: 18 times its division's standard wage. The existing ability/age weights distribute that budget among 22 signed contracts, preserving positive wages and exact totals. Every contract has its own dated wage obligation. Squad size changes selection, individual pay and later renewals; preserving the starting wage bill does not preserve later financial or sporting outcomes.

Opening ages now draw uniformly from 17 through 41 for goalkeepers and 17 through 39 for outfield players, replacing the younger triangular distribution. These fictional age rules spread the initial population across more retirement cohorts. Veterans already eligible for a final-year retirement announcement receive it during world creation, before an opening transfer can be approved. Signed terms are never shortened.

The free-agent pool has 19 goalkeepers, 48 defenders, 48 midfielders and 29 forwards, the rounded academy role proportions. A private shuffled role stream avoids coupling role to the previous club's division. Each club is the previous employer of three people. These are pre-career backgrounds: expired week-zero contracts, positive historical wages, no current wage obligation and no invented in-career departure or academy event. IDs and contract IDs 2000–2143 are disjoint from opening squads and future graduates. Names are unique across all 1,200 people. Separate saved random streams make generation repeatable for a seed.

Available players draw ability from their previous division's par minus 15 through par plus 10 (clamped to 1–100). Historical weekly wages draw an integer percentage from 50–125% of that division's standard wage, rounded down to £10 with a £100 floor. These are fictional starting distributions, not measured real-world salary or ability data.

Existing schema-17 careers migrate by validation and version markers only. No people are added, renamed or aged; saved contracts, notices, offers and history remain intact. Smaller existing worlds stay smaller. Older migrations retain their original policies, including an empty pool when loading a pre-free-agent save. Previously quoted commands must be previewed again after a version change.

Academy admission and owner/rival free-agent approaches still stop at 22 contracted players. The older authorized transfer searches retain their existing rules and can take a squad above 22. Rival discretionary replenishment retains its conservative 18-player working mix; this unit does not raise that target. Academy intake, retirement cutoffs, negotiation probabilities and cover minimums are unchanged.

## Measurement method

The comparison uses PreserveReserve with seed 2026 for 25 seasons and seed 2027 for ten, matching the preceding [recruitment-contention evidence](RECRUITMENT_CONTENTION.md). The owner accepts recommended renewals/intake but makes no free-agent approaches or injections. The diagnostic runner includes both contracted and available opening identities in weekly conservation; only actual academy graduates add people. It uses ordinary weekly simulation and exact annual codec round trips, without resets, fabricated replacements or history pruning. The playable game remains limited to three seasons.

This experiment changes squad depth, starting free-agent supply, ages and the random draws that depend on those choices together. Differences cannot be attributed to player count alone. Two examples do not satisfy the 100-world/fifty-season gate or prove the economy balanced.

The preserved new runner is `artifacts/opening-population-runner`, based on `a77b3cd` plus this unit's generation, migration and conservation changes. Its assembly SHA256 is `95785DE6D902BB1C036360AC6F1423CC1FE464DAA9129E1A9EBA70265DD97885`. Baseline schema-17 evidence is preserved in `artifacts/contention-25-2026` and `artifacts/contention-10-2027`; runner provenance is recorded in the linked report. Timings overlap development checks and are not controlled benchmarks.

## Completed comparisons

| Observation | Schema 17 opening | Schema 18 opening |
|---|---:|---:|
| Seed 2026, 25 seasons: minimum / maximum active | 864 / 1,176 | 1,089 / 1,216 |
| Same run: weeks outside 1,100–1,500 | 781 | 3 |
| Same run: club-weeks below cover | 1,307 | 1,536 |
| Same run: final contracted / available / active | 849 / 267 / 1,116 | 837 / 281 / 1,118 |
| Same run: graduates / retirees | 1,173 / 921 | 1,168 / 1,250 |
| Same run: rival signed / declined / unavailable | 722 / 312 / 110 | 711 / 318 / 96 |
| Seed 2027, ten seasons: minimum / maximum active | 864 / 1,174 | 1,146 / 1,223 |
| Same run: weeks outside target | 312 | 0 |
| Same run: club-weeks below cover | 184 | 243 |
| Same run: final active | 1,144 | 1,146 |

The longer new run's three breaches occur at season closes 13, 15 and 16, before the next intake. They remain counted; no boundary exemption is applied. Both runs start inside the target, but the longer run still fails a strict requirement to remain inside it throughout. Larger starting squads also constrain early academy admissions through the unchanged cap, so graduate totals can differ.

Squad-cover results worsened in both samples. At both new endpoints only the owned club is below cover: 14 players in season 25, including one goalkeeper and three defenders, despite 47 available goalkeepers and 105 defenders below retirement age; 15 players with all role minimums met in season ten. The owner policy has made no approaches. Endpoint observations do not assign the entire cumulative shortage increase to that policy or establish that all available candidates are affordable.

The economy remains unbalanced evidence to investigate. Aggregate club cash at the new endpoints is £2,211,062,046 after 25 seasons and £777,256,964 after ten, versus £2,128,795,077 and £702,914,095 respectively. All four endpoints have zero clubs with arrears; that does not establish weekly financial health or economy acceptance. Initial wage budgets are unchanged, but different people, ages, matches and later contracts change the financial path.

New evidence is preserved in `artifacts/opening-population-25-2026` and `artifacts/opening-population-10-2027`. Both report `Completed` with weekly identity conservation and exact annual codec round trips. Final gameplay SHA256 values are `433C38659A42B8119A8E37E14F06136DFB9E26BEF4FEDD6C1925AFC1D4742160` (25 seasons, 206,364,302 uncompressed bytes) and `5DD0E1E887B5FA584EBB8B8F3A4ACC0CEC46899B2CD8A10D9459B58E667B2D3C` (ten seasons, 83,151,710 bytes). These identify the observed worlds, not a production acceptance pass.

## Validation and remaining work

The full Debug suite passed 173 tests, followed by the strengthened four-season population-counter test. Release build completed with no compiler warnings/errors, and core/desktop format checks passed. Tests cover exact population/role counts, identity/name uniqueness, dated wages, historical free-agent terms, role-sensitive ages, initial retirement announcements, determinism, versions-only migration, stale quotes and corrupt input. Controlled legacy/expiry fixtures explicitly isolate their smaller squads and empty background pool; generated-world season tests continue to exercise real retirement and population behavior.

The schema-18 self-contained package `artifacts/windows-d10beb22` passed headless execution and the rendered three-season, rival-history, development and controlled-retirement probes with `DOTNET_ROOT` cleared. The graphical process exited zero with all five PASS markers; only the known sandbox root-certificate-store diagnostic appeared. Captures are under `artifacts/opening-population-package-screenshots`. Inspected People page headings at 100%/150%, retirement announcements at 150%, and rival history at 100%/150%; this is not a complete accessibility certification. The portable ZIP `artifacts/FootballTycoon-opening-population-windows.zip` contains the executable, PCK, 194 runtime files and three font licenses (199 entries).

Remaining work includes reducing avoidable squad shortages, measuring an owner policy that actually considers replacements, tuning the long-run economy and completing the 100 varied fifty-season worlds. Durable-save endurance, clean-machine acceptance and human playtesting remain separate gates. Existing smaller saves do not acquire the new opening population during migration.

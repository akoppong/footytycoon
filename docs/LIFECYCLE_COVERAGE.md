# Academy role supply and coverage evidence

The long-career diagnostics now distinguish a shortage of people in a role from a club failing to sign available people. They report role populations, actual graduate-cohort survival, club vacancies and retained rival offer outcomes. These observations do not bypass financial checks or turn a person in the available pool into a guaranteed signing. Field definitions are in [ENDURANCE.md](ENDURANCE.md).

## Observed problem

The schema-15 academy chose all four roles with equal probability, while each club's cover requirements are 2 goalkeepers, 5 defenders, 5 midfielders and 3 forwards, plus one unspecified spare. In the seed-2026 baseline at season 19, the available pool held 95 goalkeepers, no defenders, 5 midfielders and 62 forwards. Four clubs lacked defender cover. The population was 992, below the 1,100 target. These are separate problems: changing the role mix cannot create the missing total population.

Supply was not the only constraint. At season 16, the baseline had 18 available defenders and ten clubs below defender cover. Several rivals with higher numeric club IDs recorded 12 or 13 unavailable outcomes over the previous 52 weeks, consistent with repeatedly losing competing offers under stable club-order resolution. The diagnostic owner makes no free-agent approaches, so owned-club gaps are also an expected limitation of that disclosed policy. Availability alone proves neither affordability nor suitability, and this report does not attribute every shortage to one cause.

## Bounded change

Future academy role draws now use 2/5/5/3 weights out of 15, matching the existing role-cover policy. One unconditional role draw replaces one unconditional role draw. Cohort size remains zero to two candidates, and unchanged admission checks still require squad space, wages within the revenue limit, no arrears and cash above reserve. Names, IDs, age, ability draws, wage terms and draw order remain unchanged for an otherwise identical cohort. No role is selected in response to an immediate vacancy. Recruitment and retirement policies are unchanged.

Schema 16 validates and migrates schema 15 by changing only version markers. Existing people and all historical records remain intact; only new, uncommitted cohorts use the weighted mix. The playable boundary stays at three seasons. [ACADEMY.md](ACADEMY.md) details the contract and migration behavior.

## Measurement method and limits

The matched runs use the same seed, horizon and PreserveReserve owner policy with no owner recruitment or injections. Both execute normal weekly simulation, validate identity accounting and round-trip annual worlds without pruning history. Their gameplay hashes are expected to differ after a rule change. The diagnostic-only census itself was checked against the previous bounded run: all 49 overlapping nonconfiguration, nonfinal observations match after removing the new `Coverage` field.

Baseline binaries are preserved in `artifacts/coverage-baseline-runner` (assembly SHA256 `96DDFD8C27674103F13BBAF800D911F31C7CF5A33388A964CC37A15F5CD31110`). Weighted binaries are in `artifacts/coverage-weighted-runner` (`66D8839CBE3B4629F9E47A1ED7905B10939BFF1FC723DB3DA07A44F48227B972`). Both report base build revision `4edea24` plus local implementation: the baseline contains the coverage diagnostics; the weighted build additionally contains schema-16 role weighting. Runtime and full provenance are in each configuration row. Elapsed times overlap other validation and are not performance comparisons.

## Completed comparisons

| Observation | Uniform roles | Weighted roles |
|---|---:|---:|
| Seed 2026, 25 seasons: club-weeks below cover | 6,292 | 4,087 |
| Same run: weeks outside population target | 781 | 781 |
| Same run: final active / contracted / available | 1,129 / 829 / 300 | 1,118 / 823 / 295 |
| Same run: actual graduates / retirees | 1,173 / 908 | 1,173 / 919 |
| Same run: available GK / defender / midfielder / forward | 157 / 24 / 24 / 95 | 40 / 113 / 94 / 48 |
| Seed 2027, 10 seasons: club-weeks below cover | 346 | 352 |
| Same run: weeks outside population target | 312 | 312 |
| Same run: final active population | 1,147 | 1,145 |

The 25-season sample records 35.04% fewer club-weeks below cover. The shorter second-seed sample is slightly worse; it does not establish a universal improvement. Both 25-season endpoints have one short club, the owned club. Its baseline endpoint lacks one defender and one total squad place; the weighted endpoint lacks goalkeeper, defender and forward cover and two total squad places. Available players exist in all those roles, but the diagnostic owner does not approach them. The change does not claim to resolve those shortages or the population target. Both 25-season runs remain below the population target at season-close samples 14–23.

Evidence directories are `artifacts/coverage-25-2026`, `coverage-weighted-25-2026`, `coverage-10-2027` and `coverage-weighted-10-2027`. All four report `Completed`, validate every weekly identity check and preserve exact annual codec round trips. The 25-season final gameplay hashes are `7352420F258C389FBD01CD7EAFD5A15DB1C9D8EF37E87199803B2E0BF72893C9` (uniform) and `EFDF2B64F91054225C5EAA59EFB87CE7254E09B5C2E32C1B515503A21107FAFB` (weighted). Final uncompressed worlds are 202,016,549 and 201,596,412 bytes. There is no history pruning.

These samples do not satisfy the PRD's 100 varied fifty-season worlds. Opening population is still 864 rather than the planned approximately 1,200. Recruitment contention, refusal, total-squad spare cover, age distribution, starting population, economy balance and the owner's replacement policy still need work. A better role mix is not a guarantee of a filled squad or a sustainable population.

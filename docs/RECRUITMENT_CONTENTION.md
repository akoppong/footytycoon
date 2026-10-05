# Rival recruitment contention

This report records the schema-16/17 comparison. Schema 18 subsequently changes [new-career opening population](OPENING_POPULATION.md); the baseline observations below remain historical evidence.

The schema-16 coverage report showed rivals repeatedly making unsuccessful approaches while alternatives remained in the pool. All directors ranked the same eligible people by shortage, ability, age and ID. With a stable club-ID resolution order and four-week cooldown, several clubs could spend each opportunity chasing a person who signed earlier in the resolution pass.

Schema 17 adds pending-approach count immediately after shortage priority in candidate ranking. Each director still chooses only an eligible, affordable candidate. Adding an offer increments that person's interest count for subsequent directors in the same weekly pass. Unaffordable alternatives are skipped; an urgent role vacancy still outranks an optional window upgrade. A scarce candidate may receive multiple offers. The ranking creates neither an exclusive reservation nor a contract.

Existing offers resolve before the next planning pass. Stable club-ID resolution, the fictional 70% acceptance probability, saved random streams, contract terms, four-week cooldown, owner approval, windows and wage/reserve checks are unchanged. The owner does not gain advance protection or automatic signings. Existing schema-16 worlds migrate by validating the original state and changing only version markers; pending targets and terms are preserved.

## Method

The comparisons use the same seeds, PreserveReserve owner policy and horizons as the preceding [academy role-supply](LIFECYCLE_COVERAGE.md) measurements. The baseline already includes weighted academy roles. The owner does not make free-agent approaches or inject money. These are two matched examples, not a varied-strategy acceptance suite. Both policies execute ordinary weekly simulation, identity accounting and annual codec round trips without resetting or pruning history. Gameplay hashes are expected to differ after changing recruitment behavior.

The preserved schema-16 runner is `artifacts/coverage-weighted-runner` (assembly SHA256 `66D8839CBE3B4629F9E47A1ED7905B10939BFF1FC723DB3DA07A44F48227B972`), based on `4edea24` plus that unit's diagnostic and academy changes. The schema-17 runner is `artifacts/contention-runner` (`136A3ABE18447069E8427747B6EEF1BE0D6ED19AF4C899A7214C1B8A8B1F8133`), based on `fb09f7c` plus the recruitment implementation. Each run records its binary hash, build, runtime and policy. Elapsed times overlap other development checks and are not controlled benchmarks.

## Completed comparisons

| Observation | Previous ranking | Pending-interest ranking |
|---|---:|---:|
| Seed 2026, 25 seasons: club-weeks below cover | 4,087 | 1,307 |
| Same run: unavailable rival outcomes | 1,399 | 110 |
| Same run: signed / declined rival outcomes | 384 / 171 | 722 / 312 |
| Same run: final contracted / available / active people | 823 / 295 / 1,118 | 849 / 267 / 1,116 |
| Same run: graduates / retirees | 1,173 / 919 | 1,173 / 921 |
| Same run: weeks outside population target | 781 | 781 |
| Seed 2027, 10 seasons: club-weeks below cover | 352 | 184 |
| Same run: unavailable rival outcomes | 402 | 20 |
| Same run: signed / declined rival outcomes | 66 / 21 | 230 / 98 |
| Same run: final active people | 1,145 | 1,144 |
| Same run: weeks outside population target | 312 | 312 |

Club-weeks below cover fell 68.02% in the longer sample and 47.73% in the shorter sample. More of the existing available pool found employment; no extra academy admissions or outside players were created. Refusals increased as more offers reached candidates who remained available to respond. This changes later contracts and results, so outcomes are not expected to be otherwise identical.

At both new endpoints, the owned club is the only club below cover. At season 25 it has one goalkeeper (target two), three defenders (target five) and 14 players overall (target 16), despite 42 available goalkeepers and 102 defenders below retirement age. The owner policy has made no approaches. The previous 25-season endpoint also had a 14-player owned squad, with goalkeeper, defender and forward gaps. At season ten the new endpoint retains the owned defender/total-squad gap; the previous endpoint also had two short rival clubs. None of these remaining gaps is reported as solved.

All four endpoints have zero clubs with arrears, but this is not proof of financial balance across every week or strategy. Aggregate club cash remains high: at season 25 it is £2,128,795,077 versus £2,152,733,155 previously. Population remains below target at season-close observations 14–23 in both longer runs. Recruitment efficiency does not repair the starting population or economy.

Evidence is in `artifacts/contention-25-2026` and `artifacts/contention-10-2027`, compared with `artifacts/coverage-weighted-25-2026` and `artifacts/coverage-weighted-10-2027`. All runs report `Completed` with weekly identity accounting and byte-identical annual codec round trips. New final gameplay SHA256 values are `43C29EB487F230862AE1B32C147E9F217CAB3D7FFA61AE453EAD161DD5AF137F` (25 seasons, 203,882,061 bytes) and `6111A690F0213EBDFA85BD5359A958F6BC86496745D1C0E83F542C233452C72A` (10 seasons, 81,014,762 bytes). These hashes identify the observed worlds; they do not substitute for the full release validation suite.

## Remaining limits

Fewer unavailable outcomes do not prove that every signing improves results or that club budgets are balanced. The preference can select a weaker eligible player over a stronger contested one. Stable club order remains an advantage when supply is scarce, and player refusals still matter. Starting population remains 864, below the planned approximately 1,200. Owned-club shortages under the no-recruitment diagnostic policy remain a separate limitation. The 100-world/fifty-season population and economy gates, durable-save endurance and clean-machine acceptance remain open. Playable careers still stop after three seasons.

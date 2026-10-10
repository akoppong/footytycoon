# Rival minimum-squad recruitment

The [weekly vacancy measurements](RIVAL_VACANCY_DIAGNOSTICS.md) exposed a rival shortage lasting 104 observed weeks. For 103 of those weeks the club met each role minimum, but still had fewer than the required sixteen players. Role minima sum to fifteen. The old emergency rule covered only missing roles; outside a transfer window, the sixteenth place could remain unfilled even with available players.

## Rule and compatibility

Schema 19 (`rival-minimum-squad-19`) lets a rival below sixteen seek available depth in any window state without requiring an ability improvement. The candidate must still fit its working role target: two goalkeepers, six defenders, six midfielders or four forwards. A squad with two goalkeepers cannot solve a total-only gap by collecting another goalkeeper. Missing roles remain the first ranking priority. Once the squad has sixteen, existing role-shortage and in-window improvement rules apply; this change does not replenish every club to eighteen.

The existing financial rules, retirement-age filter, candidate contention preference, stable resolution order, four-week cooldown and fallible acceptance remain. Planning and next-week resolution use the same need predicate, so a changed squad can close an offer without a signing. Agreement preserves the person and starts the quoted finite wage obligation the following week. No transfer fee, rescue money, extra graduate, waiver or replacement person is created. A depth signing may be weaker than every existing player in that role.

The owner still makes explicit recruitment decisions through the existing recommendation path. This unit changes only rival eligibility; it neither automates human spending nor widens the owner's outside-window recommendation rule. The playable boundary remains three seasons.

Migration validates schema 18 before changing only its schema and simulation markers. Existing people, finances, contract terms, offers, history and random states are preserved. Future planning and resolution use the new need rule, including for retained pending offers; targets and terms are never rerolled. Old previews must be refreshed because their world fingerprint changes. Source save bytes remain unchanged.

The diagnostic runner reuses this updated predicate. It no longer reports `OutsideWindowTotalGap`, which remains part of historical schema-18 evidence. A total-only gap with supply only in already-full roles reports `NoSuitableCandidate`. Other observational gate and episode definitions remain as documented in [ENDURANCE.md](ENDURANCE.md); pending/cooldown priority does not prove that hidden supply or finance checks would pass.

## Measurement method

Compare PreserveReserve with the existing CoverShortages owner policy at 25 seasons / seed 2026 and ten seasons / seed 2027. Baselines are the preserved schema-18 vacancy runs. Both versions use ordinary renewal/intake decisions, no owner injections or reserve exceptions, weekly identity conservation and exact annual codec round trips. The current runner is preserved at `artifacts/minimum-squad-runner`, based on `0a1f11e` plus this unit's core and diagnostic changes. Assembly SHA256: `41EEB57174FDD411D78FF31AC3477FC5F4C02EB641C3ABB937EF3FD8FA53AF0A`.

Current evidence is in `artifacts/minimum-squad-25-2026` and `artifacts/minimum-squad-10-2027`; baselines are `artifacts/rival-vacancy-25-2026` and `artifacts/rival-vacancy-10-2027`. Schema/simulation markers change encoded bytes even when recorded outcomes agree, and changed contracts may also affect later matches, finances and development. Gameplay hashes are therefore identities, not expected equality checks. Runs overlap other work and are not performance benchmarks.

## Completed comparisons

Both requested horizons completed with weekly person accounting and exact annual codec round trips.

| Observation | Schema 18 | Schema 19 |
|---|---:|---:|
| Seed 2026, 25 seasons: rival club-weeks below cover | 1,222 | 972 |
| Same run: any role gap / total-only gap | 837 / 385 | 935 / 37 |
| Same run: shortage episodes / longest episode in weeks | 230 / 104 | 240 / 29 |
| Same run: mean episode length in weeks | 5.31 | 4.05 |
| Same run: owned shortage weeks | 4 | 4 |
| Same run: combined club-weeks below cover | 1,226 | 976 |
| Same run: population minimum / maximum | 1,088 / 1,216 | 1,088 / 1,216 |
| Same run: weeks outside population target | 3 | 3 |
| Seed 2027, ten seasons: rival club-weeks below cover | 191 | 191 |
| Same run: any role gap / total-only gap | 190 / 1 | 190 / 1 |
| Same run: episodes / longest episode in weeks | 63 / 17 | 63 / 17 |
| Same run: owned shortage weeks / population misses | 2 / 0 | 2 / 0 |

Rival shortage time falls by 250 club-weeks (20.46%) in the longer run. Total-only gaps fall by 348, but role-gap observations increase by 98; this is not an improvement in every type of cover. All 972 current observations are pending-offer (423) or cooldown (549) snapshots. They do not establish that finance or supply checks behind those priority gates would pass. No episode remains open at either endpoint, and all 48 clubs meet contracted cover there. A different signing changes later selection, results, contracts and player availability; the aggregate role-gap increase does not establish a single causal mechanism.

The longer run finishes with 1,122 active people (847 contracted, 275 available), compared with 1,118 (845/273). Both have 1,168 graduates; retirements change from 1,250 to 1,246. Rival signings rise from 710 to 715. The unchanged diagnostic owner policy approves eight approaches and signs five players instead of seven/four; its final cash is £42,649,694 rather than £43,952,024. This reflects different later worlds and recommendations, not new owner authority. No blocked or missing owner recommendations occur in either longer sample.

Aggregate club cash grows to £2,213,000,723 from £2,205,752,988, so the high-cash economy concern remains. Both endpoints have zero clubs with arrears; that does not prove financial health every week. The shorter run's recorded cover, population, signings and endpoint finances agree with the baseline: 1,145 active people (859/286), 430 graduates, 485 retirements and £777,230,324 aggregate cash. Its owner approves three approaches and signs one player. The unchanged shorter sample limits how broadly the benefit can be claimed.

| Current run | Gameplay SHA256 | Uncompressed bytes |
|---|---|---:|
| 25 seasons | `A5EC6C6B3FB9B2CB71D875781C4F975299B329B537877285EAC274B2C0046A92` | 206,619,752 |
| Ten seasons | `777A583583A9A47ED96496F4468FB27078BED121A1286E53ED6EE5DA6686BC1D` | 83,175,180 |

## Validation

All 208 Release tests passed in `artifacts/minimum-squad-full-tests.log`. Twelve new cases cover weaker outside-window depth, the sixteen/seventeen-player boundary, working role caps, cash/wage/forecast/arrears restrictions, changed need at resolution, exact wage dates, loaded replay, refusal/cooldown, in-window improvement requirements and schema-18 migration preserving pending terms while rejecting stale quotes and corrupt input. Existing migration expectations now target schema 19. The initial fixture incorrectly placed retirement-age people in the available pool; valid ages, cleared training exposure and explicitly unaffordable alternative candidates corrected the setup. The corrected focused suite passed 32 tests before two additional in-window cases joined the full run. Independent source/test review cleared the final fixtures and rule.

Core and desktop formatting and the warnings-as-errors Release build passed. The self-contained package `artifacts/windows-70951d7d/FootballTycoon.exe` passed headless and rendered Windows/OpenGL three-season, rival-history, development and retirement probes with `DOTNET_ROOT` cleared. The graphical log contains RIVAL, DEVELOPMENT, SEASON, RETIREMENT and general SMOKE PASS markers; stderr is empty. The rival-history capture at 150% was inspected in `artifacts/minimum-squad-package-screenshots`. The portable ZIP `artifacts/FootballTycoon-minimum-squad-windows.zip` contains 199 entries: executable, pack, 194 runtime files and three font licenses. These checks ran on the development PC and do not satisfy clean-machine acceptance.

## Remaining gates

Two policy samples do not establish population sustainability or economy balance, nor replace the required hundred varied fifty-season runs. Owner recruitment, durable-save endurance, clean-machine exports, human playtests and the rest of the ownership game remain open. The change is a bounded correction to a measured rival cover gap, not a complete transfer market or a promise every club can sign someone.

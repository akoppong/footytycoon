# Owner replacement diagnostics

The [opening-population comparison](OPENING_POPULATION.md) left only the owned club below cover at both measured endpoints, while the diagnostic owner had made no free-agent approaches. Endpoint snapshots could not establish how much of the cumulative shortage belonged to that policy. This unit separates owner and rival shortage counts and adds an opt-in owner policy that considers affordable replacements through the existing proposal path.

Only the Endurance diagnostic assembly, its tests and documentation change. The production core, schema 18, three-season boundary and Windows package `windows-d10beb22` remain unchanged. The game does not gain automatic owner spending.

## Method

Each matched pair uses identical seeds, horizons and PreserveReserve opening strategy. Both accept recommended renewals/intake and then preserve reserve. `None` makes no owner approaches; `CoverShortages` reviews the director's recommendation while below cover and commits it only if ordinary checks pass. No injections, reserve exceptions, alternative-target searches, forced agreements, invented people or resets are used. The policy is not a model of optimal or actual human play. CLI and field definitions are in [ENDURANCE.md](ENDURANCE.md).

The runner records every eligible owner evaluation separately from approvals and actual signings. Blocked/no-recommendation states can recur. Weekly shortage counts partition the owned club and the 47 rivals, counting each short club once regardless of the number of gaps. Season-boundary shortages remain counted, and requested endpoints stop before the next renewal. All runs retain weekly identity conservation and exact annual codec round trips.

The preserved Release runner is `artifacts/owner-replacement-runner`, based on `67f0865` plus this unit's diagnostic changes, assembly SHA256 `8F653B6E02BE071D0060FDCD2D347B8913022F7FA1BC918875E81F63CC790A2C`. Paired evidence uses `artifacts/owner-none-25-2026`, `artifacts/owner-cover-25-2026`, `artifacts/owner-none-10-2027` and `artifacts/owner-cover-10-2027`. The default policy is compared with the preceding preserved schema-18 worlds to check that added measurements do not change gameplay. Runs overlap other checks, so elapsed times are not controlled benchmarks.

## Completed comparisons

| Observation | None | CoverShortages |
|---|---:|---:|
| Seed 2026, 25 seasons: owned weeks below cover | 416 | 4 |
| Same run: rival club-weeks below cover | 1,120 | 1,222 |
| Same run: combined club-weeks below cover | 1,536 | 1,226 |
| Same run: approved owner approaches / actual signings | 0 / 0 | 7 / 4 |
| Same run: owned squad at endpoint | 14 | 17 |
| Same run: active population minimum / maximum | 1,089 / 1,216 | 1,088 / 1,216 |
| Same run: weeks outside population target | 3 | 3 |
| Seed 2027, ten seasons: owned weeks below cover | 52 | 2 |
| Same run: rival club-weeks below cover | 191 | 191 |
| Same run: combined club-weeks below cover | 243 | 193 |
| Same run: approved owner approaches / actual signings | 0 / 0 | 3 / 1 |
| Same run: owned squad at endpoint | 15 | 16 |
| Same run: weeks outside population target | 0 | 0 |

The endpoint-only view understated rival shortages: rivals accounted for 1,120 of the default longer run's 1,536 short club-weeks, despite every rival having adequate cover at its final snapshot. Owner replacement decisions sharply reduced the owner's shortage time in these two examples. They did not remove the broader problem: rival shortage time increased in the longer pair, and three population breaches remained. A changed signing affects later player availability, wages, selection, results and contracts; the aggregate rival difference is not itself proof of a particular competition or affordability cause.

Every recorded evaluation in these two CoverShortages runs resulted in an approved approach; blocked and no-recommendation counts were zero. Approvals did not guarantee agreement: only four of seven became owner signings in the longer run, and one of three in the shorter. Other seeds or financial states may produce blocked/missing recommendations; regression tests exercise those paths. No claim is made that this policy is optimal, profitable or representative of real players.

At both CoverShortages endpoints all 48 clubs meet contracted squad cover. The longer pair both finish with 1,118 active people, 1,168 graduates and 1,250 retirees; contracted/available populations shift from 837/281 to 845/273. The shorter pair ends with 1,146 versus 1,145 active people, with 430 graduates in both and 484 versus 485 retirements. Neither final census summarizes every intervening week.

The owner pays for the additional contracts: its final cash is £43,952,024 versus £48,125,912 after 25 seasons and £12,202,069 versus £12,284,389 after ten. Aggregate club cash remains high at £2,205,752,988 versus £2,211,062,046 in the longer pair, and £777,230,324 versus £777,256,964 in the shorter. All four endpoints have zero clubs with arrears. These observations neither prove weekly financial health nor resolve the economy gate.

All four runs report `Completed`. Both None worlds match the preceding schema-18 gameplay hashes and byte counts exactly, confirming that the extra observations did not change those runs:

| Run | Gameplay SHA256 | Uncompressed bytes |
|---|---|---:|
| None, 25 seasons | `433C38659A42B8119A8E37E14F06136DFB9E26BEF4FEDD6C1925AFC1D4742160` | 206,364,302 |
| CoverShortages, 25 seasons | `E34B9268F60C75E43B122C8323BFBBD39FE40666E5BEEA65DF667B58DD4EAB55` | 206,478,467 |
| None, ten seasons | `5DD0E1E887B5FA584EBB8B8F3A4ACC0CEC46899B2CD8A10D9459B58E667B2D3C` | 83,151,710 |
| CoverShortages, ten seasons | `85824FA6E0D70327B69081492E697C1CDDF56E3800A20D602482D4FF43C225D5` | 83,175,179 |

## Validation and next work

The full Release suite passed 178 tests, followed by all seven final owner-policy regressions (including two added after that full run). Coverage includes disabled/healthy/pending/boundary no-ops, exact ordinary mandate terms, no cash or obligation on approval, saved replay and identity conservation, blocked/missing recommendations without mutation, total-squad-only gaps outside a window, coverage partitioning and invalid modes. The six-season integration case requires real runner approvals, a signing and JSONL/summary agreement. Format verification passed. Invalid CLI policy and nonempty output directory both exit 64; invalid policy creates no output directory.

The next evidence needed is the duration and cause of rival vacancies, separated from owner choices: recurring total-squad gaps, missing role supply, cooldown/refusal and financial restrictions may need different changes. Improving how the playable game surfaces existing director recommendations also needs a product-flow review. The two matched examples do not satisfy the 100 varied fifty-season worlds, economy, durable-save, clean-machine or human-playtest gates. The shipped package and schema remain unchanged.

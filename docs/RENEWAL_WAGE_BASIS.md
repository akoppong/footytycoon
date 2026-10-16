# Renewal wage basis

The [cashflow measurements](ECONOMY_CASHFLOW.md) showed actual annual wage payments falling from £82.74m to £31.53m over 25 seasons. A read-only cohort trace reproduces that schema-20 world exactly and finds 841 academy graduates among 847 contracted players at the endpoint. Of 529 academy players aged at least 25, 168 meet their current division's ability standard; 82 of those still earn less than half its wage reference.

For example, at year ten, Jordan Harding is 25 with ability 76 in Division 1, paid £540/week against its £2,720 reference. By year 25, Division 3 keeper Emeka Jennings is 37 with ability 60, paid £110/week against £1,105. These are selected fictional players, not external salary benchmarks. The trace establishes persistent low-reference pay, not an isolated causal decomposition of all wage decline. Its endpoint annualized current contracted pay is £31,802,160, distinct from the £31,527,520 actually paid during that final year.

## Prospective contract rule

Schema 21 (`renewal-wage-basis-21`) extends the existing promotion/relegation basis to every renewal offer: start halfway between current pay and the next season's division wage reference, then apply the existing standing/age adjustment, round to £10/week and apply the existing floor. A regular on £340 in Division 2 receives a £1,020 renewal quote before any wage-rule hold. A regular on £3,400 receives £2,550. At fixed standing and division, repeated low-wage quotes approach the reference rather than retaining the academy starting wage indefinitely.

This is a fictional tuning rule, not a market simulation or a guarantee of economy balance. Release and retirement recommendations, role/total cover rules, contract lengths, the 75% wage limit and the fallback that holds raises at current wages are unchanged. The owner still explicitly confirms the renewal choices; rivals apply the same pricing policy. The quote explains the basis and shows actual proposed terms. A percentage discount to the basis can still mean an increase against current pay.

Existing signed contracts are never repriced early. Only expiring contracts receive new offers. Confirmation creates exactly the approved finite obligations starting the following week; non-expiring contracts keep their dates and wages. Academy entry contracts and free-agent quotes keep their existing rules. Schema-20 migration validates first and changes only the two version markers, preserving people, cash, history, random states and pending mandates. Old previews become stale and must be refreshed. The playable boundary remains three seasons.

## Matched policy samples

Both versions use PreserveReserve at opening and thereafter, ordinary recommended annual renewals/intake, and CoverShortages owner recruitment without injections or reserve exceptions. Baselines are the preserved schema-20 cashflow runs. The updated runner is `artifacts/renewal-basis-runner`, built from `8a2665b` plus this unit's core changes, assembly SHA256 `F55E6864B37C3CF60DB806C9E3C30E8962B6632142EEFB999C3963323023ADC8`. Results are in `artifacts/renewal-basis-25-2026` and `artifacts/renewal-basis-10-2027`.

| Measure, pounds unless noted | Schema 20 | Schema 21 |
|---|---:|---:|
| 25 seasons / seed 2026: final club cash | 2,212,553,023 | 2,020,802,093 |
| Cumulative wage payments | 1,437,419,650 | 1,629,170,580 |
| Final-year wage payments | 31,527,520 | 56,682,650 |
| Final-year cash increase | 111,206,031 | 86,050,901 |
| Final owned-club cash | 42,487,694 | 39,291,534 |
| Ten seasons / seed 2027: final club cash | 777,230,324 | 781,153,634 |
| Cumulative wage payments | 727,611,920 | 723,688,610 |
| Final-year wage payments | 68,329,820 | 67,975,850 |
| Final-year cash increase | 75,216,958 | 75,570,928 |
| Final owned-club cash | 12,202,069 | 12,446,599 |

The longer sample has £191,750,930 less accumulated cash, while the shorter sample has £3,923,310 more. The basis can reduce high historical pay as well as raise low pay, so improvement in aggregate cash is not uniform. Cumulative non-wage category flows agree exactly in each matched pair; these cash differences reconcile to the wage-payment differences. Neither sample has endpoint arrears.

Population and shortage counters remain the same in each pair. The longer run ends with 1,121 active people (847 contracted, 274 available), ranges 1,088–1,216, and has three out-of-target weeks, seven owner shortage weeks and 960 rival club-weeks below cover. The shorter run ends with 1,145 active people (859/286), ranges 1,145–1,223, and has no out-of-target weeks, two owner shortage weeks and 191 rival club-weeks below cover. This rule does not resolve the population gate. Broad rival investment/trading policies, varied scenarios and larger acceptance samples remain needed.

## Reproducibility and validation

The read-only cohort harness is in `artifacts/wage-investigation`, with output `artifacts/wage-investigation-25.jsonl`. It references the preserved schema-20 cashflow runner and observes season-close and renewal snapshots. Its final gameplay hash `80BAC68183023FDFAC53C8017FE135D16F9491257252654E9403F7DF5521AC7A` and 206,640,090 uncompressed bytes match the baseline. It does not alter contracts or consume random draws. The comparison script `artifacts/renewal-basis-compare.ps1` produces `artifacts/renewal-basis-comparison.txt`, including both matched samples, cash reconciliation and cohort totals.

Both updated samples complete weekly identity checks and exact annual codec round trips. Their gameplay hashes are `27296ACB7641C1977ACBE010FC75434DB1A6A342536BD22B5A79481F0D11284B` (25 seasons, 206,878,763 bytes) and `1440529D88A29397E05E7DC76875DAA9F42A62BDEA994B71F80D77646B37969E` (ten seasons, 83,218,516 bytes). Runs overlap other validation, so timings are not controlled performance evidence.

All 235 Release tests pass, including nine new renewal-basis cases for low/high pay, standing and age adjustments, fixed-standing convergence, wage-limit holds, exact confirmed obligations, loaded replay, non-expiring terms and migration with pending mandates. The initial tests needed two fixture corrections: the wage-limit stimulus was too small, and a reused command ID correctly prevented a second season's capital-plan commit. The corrected focused suite passes 26 tests. Source and tests received independent review with no actionable findings. Package and final evidence review status is recorded in [delivery progress](DELIVERY_PROGRESS.md).

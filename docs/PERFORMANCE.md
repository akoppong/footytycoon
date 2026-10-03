# Measured simulation performance

The endurance pilot made history growth measurable before the playable career expands. The first optimization keeps simulation rules, validation coverage, quote identities and saved bytes unchanged. It does not prune old matches, cash movements or people.

## Profiling and changes

Only the diagnostic assembly compiles timing scopes. Counters reset for each synchronous runner invocation and are thread-local so independent tests do not mix results. The summary's `Timings` map contains call counts and elapsed milliseconds for world validation, eligible revenue, cash projection, fingerprinted forecasts and forecast identity work. Scopes can nest: identity and projection time are included in forecast totals, so their values must not be added together as independent costs. These are scoped wall-clock measurements, not a CPU sampling profile.

A four-season seed-2026 PreserveReserve run took 60.77 seconds. Validation accounted for 43.98 seconds across 227 calls; forecast identity work took 9.10 seconds. This identified validation as the first target.

- Validation now builds temporary fixture and identity indexes on each call. Missing and duplicate references, invalid cup winners, living/retired overlap and continuing free-agent wage obligations still fail. No index survives a mutation or load.
- Reconciliation accumulates each tracked account in one journal pass, preserving checked intermediate sums and entry order within each account. Club/owner opening balances still reconcile exactly. External seller accounts remain outside those balance checks.
- `Finance.ProjectCash` computes the existing 52-week cash path without serializing the world. Rival and pending-signing affordability checks use it because they never consumed a forecast ID. Owner previews continue through `Finance.Forecast`, which uses the same cash path and the unchanged full-world fingerprint. This is not a relaxed quote-validation rule or a stored forecast cache.

## Observed comparisons

All four-season runs used seed 2026, PreserveReserve, the same horizon and the ordinary diagnostic policy. Their final gameplay SHA256 is identical: `BE864A798F6B81D7D9CB2E05790F67C08B4B340ED11320B5B8BDC1567547D5AC`, with 32,232,266 uncompressed bytes.

| Four-season run | Total elapsed | Validation | Forecast identity | Longest weekly call |
|---|---:|---:|---:|---:|
| Before optimization | 60.77 s | 43.98 s | 9.10 s | 2,245 ms |
| Local validation indexes | 18.53 s | 1.17 s | 8.69 s | 1,674 ms |
| Indexes and internal cash projections | 11.05 s | 1.11 s | 1.00 s | 89 ms |

Evidence is in `artifacts/profile-before`, `artifacts/profile-validation-after` and `artifacts/profile-final`. The final run makes 16 fingerprinted owner forecasts instead of fingerprinting all 147 cash calculations.

The ten-season repeat (`artifacts/performance-ten-season`) completed in 78.65 seconds, versus the earlier pilot's 479.05 seconds. Its final state remains exactly 80,372,993 bytes with SHA256 `0525BE1D73FC85B45E9FD7F8089AFA1514EDA116194E28A1C1B72D4B023827F8`. All 21 nonconfiguration JSONL observations match the original pilot exactly, including population, retirements, club finances and role cover. Both runs overlapped other development checks for part of their execution; these are local observations, not hardware-normalized speed guarantees or the PRD performance gate.

Validation regressions deliberately corrupt fixture links, duplicate IDs, cup winners, balances and free-agent wages, and exercise checked intermediate overflow and interleaved accounts. Projection regressions check exact finite wage dates, quote/cash-path agreement, preview purity and full-world identity changes outside the cash path. See [delivery progress](DELIVERY_PROGRESS.md) for executed test, desktop and package evidence.

A subsequent 50-season request with a five-minute limit (`artifacts/performance-bounded-fifty`) returned `Incomplete` after 24 completed seasons, at week 1,249. Its 306.65 seconds include the operation that crossed the soft time limit; no simulation exception was reported. The longest weekly call was 874 ms and the final uncompressed world was 194,128,300 bytes. Annual active population fell below the 1,100 target in seasons 14–23. The run accumulated 6,169 club-weeks below minimum cover and 781 weeks outside the population target. These are partial observations, not a completed fifty-season result.

The larger world still needs measured work: eligible-revenue lookups, annual proposal cloning, snapshot size and durable saves remain costs. Population and economy balance are unchanged. A faster run does not satisfy the 100-world/50-season lifecycle gate or authorize longer playable careers.

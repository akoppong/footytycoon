# Academy candidate frequency experiment

This isolated experiment tests whether a modest increase in academy candidates can reduce the season-close population troughs reported in the [fifty-season pilot](FIFTY_SEASON_PILOT.md). It is not a production balance change. The playable build remains schema 22 with its existing academy rule and three-season boundary.

## Rule and provenance

The source is commit `1b32e88`, the schema-21 renewal-wage baseline used by the matched comparison runs. An archive of `src/FootballTycoon.Core` and `src/FootballTycoon.Endurance` was extracted to `artifacts/academy-frequency-experiment`. Only four files differ from that source:

- `Academy.cs`: replace the single three-bucket count draw with a single five-bucket draw. Zero gives no candidates, one or two gives one, and three or four gives two. The nominal probabilities change from equal thirds to 20% / 40% / 40%, raising expected candidates per club-year from 1.0 to 1.2 before admission checks.
- `Model.cs` and `WorldFactory.cs`: label the isolated world `academy-frequency-experiment-21`.
- `EnduranceRun.cs`: add the experiment description to its configuration record.

Names, role weights, ability generation, private random stream, maximum two admissions, squad cap, wages, contract lengths, affordability checks, owner approval and retirement rules are unchanged. Changing the count modulus remaps each club-year's draw: some cohorts become smaller. This is not a monotonic addition of people to the baseline. Later sporting and financial differences include downstream effects.

The experimental runner SHA-256 is `1BAE1A695B6E661800CCE8D3ECC107710478DD09259ED6948C4328CEA13CE94B`; its build version is `1.0.0+1b32e88.academy-frequency-experiment`. The baseline runner is preserved separately under `artifacts/renewal-basis-runner`, SHA-256 `F55E6864B37C3CF60DB806C9E3C30E8962B6632142EEFB999C3963323023ADC8`. These are schema-21 comparisons, not endurance evidence for acquisition schema 22.

Both policies accept recommended renewals and affordable academy intake, and approve ordinary affordable owner shortage recommendations. They make no injections or emergency player creation. The ten-season / seed-2027 and 25-season / seed-2026 pairs select PreserveReserve each year. The fifty-season / seed-2028 pair selects Training for the opening plan, then PreserveReserve. Compare each experiment with its matched baseline; differences across horizons also change seed and, for the longest pair, opening strategy. Existing evidence directories must never be overwritten.

## Reproduction

The experimental binary was built with:

```powershell
./.tools/dotnet/dotnet.exe build artifacts/academy-frequency-experiment/src/FootballTycoon.Endurance/FootballTycoon.Endurance.csproj -c Release -m:1 -p:InformationalVersion=1.0.0+1b32e88.academy-frequency-experiment -p:IncludeSourceRevisionInInformationalVersion=false
```

All three completed invocations use the same binary:

```powershell
$runner = 'artifacts/academy-frequency-experiment/src/FootballTycoon.Endurance/bin/Release/net10.0/FootballTycoon.Endurance.dll'
./.tools/dotnet/dotnet.exe $runner 10 2027 artifacts/academy-frequency-10-2027 PreserveReserve 30 CoverShortages
./.tools/dotnet/dotnet.exe $runner 25 2026 artifacts/academy-frequency-25-2026 PreserveReserve 60 CoverShortages
./.tools/dotnet/dotnet.exe $runner 50 2028 artifacts/academy-frequency-50-2028-training Training 60 CoverShortages
```

Raw `metrics.jsonl` and `summary.json` files live in each output directory. Baselines are `artifacts/renewal-basis-10-2027`, `artifacts/renewal-basis-25-2026` and `artifacts/renewal-basis-50-2028-training`. The read-only local extraction `artifacts/academy-frequency-compare.py` produces `artifacts/academy-frequency-comparison.json`; partial runs are explicitly excluded. The source archive, experiment manifest and build log are retained locally. These ignored artifacts are not included in the game package or this report's repository commit.

## Ten-season result: seed 2027

Both runs completed all 520 weeks, including weekly identity checks and ten annual exact codec round trips. Each has 21 cashflow samples; all 1,029 club-plus-aggregate reports per run reconcile original and trailing-window cash. Independent extraction also checks each census against the cash and arrears totals.

| Measure | Baseline | Experiment |
|---|---:|---:|
| Active population range | 1,145–1,223 | 1,187–1,232 |
| Out-of-target weekly observations | 0 | 0 |
| Final contracted / available | 859 / 286 | 863 / 330 |
| Graduates / retired people | 430 / 485 | 479 / 486 |
| Owner shortage weeks | 2 | 0 |
| Rival shortage club-weeks | 191 | 158 |
| Final aggregate club cash | £781,153,634 | £785,301,124 |
| Final-year wage payments | £67,975,850 | £67,094,120 |
| Final unpaid arrears | £0 | £0 |
| Uncompressed checkpoint bytes | 83,218,516 | 83,422,893 |

The endpoint has 48 more active people, but 44 of those additional people are available rather than contracted. Both policies already meet the population target in this sample. Shortages decrease, while accumulated club cash increases by £4,147,490 and final-year wage payments decrease. This is mixed evidence, not a general economy improvement.

The baseline gameplay SHA-256 is `1440529D88A29397E05E7DC76875DAA9F42A62BDEA994B71F80D77646B37969E`; the experimental hash is `5F69E5DEA2E42BB3CBB478D446FF9E5778E7FADA2028B20923FFEFDC350DA0C9`.

## Twenty-five-season result: seed 2026

Both runs completed all 1,300 weeks, including weekly identity checks and 25 annual exact codec round trips. Each has 51 cashflow samples and 2,499 reconciled club-plus-aggregate cumulative/window reports.

| Measure | Baseline | Experiment |
|---|---:|---:|
| Active population range | 1,088–1,216 | 1,187–1,349 |
| Out-of-target weekly observations | 3 | 0 |
| Final contracted / available | 847 / 274 | 851 / 465 |
| Graduates / retired people | 1,168 / 1,247 | 1,395 / 1,279 |
| Owner shortage weeks | 7 | 4 |
| Rival shortage club-weeks | 960 | 643 |
| Final aggregate club cash | £2,020,802,093 | £2,028,067,851 |
| Final-year wage payments | £56,682,650 | £56,410,910 |
| Final unpaid arrears | £0 | £0 |
| Uncompressed checkpoint bytes | 206,878,763 | 208,568,183 |

The experiment eliminates the three observed population breaches and reduces rival shortage time by 317 club-weeks (33.0%). It still has owner and rival shortages. Of the 195 additional active people at the endpoint, 191 are available and four are contracted. Final cash increases by £7,265,758. The results favor further population/coverage investigation while leaving the financial problem unresolved.

The baseline gameplay SHA-256 is `27296ACB7641C1977ACBE010FC75434DB1A6A342536BD22B5A79481F0D11284B`; the experimental hash is `7D014DA35ABE7671519D79D34F8578260879DB98434BD31160F6096BEE609D04`.

## Fifty-season result: seed 2028

Both runs completed all 2,600 weeks, including weekly identity checks and 50 annual exact codec round trips. Each has 101 cashflow samples and 4,949 reconciled club-plus-aggregate cumulative/window reports.

| Measure | Baseline | Experiment |
|---|---:|---:|
| Active population range | 1,077–1,225 | 1,186–1,363 |
| Out-of-target weekly observations | 2 | 0 |
| Final contracted / available | 808 / 269 | 829 / 449 |
| Graduates / retired people | 2,365 / 2,488 | 2,797 / 2,719 |
| Owner shortage weeks | 12 | 4 |
| Rival shortage club-weeks | 1,609 | 1,261 |
| Final aggregate club cash | £4,098,804,292 | £4,125,663,071 |
| Final-year wage payments | £59,200,920 | £57,850,880 |
| Final unpaid arrears | £0 | £0 |
| Uncompressed checkpoint bytes | 416,739,256 | 419,463,036 |

The experiment stays inside the population target throughout this sample and reduces rival shortage time by 348 club-weeks (21.6%). The final active population is 201 higher: 21 more contracted people and 180 more available people. Identity conservation remains exact: 1,200 + 2,797 − 2,719 = 1,278 active people. Both owner and rival shortages persist. Final cash increases by £26,858,779, and final-year wage payments decrease by £1,350,040. This continues the shorter samples' financial trade-off.

The baseline gameplay SHA-256 is `ACB95C74B72A9A1C6F8930E93B9BB15D79FE5CCA827D3CAA0D37DD7FF74F46AB`; the experimental hash is `4F20C51AED4D60EC1C93E04A9691FBDA493ECE568456C53719115CE7B850A16A`. Progress and completion are recorded in `artifacts/academy-frequency-50-2028.log`. Overlapping development work means elapsed timings are not controlled performance evidence.

## Decision limits

A completed horizon proves only that this policy and seed reached its requested endpoint with the runner's checks. It does not establish broad population balance, sustained squad cover, durable-save capacity or acceptable finances. Additional available players do not guarantee affordable or successful signings. Increased academy admission can also affect wage payments, match outcomes and cash accumulation.

Three matched samples, only one at fifty seasons, support further testing of population supply. They do not yet justify production promotion. A decision still needs varied seeds and policies, explicit assessment of the financial trade-off, and appropriate source, migration and desktop validation. The required 100 varied fifty-season worlds and human playtests remain open. The playable package and academy rule remain unchanged.

# Fifty-season pilot: renewal wage basis

One seed-2028 world completed all 50 seasons (2,600 weeks) under the schema-21 renewal policy. Weekly identity checks and all 50 annual byte-exact codec round trips completed. This is evidence that this particular direct-core simulation reached its horizon; it is not a population, economy, durable-save, performance or release acceptance pass. The playable game remains at three seasons.

## Configuration and provenance

The runner used Training for the first opening capital plan, then PreserveReserve each subsequent year. Its `CoverShortages` owner policy approved the director's ordinary affordable free-agent recommendations when short, without a reserve exception. It accepted recommended renewals and academy intake unless blocked, then declined intake. It made no owner injections, additional training investments, emergency player creation, debt forgiveness, pruning or autosaves. Rival policies were the ordinary core policies.

The preserved executable is `artifacts/renewal-basis-runner/FootballTycoon.Endurance.dll`, with SHA-256 `F55E6864B37C3CF60DB806C9E3C30E8962B6632142EEFB999C3963323023ADC8`. Its configuration reports `renewal-wage-basis-21` and build base `8a2665b8bbcd9466ba45615210b55eb34283cbf8`; it was built from that base plus the renewal change subsequently committed as `1b32e88`. It predates acquisition-counteroffer schema 22. Do not substitute a newly built runner when comparing hashes with this evidence.

The completed invocation was:

```powershell
./.tools/dotnet/dotnet.exe artifacts/renewal-basis-runner/FootballTycoon.Endurance.dll 50 2028 artifacts/renewal-basis-50-2028-training Training 60 CoverShortages
```

The existing output directory must be preserved; any repeat requires a new empty directory. Raw files are `metrics.jsonl` and `summary.json` in that directory, with progress in `artifacts/renewal-basis-50-2028.log`. The read-only extraction `artifacts/fifty-season-analysis.py` produces `artifacts/fifty-season-analysis.json` and checks the runner hash and cash bridges. These are local development artifacts, not packaged save files.

## Population and squad cover

| Measure | Result |
|---|---:|
| Active population range | 1,077–1,225 |
| Weekly observations outside 1,100–1,500 | 2 |
| Final contracted / available people | 808 / 269 |
| Graduates admitted / people retired | 2,365 / 2,488 |
| Owner weeks below squad cover | 12 |
| Rival club-weeks below squad cover | 1,609 |
| Longest rival shortage episode | 25 weeks |
| Clubs short at the final census | 9, all rivals |

The two out-of-range weeks were season 26 close (week 1,352, 1,085 people) and season 50 close (week 2,600, 1,077). The final report repeats the week-2,600 census; it is not a third out-of-range week. These are retirement/season-close observations before any subsequent renewal intake. The terminal season has no following intake. The population gate has no exemption for those boundaries, so this sample fails it. Identity accounting still reconciles: 1,200 opening people + 2,365 graduates − 2,488 retired = 1,077 active.

Of rival shortage observations, 1,581 had a missing role and 28 were total-squad-only gaps. The ordered end-of-week gates reported 754 pending-offer observations, 846 cooldown observations and nine career-boundary observations. They are observations after the week's processing, not a causal tally of denied signings. In particular, they do not establish that finances or candidate supply would have passed later checks. The owner approved 22 approaches; approvals are not guaranteed signings.

The next population investigation should focus on the season-close troughs and renewal intake timing, retaining named identities, the two-graduate-per-club cap and the published target. Shortages also need broader samples and actual approach outcomes; a full final squad alone would not establish reliable cover over a career.

## Cash and cost evidence

All figures below are pounds, converted from integer pence. Cash belongs to clubs, not the owner personally.

| Measure | Result |
|---|---:|
| Opening aggregate club cash | £72,800,000 |
| Final aggregate club cash | £4,098,804,292 |
| Final owned-club cash | £119,579,814 |
| Cumulative wage payments | £3,127,868,180 |
| Final-year wage payments | £59,200,920 |
| Final-year aggregate cash increase | £83,531,004 |
| Final unpaid club arrears | £0 |

The report contains 101 opening, season-close, renewal and final cashflow samples. Across all 4,949 club-plus-aggregate reports, original opening cash plus cumulative net cash movements equals closing cash, and reconstructed window-opening cash plus the trailing-window net movements also equals closing cash. Aggregate cash and arrears agree with the per-club census at every sampled point.

The wage reassessment does not by itself produce a balanced long career: this policy still accumulates substantial cash. This is a different seed, horizon and opening strategy from the matched ten- and 25-season runs in [renewal wage basis](RENEWAL_WAGE_BASIS.md), so it cannot isolate the causal effect of that change. These figures are cash movements, not profit, sustainable spending capacity or realized owner return. Broader rival investment/trading, demand constraints and the owner's distribution/exit systems remain absent. Adding unexplained costs just to suppress cash would not satisfy those requirements.

## Capacity and remaining gates

The final uncompressed checkpoint was 416,739,256 bytes, with gameplay SHA-256 `ACB95C74B72A9A1C6F8930E93B9BB15D79FE5CCA827D3CAA0D37DD7FF74F46AB`. It was encoded and checked in memory; it is not a durable, compressed, loadable production save. The run reported about 32.2 minutes elapsed and a maximum measured weekly simulation step of 3,881 ms. Other development work overlapped the run, and annual serialization is outside that weekly timer. These timings are not controlled performance acceptance evidence.

This pilot does not replace the required 100 varied 50-season worlds, scenario/strategy/difficulty balance samples, durable-save and migration stress tests, history-capacity work or human playtests. Acquisition schema 22 has separate short-career validation; this pilot makes no 50-season claim for that newer build. See [endurance methodology](ENDURANCE.md) and [complete-game delivery](DELIVERY_PROGRESS.md).

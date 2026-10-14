# Economy cashflow diagnostics

This report preserves schema-20 measurements. Schema 21 subsequently changes [prospective renewal wage offers](RENEWAL_WAGE_BASIS.md); its matched comparison is documented separately.

The schema-19 rival recruitment sample accumulated £2.213 billion across clubs after 25 seasons. A large cash balance alone cannot show whether broadcast receipts, matchday income, wage changes or missing investment policies drive the accumulation. The isolated endurance runner now records reconciled club cash movements before any economy tuning.

## Scope and interpretation

Each opening, season-close, renewal and final sample includes cumulative and trailing-52-week receipts/payments for every cash category, per club and in aggregate. Original cash plus cumulative net must equal the club's current cash. Cash at the rolling-window boundary is reconstructed from posted movements. Gross receipts/payments are retained even when the category net cancels. The report adds no saved fields, random draws, owner authority or simulation changes. Schema 20 and the playable three-season boundary remain unchanged.

This is cash accounting, not profit or a forecast. Owner and seller accounts are excluded; club injections and rescue receipts remain visible. Transfers between clubs cancel in aggregate net but still count in gross flows. Unpaid arrears are reported separately, and future signed commitments do not become payments early. Week-zero construction remains in cumulative totals after leaving the trailing window. The exact window convention and field definitions are in [ENDURANCE.md](ENDURANCE.md).

The diagnostic owner uses PreserveReserve at opening and thereafter, accepts recommended annual renewals/intake through ordinary checks, and uses CoverShortages for affordable director recommendations. It makes no injections or reserve exceptions. Rival investment and broader trading strategies remain incomplete. These samples do not represent varied human play, accounting profitability or the full economy acceptance suite.

## Method and provenance

Both samples use the policy above: ten seasons / seed 2027 and 25 seasons / seed 2026. Weekly identity conservation and exact annual codec round trips remain enabled. No histories are pruned. The updated runner is preserved in `artifacts/economy-cashflow-runner`, built from `e0ff1d7` plus only this unit's diagnostic changes, assembly SHA256 `F028444C3BB45905DF99A50BA0929A0254383E1A400F20A41ADED4888751B698`. Evidence directories are `artifacts/economy-cashflow-10-2027` and `artifacts/economy-cashflow-25-2026`.

The ten-season control uses `artifacts/economy-baseline-runner`, preserved from the preceding schema-20 source, assembly SHA256 `2CBE611B4D6B880310E60CB23CB90098CCD99B40E507C7B205C91C680DEFF541`. Its build version reports `77677ae` because it was compiled with the owner-minimum-squad changes before their commit as `e0ff1d7`; its simulation version is `owner-minimum-squad-20`. Its results are in `artifacts/economy-baseline-10-2027`. Compare every prior event field after excluding configuration provenance and the newly added Cashflow field, all non-timing summary fields, final gameplay hash and byte count. Timings overlap other validation and are not controlled performance measurements.

## Ten-season cash accumulation

The seed-2027 sample starts with £72,800,000 across 48 clubs and ends with £777,230,324, a net increase of £704,430,324. Cumulative club receipts/payments in pounds are:

| Category | Receipts | Payments | Net |
|---|---:|---:|---:|
| Broadcast | 676,000,000 | 0 | 676,000,000 |
| Sponsorship | 270,400,000 | 0 | 270,400,000 |
| Commercial | 162,196,716 | 0 | 162,196,716 |
| Tickets | 571,102,280 | 0 | 571,102,280 |
| Hospitality | 97,903,248 | 0 | 97,903,248 |
| Prize | 33,000,000 | 0 | 33,000,000 |
| Wages | 0 | 727,611,920 | -727,611,920 |
| Operations | 0 | 378,560,000 | -378,560,000 |

Purchase, injection, transfer, construction and rescue have zero club cash movements in this particular policy sample. Acquisition payments belong to the separate owner/seller accounts. Zero transfer fees do not mean zero recruitment: free-agent signings still create wages. Final unpaid arrears are zero; this is an endpoint observation.

The aggregate annual net inflow grows from £60,749,476 in year one to £75,216,958 in year ten. Actual annual wage payments fall from £82,742,400 to £68,329,820. This establishes cash accumulation and lower wage payments in the measured world; it does not isolate why wages change or prove that changing wage rules alone would balance the game. The sample finishes with 1,145 active people, no out-of-target population weeks, two owner shortage weeks and 191 rival club-weeks below cover.

## Twenty-five-season cash accumulation

The seed-2026 sample completes with £2,212,553,023 club cash, a £2,139,753,023 increase over the same £72,800,000 opening capital. Its cumulative net movements in pounds are:

| Category | Net |
|---|---:|
| Broadcast | 1,690,000,000 |
| Sponsorship | 676,000,000 |
| Commercial | 405,584,940 |
| Tickets | 1,425,172,455 |
| Hospitality | 244,315,278 |
| Prize | 82,500,000 |
| Wages | -1,437,419,650 |
| Operations | -946,400,000 |

All other club categories are zero in this sample. Annual wages fall from £82,742,400 in year one to £31,527,520 in year 25; the last year's cash increase is £111,206,031. Broadcast and sponsorship contribute £94,640,000 in that last year, while operations cost £37,856,000. This identifies the scale of the wage-payment decline and continuing income under the current policy. The next controlled investigation should separate squad size, academy wage bases and renewal wage paths before changing player terms or revenue tuning. Broader rival spending policies are still absent; PreserveReserve also deliberately avoids optional owner investment.

Final owned-club cash is £42,487,694 and aggregate unpaid arrears are zero. The final active population is 1,121; its range is 1,088–1,216 with three out-of-target weeks. The run records seven owner shortage weeks and 960 rival club-weeks below cover. Neither the population nor economy acceptance gate is satisfied.

This is a schema-20 sample, after the owner minimum-squad rule, so its world differs from the historical schema-19 rival report. It is not a paired estimate of the owner's rule change or an effect caused by these read-only diagnostics. Only the ten-season sample has a matched pre-diagnostic control in this unit.

## Replay and reconciliation evidence

Both runs completed with weekly identity checks and exact annual codec round trips. All 215 prior ten-season events agree after excluding configuration and the new report; all non-timing summary fields agree too. Its gameplay SHA256 is `E670D20CF08F3D694C4931A0AEBC363AA9A0AC475B12A8065DE32EF606704494`, with 83,175,180 uncompressed bytes in both versions. The 25-season hash is `80BAC68183023FDFAC53C8017FE135D16F9491257252654E9403F7DF5521AC7A`, with 206,640,090 bytes.

The 21 ten-season and 51 longer-run population samples each reconcile cumulative and rolling-window cash per club and in aggregate. Their overlapping observations are checked separately, not summed across time. Raw evidence remains in the directories above; `artifacts/economy-analyze.ps1` produces `artifacts/economy-analysis.txt` with comparisons, cash categories and annual totals.

## Validation

Seven targeted regressions cover opening cash, sampling purity/determinism, gross transfer cancellation, signed category movements, owner/seller exclusion, separate arrears and future commitments, exact window boundaries, week-zero spending, and rejection of inconsistent cash or invalid journal entries. All 226 Release tests passed; the final warnings-as-errors build and solution formatting check passed. An earlier build overlapped active tests and encountered locked output files; rerunning after the tests completed passed with zero warnings/errors. This diagnostic-only unit left production code and the Windows executable unchanged; its reviewed playable package was `windows-9af7f414`.

Population and economy tuning, the hundred varied fifty-season runs, durable-save endurance, clean-machine execution and human playtesting remain open. A completed run or reconciled cash bridge is not an acceptance pass.

# Three-season ownership loop

This milestone extends the supplied design-system interface into three playable seasons (156 career weeks). It does not implement the full 50-season PRD.

At the end of seasons one and two, time stops. The owner reviews a frozen closing report and previews next-season renewals before confirming. Annual broadcast and sponsorship renew at the next division's published tier rates. Expiring player and operating contracts extend one season at unchanged wages and costs. Existing wages, construction, upkeep, cash, personal reserve and arrears carry forward. Creditor deadlines do not restart at year-end.

Each new season generates 720 fixtures, resets standings and asks for a new capital plan. Original decisions, monthly reviews, results and closing tables remain in the saved world. History shows closing reports; Football and the season chart show the current season. The chart and current fixtures show season weeks 0–52; contracts, forecasts and saved history use labeled cumulative career weeks (season two is career weeks 52–104). A renewal preview shows the upcoming season’s cash path; fixtures appear after confirmation.

After week 52, wage affordability uses trailing 52-week ticket, hospitality and commercial receipts plus annual broadcast and sponsor terms. Owner injections and player sales are excluded. Forecasts include signed contracts and scheduled fixtures; future unsigned renewals are excluded until previewed or confirmed.

Schema-1 saves migrate in memory through the current schema 5; original snapshots remain unchanged. Completed old saves open at the first season review. Confirming a renewal follows the same durable candidate-write rule as other decisions, and loading preserves branch ancestry.

Remaining boundaries: no negotiated renewals, aging/retirement, loans, club sale or 50-season endurance claim. Promotion/relegation, tier-based commercial renewals, and the domestic cup are now implemented; see [implementation status](IMPLEMENTATION_STATUS.md) for their exact limits. Annual capital choices retain the existing reserve, hospitality and forward-search options. Rival rescue policies and snapshot pruning remain deferred.

## Validation

Validation completed on 17 September 2026:

- All 30 xUnit tests passed: the initial 29-test suite, the added rollover persistence test, and a repeat of all four season tests after strengthening migration and payment assertions. Tests cover three-season completion, separate tables, renewal preview purity, unchanged cash, wage and operations settlement after expiry, administration recovery/deadlines, old-format migration, and failed rollover persistence.
- Core, application, infrastructure, headless runner and desktop builds passed with no compiler warnings or errors.
- The native graphical smoke run completed all three seasons. The final exported executable also passed the complete UI and three-season smoke flows, including renewal confirmations, annual plans, final reports, saved resume, six workspaces and text scaling. Renewal layouts were captured at 100% and 150% and visually inspected; the existing workspace flow covers 100%, 125% and 150%.
- Final package: `artifacts/windows-810a4a7b/FootballTycoon.exe`, with adjacent `.pck`, runtime directory and three font licenses. Runtime evidence is in `season-smoke.log`; screenshots are under `artifacts/season-loop-export-screenshots`.
- Godot's exporter completed publishing and packing but stalled during shutdown. That verified task-owned exporter process was stopped; the package was then tested directly. The sandboxed runtime logged a root-certificate-store warning; offline gameplay and both smoke success markers completed. Clean-machine release acceptance remains outstanding.
- Independent reviews covered core logic, application/UI/headless changes, tests and documentation. Reported UI and test issues were fixed and rechecked; no actionable concerns remain in this bounded milestone.

The optional native probe runs with `--smoke-test --season-smoke-test`; it exercises renewal confirmations, annual plans and closing reports in addition to the existing UI smoke flow.

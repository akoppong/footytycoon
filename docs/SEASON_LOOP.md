# Three-season ownership loop

This milestone extends the supplied design-system interface into three playable seasons (156 career weeks). It does not implement the full 50-season PRD.

At the end of seasons one and two, time stops. The owner reviews a frozen closing report and previews next-season renewals before confirming. Annual broadcast and sponsorship renew at the next division's published tier rates. Operating contracts extend one season at unchanged costs. Expiring player contracts are decisions (see below). Existing wages, construction, upkeep, cash, personal reserve and arrears carry forward. Creditor deadlines do not restart at year-end.

## Player contract renewals

Every opening contract runs to career week 104 at one wage per division, and a signed forward's contract ends at the close of the following season. So the first renewal decisions arrive at the end of season two; the season-one review shows that no player contracts expire.

At the renewal review, Jonas Reed (sporting director) presents each expiring contract with one short reason, for example "Key player, 27: recommends 2 years at £1,960/wk". His policy is deterministic and judges each player against the division the club will play in next season. The standard is the midpoint of generated ability for that division (Division 1: 76, Division 2: 68, Division 3: 60):

- **Release** a player aged 31 or over who is less than 5 above the standard ("Aging"), or any player more than 5 below it. Candidates are taken weakest first. A release that would break the squad minimum becomes a one-year renewal at 10% less ("needed for cover").
- **Renew** everyone else. A key player (5 or more above the standard) gets a 15% raise, or 5% from age 30. A young prospect (23 or under, at or above the standard) gets 10%. A veteran (30 or over) takes a 10% cut. A squad regular keeps his wage. Length is 3 years to age 24, 2 years to age 29, then 1 year.
- After promotion or relegation, the raise or cut applies to a basis halfway between the player's current wage and the next division's standard squad wage (£2,720, £1,700 and £1,105 a week for Divisions 1 to 3). Otherwise it applies to the current wage. Wages round to £10 a week.
- If the recommended set would take squad wages over the 75% rule, the director holds every raise at the current wage, so accepting his advice is always allowed.

The owner accepts the recommendations or reverses any of them. Each expiring player has a keyboard-accessible button that switches renew and release and requotes the terms. **Accept all recommendations** clears every reversal. A player the director would release carries a one-year quote at 10% less in case the owner keeps him. The terms show renewed and released counts, annual wages before and after, the Wage budget (squad wages against 75% of eligible revenue using next season's broadcast and sponsor terms), and the expected and worst-case cash lows. The forecast runs the real rollover on a copy of the world, so it includes the chosen renewal wages and every release.

Two rules block confirmation, using the same blocking-reason pattern as other proposals:

- **Minimum squad:** 2 goalkeepers, 5 defenders, 5 midfielders and 3 forwards (a 1-4-4-2 plus one cover player per role) and 16 players in total. Releases that leave fewer are blocked with the specific shortfall.
- **Wage rule:** choices are blocked when squad wages would exceed 75% of eligible recurring revenue *and* be higher than today's wage bill. Existing contracts over the limit after relegation may continue; the rule stops net new wage commitments. There is no exception for this rule, as with recruitment.

Confirmation renews contracts from the following week (a new contract ID, end week and wage obligation). Released players leave the club on free transfers and are not re-signed elsewhere. Their wage obligations already ended with the old contract, so nothing more is paid. Contracts that are not expiring are untouched. Every rival club applies its own director's recommendations with the same squad minimum and wage rule, so squads stay viable and contracts expire in different seasons after the first wave. The "Season begins" review states the owned club's and the rival clubs' renewed and released totals. History keeps every reviewed contract with the director's recommendation, the owner's choice and the quoted terms.

Each new season generates 720 fixtures, resets standings and asks for a new capital plan. Original decisions, monthly reviews, results and closing tables remain in the saved world. History shows closing reports; Football and the season chart show the current season. The chart and current fixtures show season weeks 0–52; contracts, forecasts and saved history use labeled cumulative career weeks (season two is career weeks 52–104). A renewal preview shows the upcoming season’s cash path; fixtures appear after confirmation.

After week 52, wage affordability uses trailing 52-week ticket, hospitality and commercial receipts plus annual broadcast and sponsor terms. Owner injections and player sales are excluded. Forecasts include signed contracts and scheduled fixtures; future unsigned renewals are excluded until previewed or confirmed.

Schema-1 saves migrate in memory through the current schema 7 (`contracts-7`); original snapshots remain unchanged. Completed old saves open at the first season review. Schema 7 adds the owner's contract choices to commands and the reviewed contracts to renewal decisions; earlier decisions keep neither. Proposals are never saved, so an older save paused at a season review reopens at the same review and receives the director's recommendations; its previous preview cannot be committed because the migrated world has a different identity. Confirming a renewal follows the same durable candidate-write rule as other decisions, and loading preserves branch ancestry.

Remaining boundaries: no contract negotiation (players always accept the quoted terms), free-agent market or replacement signings for released players, aging/retirement, loans, club sale or 50-season endurance claim. Released players are removed from the world, so every club's squad can only shrink toward the minimum until recruitment covers all clubs. Promotion/relegation, tier-based commercial renewals, and the domestic cup are now implemented; see [implementation status](IMPLEMENTATION_STATUS.md) for their exact limits. Annual capital choices retain the existing reserve, hospitality and forward-search options. Rival rescue policies and snapshot pruning remain deferred.

## Validation

Contract renewals (24 September 2026): all 65 xUnit tests pass in Release with zero warnings, including eight `ContractTests` (recommendation rules and determinism, overrides executed and recorded, squad minimum and wage rule blocks, director holding raises, forecast including chosen wages, rival renewals, replay across save/load, schema 6 migration at a renewal review). The desktop Debug build has zero warnings. The native smoke run (`--smoke-test --season-smoke-test`) passed; renewal terms, the reversed recommendation, every page of the contract list and the final terms were captured and inspected at 100% and 150%.

Season loop validation completed on 17 September 2026:

- All 30 xUnit tests passed: the initial 29-test suite, the added rollover persistence test, and a repeat of all four season tests after strengthening migration and payment assertions. Tests cover three-season completion, separate tables, renewal preview purity, unchanged cash, wage and operations settlement after expiry, administration recovery/deadlines, old-format migration, and failed rollover persistence.
- Core, application, infrastructure, headless runner and desktop builds passed with no compiler warnings or errors.
- The native graphical smoke run completed all three seasons. The final exported executable also passed the complete UI and three-season smoke flows, including renewal confirmations, annual plans, final reports, saved resume, six workspaces and text scaling. Renewal layouts were captured at 100% and 150% and visually inspected; the existing workspace flow covers 100%, 125% and 150%.
- Final package: `artifacts/windows-810a4a7b/FootballTycoon.exe`, with adjacent `.pck`, runtime directory and three font licenses. Runtime evidence is in `season-smoke.log`; screenshots are under `artifacts/season-loop-export-screenshots`.
- Godot's exporter completed publishing and packing but stalled during shutdown. That verified task-owned exporter process was stopped; the package was then tested directly. The sandboxed runtime logged a root-certificate-store warning; offline gameplay and both smoke success markers completed. Clean-machine release acceptance remains outstanding.
- Independent reviews covered core logic, application/UI/headless changes, tests and documentation. Reported UI and test issues were fixed and rechecked; no actionable concerns remain in this bounded milestone.

The optional native probe runs with `--smoke-test --season-smoke-test`; it exercises renewal confirmations, annual plans and closing reports in addition to the existing UI smoke flow. At the season-two renewal it reverses one recommendation, checks that keyboard focus returns to that player's button, accepts all recommendations, reverses the same one again and confirms, then checks History records the override.

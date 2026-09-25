# Complete-game delivery tracking

Goal: a complete, compelling Football Tycoon game, from consequential ownership features to a polished design. The authoritative release scope remains PRODUCT_REQUIREMENTS.md, including its player-experience requirements and release gates. A green milestone is not completion of this goal.

## Current evidence and next work

The current development executable is artifacts/windows-341fe097/FootballTycoon.exe. It includes promotion/relegation, tier-based revenue renewals, historical division membership, the cross-season head-to-head correction, the domestic cup and midseason recruitment. This remains a three-season development milestone, not the complete game or a production release.

| Required area | Current state / evidence needed to finish |
|---|---|
| Ownership and finance | Acquisition, forecasts, journal and finite injections exist. Complete scenario selection, loan/refinance policies, distributions, covenants and accountable exit reconciliation. |
| Competition | League, yearly rollover, tier movement and domestic cup are implemented. Complete career records, meaningful run-in presentation and long-career endurance. |
| Delegated football | Opening search and a separate midseason shortlist/keep-squad review exist. Complete all-club market and rival strategy, contract negotiation, protected-player sale decisions and wage transition/embargo policy. |
| People and attachment | Named squad and fixed leaders exist. Complete staff hiring/mandates/succession and player aging, fatigue and rotation (injuries and suspensions exist), development, retirement, academy pipeline and historical callbacks. |
| Club development | Hospitality exists. Complete stadium, training and academy investment, cancellation/delays, condition, demand, prices, sponsor choices and supporter consequences. |
| Living world | Stable rivals and finances exist. Complete constrained rival recruitment/investment, credible distress/rescue and balance across 50 seasons. |
| Content and experience | Design-system shell and durable decision flow exist. Complete six acquisitions, difficulties, twelve event families/48 templates, linked arcs, ambition, onboarding and skippable major moments. |
| Presentation | Native layout, chart and typography exist. Finish distinctive fictional club art, sound, music, interaction polish and accessibility validation. See the [presentation proposal](PRESENTATION_PROPOSAL.md) for the phased plan. |
| Career and release | Three-season development boundary remains. Complete ten-year assessment, optional continuation to 50, robust history retention, migrations, Steam integration, cloud conflicts, achievements, demo/import and clean-machine acceptance. |
| Product proof | No player delight or production readiness claim. Run the PRD's usability, pacing, strategy, balance, performance and release checks with evidence matching each requirement. |

## Current unit: sporting consequences

Rules follow PRD G3: top two promoted, bottom two relegated between adjacent divisions, no playoffs, with the top and bottom outer boundaries closed. All movements use the same final tables. The renewal preview discloses next division and current-to-next broadcast/sponsor payments before the owner commits. Changes take effect in the next season; no advance windfall is booked.

Tier tuning reuses the authored factors (1.6 / 1.0 / 0.65). Future trading budgets use the ratio to each club’s opening tier, without rewriting prior receipts or compounding repeated moves. Existing player wages and operating commitments persist; negotiated clauses, final-position broadcast awards, relegation wage transition/embargo enforcement and lowest-tier supporter consequences remain work to finish, not implicit features of this unit.

Fixture division tags preserve historical membership. Schema 1 and 2 snapshots migrate in memory through schema 3 without altering the source. Old three-season-complete saves remain at their endpoint until the career-extension unit explicitly handles them.

## Recovery-browser correction

Native verification exposed a timeout when opening the accumulated checkpoint library: the browser decoded every full world before showing any saves. The browser now orders bounded headers and fully validates only the displayed page of 20 checkpoints. Older/newer navigation preserves access to the rest, and corrupt candidates remain on disk. Checksums, decompression bounds and world validation still apply to every displayed checkpoint and every load. This is paging, not deletion or a relaxed validation rule.

## Validation in this unit

Core and desktop builds passed without compiler warnings or errors. All 35 automated tests passed across the pyramid/season, remaining regression and added paging test runs. The pre-existing persistence tests were repeated after the reader refactor. Independent reviews found no outstanding code or test issues. The first native run exposed the recovery-browser timeout. After the paging correction, the native Windows graphical probe completed all three seasons with both SEASON SMOKE PASS and SMOKE PASS. Screenshots are in artifacts/pyramid-screenshots-final; inspected recovery, promotion renewal, new-tier capital plan and survival renewal screens. The runtime emitted the known sandbox root-certificate-store diagnostic but exited successfully. This verifies the development source, not a refreshed distributed executable.

## Current unit: domestic cup

Current source now runs the specified 48-club cup: 32 clubs play a preliminary round, 16 receive saved byes, and the resulting field plays five knockout rounds. Draws are deterministic and persisted. Level ties use extra time and, when needed, a recorded shootout winner; there are no replays. The Football workspace shows the run, next opponent, extra time, penalties and prize money. Season reports retain the final cup outcome.

Winner prizes are posted as actual cash only after a result. They never enter the recurring-revenue denominator. Forecasts include estimated gate receipts only for fixtures already drawn and exclude future draws and every unearned prize. League standings, promotion and league form use league fixtures exclusively.

Schema 4 adds competition history and migrates schemas 1–3 in memory. An old save at a season boundary receives that season’s opening draw; a save already underway starts the cup the following season, avoiding invented matches in elapsed weeks.

All 38 automated tests passed (artifacts/test-results/cup-final.trx). After the final legacy-table migration correction, its focused regression passed again (cup-legacy-final.trx). Tests cover the 47-match structure, byes and progression, replay identity, prizes, recurring-revenue exclusion, extra time, penalties, league isolation and schema migration. The desktop build passed with no warnings or errors. The native source probe completed all three seasons with SEASON SMOKE PASS and SMOKE PASS; inspected cup captures are in artifacts/cup-screenshots.

Migration testing now uses a cup-free legacy season. It also exposed and corrected reconstruction order: schema-1 fixture divisions are restored before rebuilding the historical league table. The regression requires 16 clubs with 30 played matches each, preserved cash and no invented historical cup fixtures. Legacy season reports explicitly say the cup was not held.

The fresh self-contained Windows package windows-ea39cc4f passed both the headless executable smoke test and the three-season Windows/OpenGL probe with DOTNET_ROOT cleared. The packaged probe exited with code 0 and both SEASON SMOKE PASS and SMOKE PASS; logs are beside the executable and screenshots are in artifacts/cup-package-screenshots. The known sandbox root-certificate diagnostic appeared, without preventing offline gameplay, persistence or rendering. The export script now waits explicitly for the direct engine executable and captures export logs, correcting Windows process-wait behavior. Code, tests, UI, packaging and documentation received independent review with no outstanding findings. Clean-machine acceptance and the broader production gates remain open.

## Current unit: midseason recruitment

The owner now receives a midseason review in weeks 24–27 with a first-choice forward, a lower-cost alternative and the option to keep the squad. New proposals show named targets and full fee/wage/contract terms, retain those terms in History, and enforce one midseason decision per season. Approval spends no fee; the next week's negotiation can fail and repeats affordability checks. Midseason targets leave rivals' two strongest forwards in place. See RECRUITMENT_MARKET.md for the exact scope and remaining market work.

All 46 tests passed in artifacts/test-results/market-final.trx. After adding persisted historical mandate terms, all eight recruitment cases passed again in market-history-final.trx. The desktop build is clean. The graphical source probe completed with MARKET SMOKE PASS and SMOKE PASS, covering all three options at 100%/150%, an explicit reserve exception, final confirmation, pending-save reload and the week-25 outcome. Captures and logs are under artifacts/market-screenshots-verified and artifacts/market-verified-smoke*.log. The first probe exposed its own incorrect assumption that the hospitality-first career could approve without a reserve exception; the corrected probe reviews that explicit exception through the actual UI.

Independent reviews cleared the core, tests, read-model, UI, saved history terms, smoke flow, packaging and recruitment documentation. The fresh schema-5 package windows-341fe097 passed its headless executable smoke and the full Windows/OpenGL career probe with DOTNET_ROOT cleared. The graphical run exited with code 0 and MARKET SMOKE PASS, SEASON SMOKE PASS and SMOKE PASS: it approved and resolved the midseason mandate, then completed all three seasons. Logs are in the package directory (career-smoke.log and career-smoke-errors.log); captures are in artifacts/market-package-screenshots. Only the previously documented sandbox certificate-store diagnostic appeared. Packaging now waits for the direct Godot process rather than reusable MSBuild descendants; the corrected script passed runtime verification and independent review. Clean-machine acceptance, the broader contract market, leadership decisions and full-game requirements above remain open.

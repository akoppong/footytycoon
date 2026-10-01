# Complete-game delivery tracking

Goal: a complete, compelling Football Tycoon game, from consequential ownership features to a polished design. The authoritative release scope remains PRODUCT_REQUIREMENTS.md, including its player-experience requirements and release gates. A green milestone is not completion of this goal.

## Current evidence and next work

The current development executable is artifacts/windows-f618441b/FootballTycoon.exe (schema 10, training investment and annual player development). It includes promotion/relegation, tier-based revenue renewals, historical division membership, the cross-season head-to-head correction, the domestic cup and midseason recruitment. This remains a three-season development milestone, not the complete game or a production release.

| Required area | Current state / evidence needed to finish |
|---|---|
| Ownership and finance | Acquisition, forecasts, journal and finite injections exist. Complete scenario selection, loan/refinance policies, distributions, covenants and accountable exit reconciliation. |
| Competition | League, yearly rollover, tier movement and domestic cup are implemented. Complete career records, meaningful run-in presentation and long-career endurance. |
| Delegated football | Opening search and a separate midseason shortlist/keep-squad review exist. Complete all-club market and rival strategy, contract negotiation, protected-player sale decisions and wage transition/embargo policy. |
| People and attachment | Named squads, availability, annual aging/development reports, baseline academy intake and fixed leaders exist in source. Complete staff hiring/mandates/succession, fatigue and rotation, staff development effects, retirement, academy investment/pipeline progression and broader historical callbacks. |
| Club development | Hospitality and training investment exist in source. Complete stadium and academy investment, cancellation/delays, condition, demand, prices, sponsor choices and supporter consequences. |
| Living world | Stable rivals and finances exist. Complete constrained rival recruitment/investment, credible distress/rescue and balance across 50 seasons. |
| Content and experience | Design-system shell and durable decision flow exist. Complete six acquisitions, difficulties, twelve event families/48 templates, linked arcs, ambition, onboarding and skippable major moments. |
| Presentation | Native layout, chart and typography exist. Finish distinctive fictional club art, sound, music, interaction polish and accessibility validation. |
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

## Current unit: annual player development

All 48 clubs now age their players at season close. Younger players can improve, older players can decline, and recorded appearances influence the chance of growth. The same bounded rules apply to every club, with isolated saved random streams. Renewal recommendations use the updated age and ability. People shows broad outlooks and recent changes; expandable season reports retain each owned player's name, before/after ratings and evidence after departure. See [PLAYER_DEVELOPMENT.md](PLAYER_DEVELOPMENT.md).

Schema 9 migrates schemas 1–8 without changing saved ages or abilities or inventing past development. All 83 Release tests passed in `artifacts/test-results/development.trx`; core and desktop format checks passed. The desktop build had no warnings. The graphical source probe completed three seasons with DEVELOPMENT SMOKE PASS, SEASON SMOKE PASS and SMOKE PASS in `artifacts/development-smoke.log`. That run preceded the final expandable report controls; those controls and the revised smoke were independently reviewed with no outstanding findings.

On 29 September, the fresh self-contained package `artifacts/windows-4cd5ec6a` exported successfully and passed its headless executable smoke. The full graphical package probe passed DEVELOPMENT SMOKE PASS, SEASON SMOKE PASS and SMOKE PASS with DOTNET_ROOT cleared. Logs are beside the executable; screenshots in artifacts/development-package-screenshots include expanded reviews at 100% and 150%, visually inspected. Only the previously documented certificate-store diagnostic appeared. The earlier export attempt `windows-f58bb78b` stopped during font import and is not a usable build. No production readiness, clean-machine acceptance or player appeal claim is made.

## Current unit: training investment

Training is a fourth annual capital allocation, with three upgrade steps, 32-week delivery and additive upkeep from the following week. Actual weekly facility exposure improves younger players' annual growth chances with diminishing returns; it neither guarantees development nor reverses aging. Exposure survives transfers, is saved between weeks, and is recorded in the named season report before resetting. Quotes disclose costs and contain no invented hospitality receipts or resale proceeds. See [TRAINING.md](TRAINING.md) for the financial schedule and exact development policy.

All 92 Release tests passed in `artifacts/test-results/training-final.trx`. The first run exposed an incorrect test comparison of the quote's upfront payment against already-paid committed cash; the corrected test checks cash balances throughout and signed future flows separately. Independent review also caught missing Training support in the headless runner and stale scope documentation; both were fixed and rechecked. The CI strategy matrix now includes Training. Core and desktop format checks passed, and desktop compilation had zero warnings.

The schema-10 Windows package `artifacts/windows-f618441b` exported and passed its headless executable smoke. The full graphical package career passed DEVELOPMENT SMOKE PASS, TRAINING SMOKE PASS, SEASON SMOKE PASS and SMOKE PASS with DOTNET_ROOT cleared. Logs are in the package directory and captures in artifacts/training-package-screenshots. The headless runner also completed all three seasons; artifacts/training-headless.log records its outcome and gameplay SHA256. Only the known sandbox certificate-store diagnostic appeared. Training proposal and final-terms captures at 100% and 150% were visually inspected. The portable archive `artifacts/FootballTycoon-training-windows.zip` contains the executable, game pack, runtime and three font licenses.

Next gameplay work is the academy/replacement-player pipeline and retirement, followed by longer-career validation. Those systems are needed before removing the three-season boundary; annual aging and training alone do not support a credible 50-season world. The broader product and player-validation gates remain open.

## Current unit: player departure records

Contract releases now retain named snapshots for every club. People shows the owned club's departures, and historical match reports can continue to resolve names after a player leaves. Records preserve the actual departure date, role, age and ability without keeping the player in the active squad or extending expired wages. Schema 11 reconstructs older owner releases only when a confirmed renewal decision supplies the evidence; it does not invent unrecorded rival departures. See [PLAYER_HISTORY.md](PLAYER_HISTORY.md).

All 97 Release tests passed in `artifacts/test-results/player-history.trx`. Core and desktop formatting checks passed, and desktop compilation had zero warnings or errors. Independent review found no actionable code, test or scope-documentation issues. The existing distributable `windows-f618441b` and portable training ZIP remain schema 10; this source unit has not been packaged for distribution.

The graphical Windows source probe completed all three seasons with DEVELOPMENT SMOKE PASS, SEASON SMOKE PASS and SMOKE PASS in `artifacts/player-history-smoke.log`. Its read-model assertion retained the owned club and name for every displayed departure. The capture `artifacts/player-history-screenshots/Player-departures-100.png` was visually inspected: the named player, release date, reason and original age/ability are readable below the active squad. The known sandbox certificate-store diagnostic appeared in the error log; it did not prevent offline gameplay or completion. This run verifies development source, not a new packaged executable or clean-machine acceptance.

Player aging and training are published in [PR #9](https://github.com/akoppong/footytycoon/pull/9), with all CI checks passing. Departure records continue on `codex/player-career-records`, based on that PR. Retirement, academy intake, a free-agent market and longer careers remain open; the playable boundary is still three seasons.

## Current unit: baseline academy intake

The inherited academy now provides uncertain annual cohorts of up to two 17-year-olds per club. The annual renewal proposal shows each affordable graduate's identity, ability and wage terms, includes scheduled graduate wages in the forecast, discloses the full three-year commitment, and lets the owner decline the intake. All clubs use cumulative squad, wage, cash-reserve and arrears filters. Confirmed graduates enter the ordinary match selection, training and development systems; their original details remain in People and renewal history. Schema 12 migrates older saves without retroactive graduates. See [ACADEMY.md](ACADEMY.md) for the exact policy and remaining limits.

Review caught a UI bug where resetting contract recommendations could undo a declined academy intake. The reset now preserves the academy choice, and the desktop smoke exercises decline, contract override, accept-all, reconsider and final confirmation. The first automated run passed 104 cases and exposed two test issues: the pyramid wage assertion needed to include graduate wages, and a new payment test reused a previously committed capital-plan command ID. Both assertions were corrected before the final run.

All 106 Release tests passed in `artifacts/test-results/academy-final.trx`. Core and desktop formatting passed; the desktop build had zero warnings and errors. Coverage includes exact wage schedules, cumulative admission limits, owner-only decline, negative-downside recovery, immutable graduation records, deterministic replay, schema-11 migration and malformed records.

PR #9 merged while this unit was in progress. Its final report wording, toggle cleanup and additional migration test were integrated without conflicts and independently reviewed. All 107 integrated Release tests passed in `artifacts/test-results/academy-integrated.trx`. The CI run for [PR #10](https://github.com/akoppong/footytycoon/pull/10) passed core format/build/test/audit, desktop smoke, all four headless strategies and the final gate on commit `6109546`.

The source three-season probe passed ACADEMY SMOKE PASS, DEVELOPMENT SMOKE PASS, SEASON SMOKE PASS and SMOKE PASS in `artifacts/academy-smoke.log`. The first schema-12 package `windows-384e462e` also passed its full graphical probe. After integrating main, the refreshed self-contained package `windows-a241ab2f` passed export, headless executable smoke and the same full graphical career with DOTNET_ROOT cleared. Logs are beside each executable; the final captures are in `artifacts/academy-integrated-screenshots`. The academy proposal at 150% shows the full wage terms, total commitment and decline control with the chart collapsed. Graduation snapshots remain readable at 100% and 150%. Only the previously documented sandbox certificate-store diagnostic appeared during graphical execution.

The final portable archive `artifacts/FootballTycoon-academy-windows.zip` contains 199 entries: the executable, game pack, 194 runtime files and three font licenses. Extract the archive before launching. The Training headless career also completed all three seasons in `artifacts/academy-headless.log`; its runner now declines optional academy contracts when a blocked quote requires it, through the ordinary validated proposal path. No financial guard is bypassed. Clean-machine acceptance remains open.

This does not implement retirement, academy facility investment, a free-agent market or the long-run population target. The playable boundary remains three seasons; the broader production and player-validation gates remain open.

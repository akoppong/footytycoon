# E07 — Playing the staff mandate experiment

Open **Review staff mandates** on Owner Desk, then **Change staff mandates** in People. Choose the priorities for Callum Price and Jonas Reed. Review shows the saved pair, proposed pair and any approved offers that will remain in force. The week stays held until you confirm or leave. Confirming uses the durable mandate command. A failed write keeps the last saved instructions and the proposal available for retry.

Callum's Development instruction can give up to two eligible outfield players aged 23 or younger a same-role start in place of older starters. Each replacement can cost up to five ability points. Jonas's Lower-cost value instruction compares eligible free agents in the current recommendation's role using quoted weekly wages and the full contract term. The owner still approves each approach. The People screen shows the selected player, the alternative when one qualifies, the ability difference, wages and contract duration. Paid transfers continue through the existing recruitment flow.

Use **Show mandate evidence** in History to inspect dated changes, selections and recruitment outcomes. Match detail names the actual replacement and the displaced player. A pending offer retains its approval-time instructions after a subsequent change. Season detail separates the original quote, the eventual signing or closure, and payments already recorded. Older saves with missing wage attribution display it as unknown. Loading a checkpoint clears an outstanding draft and displays the instructions and history in that checkpoint.

## Build and engine checks

The UI consumes E05 commands and E06 immutable views. Its integration base is `d28b5eeb7b05ccb7f7607d1c9fb474898dc85902`, with 372 passing tests and successful CI. See [E06 PR #54](https://github.com/akoppong/footytycoon/pull/54) and [the evidence interface](OWNER_MANDATE_EVIDENCE.md).

Build the Desktop project with the repository .NET SDK in Debug configuration. Launch the bundled Godot 4.7.2 .NET executable with `--path src/FootballTycoon.Desktop` for normal play. To start from the registered checkpoint in a fresh profile, follow the [E01B recovery instructions](OWNER_MANDATE_BASELINE.md).

If a fresh clone lacks the ignored checkpoint, run `dotnet run --project src/FootballTycoon.Headless -c Release -- --playable-start <new-directory>` in a checkout of E01B commit `ca9614c65b3bc5d445d07c91be1a0946c9608e99`. Use its `2026/checkpoint.json` with the E07 build. [E01B PR #48](https://github.com/akoppong/footytycoon/pull/48)

For the E07 engine probe, set `FT_PLAYABLE_START_CHECKPOINT` to the original `artifacts/owner-mandate-e01b-20261025/run-3/2026/checkpoint.json` and `FT_SMOKE_OUTPUT` to a fresh output directory. Use dedicated `APPDATA` and `LOCALAPPDATA` directories. Add `-- --smoke-test --mandate-smoke-test` to the Godot launch arguments. An optional `--headless` before `--` runs the input and state assertions. Graphical execution also captures the screens at 100%, 125% and 150% text size. The result file records the source hash, final week, mandate history, actual selections and approach outcome.

The probe traverses Godot's focus order with Tab and activates controls with Enter. It exercises cancellation, held time, a failed candidate write and retry, same-revision recovery, free-agent approval, a subsequent director change, ordinary weekly advances through the first competitive selection, and inspection after saving and reloading. The failure injection and second same-revision checkpoint are isolated test fixtures. The measured season comparisons belong to E08. E09 participant sessions are awaiting volunteers.

## Verification on 3 November 2026

The corrected Debug build passed with zero warnings and errors. The headless mandate probe and the graphical OpenGL probe both reached week 58 from the registered source. Both recorded one successful retry after the injected write failure, two mandate changes, an executed Development selection and a signed offer retaining its Lower-cost value origin. The source SHA-256 remained `DC33DBA821D4A6A833D17477ECD46DBDE808467556648A54EFA0352D299FFF1D`.

Logs and result JSON are in `artifacts/owner-mandate-e07-20261103/headless-1/` and `graphical-1/`. The graphical output directory contains 23 screenshots. Inspection at each supported scale confirmed readable confirmation controls and wrapped player/contract comparisons. At larger sizes the central pane scrolls to additional details. The independent standard Desktop smoke also passed in `standard-1/`, covering acquisition, capital plans, funding, save/load and navigation. The initial probe build had a helper-scope error. Its correction was independently rechecked before the passing runs.

Desktop formatting verification passed after a whitespace correction to the probe result initializer. Independent source review covered the controls, the probe and the helper fix. Final screenshot and handoff review is recorded on the PR.
The three-season engine regression also passed. It exercised both a contract override and acceptance of recommended renewals, annual capital plans, three season reports and the final career endpoint. Its log is `artifacts/owner-mandate-e07-20261103/season-1/engine.log`.

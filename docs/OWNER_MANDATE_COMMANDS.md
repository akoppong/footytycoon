# E05 — Transactional mandate changes

E05 implements the confirmation and persistence behavior registered in [the experiment contract](OWNER_MANDATE_EXPERIMENT.md), using E02's existing schema and immutable types. Prerequisite E02 commit: `43dfb2520a827e46c5db6b8619640b64159325ff` ([PR #49](https://github.com/akoppong/footytycoon/pull/49), seven CI jobs passed). The integration checkout also contains reviewed E03. E04 is developed separately; neither behavior module is modified here.

## Public interface

| API | Result and responsibility |
|---|---|
| `MandateCommands.Preview(World, ChangeMandates)` | Pure `MandateProposal` containing both requested modes, current state, world fingerprint, explanation, pending commitments and blocking reasons. |
| `MandateCommands.Commit(World, MandateProposal)` | Applies one validated change to a caller-owned candidate and returns the shared `CommitReceipt`. Direct Core callers must publish only after saving. |
| `GameSession.PreviewMandatesAsync(ChangeMandates)` | Runs preview on the serialized application worker. |
| `GameSession.CommitMandatesAsync(MandateProposal)` | Clones the current world, validates/applies the command, saves recovery and candidate snapshots, then publishes the candidate. |
| Existing `GameView.Mandates` | Immutable current state/history from E02; querying adds no evidence. |

The command includes a fresh command ID, expected **world** revision and both target modes. Preview IDs bind the complete world fingerprint and command. Confirmation rechecks that identity, current state and displayed explanation/commitment/blocking fields. A same-revision preview from a different world fails. Invalid enum values, empty/overlong IDs, stale revisions and identical target pairs are rejected without applying a change. Invalid current world state also fails before mutation.

Changes are optional in Active ownership, including before the annual capital plan, and at a nonterminal SeasonReview. Acquisition, administration, lost control and the final endpoint block them. One confirmation changes both modes atomically, increments world and mandate revision once, and appends one mandate-history entry with effective week and resulting revision. Investment history is untouched, preserving its count-derived negotiation IDs. The week, financial state, RNG, required decisions and capital-plan selection remain unchanged.

Existing approved paid and free-agent offers remain in force, with the same candidate, terms, expiry and acceptance attempt. Their explanation names the player and quotes the existing commitment. The command preserves any originating evidence already present; legacy unknown origins remain unknown. It neither creates E06 approval evidence nor rewrites earlier decisions. The current pair persists through ordinary rollover and save/load.

## Retries and failures

Investment and mandate commands use the same `World.Commands` map. As explicitly required by E00, **any used ID returns its original receipt**, even if the new submission has a different payload or command family. No second effect or write occurs. The returned receipt retains the original proposal ID, revision and summary; it does not claim that the newly requested action happened. E07 must generate a fresh ID for each distinct action and compare the receipt's proposal ID with the confirmed preview before presenting that action as completed.

Before publishing a new mandate, GameSession saves the old live world as recovery, then the candidate. Failure of either write leaves the old live world authoritative. A successful recovery write may remain after candidate failure, following the existing transaction convention. Retry applies the change once. Continuing after a load forks on the first successful write and preserves the original source snapshot. An idempotent retry after loading returns the saved receipt without creating another branch or snapshot.

## Validation and handoff

Focused source tests cover pure previews, all unrelated fields, unchanged required capital decisions, stale and altered confirmations, invalid modes/current state, state eligibility, review/rollover, pending paid/free-agent terms and acceptance streams, cross-family receipts, same-revision different-checkpoint loads, concurrent retries, and recovery/candidate-write failure with source preservation and branch-on-write.

The combined E03/E04/E05 regression run passed **122/122 tests**, with zero failures or skips, covering manager/director mandates, mandate commands/state, persistence, free agents, owner/rival minimum squads and recruitment contention. Evidence: `artifacts/owner-mandate-e05-20261028/relevant-2/combined-relevant.trx`. The Release build passed with zero warnings/errors (`build-2.log`), and solution formatting verification passed (`format.log`) in the parent directory `artifacts/owner-mandate-e05-20261028/`. The earlier `relevant-1` run retained one fixture failure: directly changing the season number left the later schedule missing. The corrected fixture reaches the terminal boundary through ordinary plans, advances and renewals; production behavior was unchanged. Independent reviewer `e05_review` inspected source/tests/docs and rechecked that repair with no actionable concerns; the reviewer also checked the final validation evidence and corrected its log-directory wording. No material concerns remain. Full regression CI runs on the task PR; no UI was changed.

No UI, new required decision, salary, authority limit, schema migration, selection/recommendation ranking, or outcome writer is introduced. E06 may rely on the saved transition/history and existing decision-time origins. E07 still owns the confirmation interface. E08/E09 must establish integrated tradeoffs and real player evidence; these command tests establish neither product value nor human comprehension.

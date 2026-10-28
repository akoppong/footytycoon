# E04 — Director recommendation handoff

E04 implements the lower-cost value rule in [the experiment contract](OWNER_MANDATE_EXPERIMENT.md). Prerequisite E02 is `43dfb2520a827e46c5db6b8619640b64159325ff` ([PR #49](https://github.com/akoppong/footytycoon/pull/49), seven CI jobs passed). The selected integration base also includes reviewed E03 `088d2289894d7a6d6d236f954f7b415f60612495` ([PR #50](https://github.com/akoppong/footytycoon/pull/50), seven CI jobs passed). E01B's ordinary season-two starts remain the scenario gate; no opening-world changes are made.

## Behavior and interface

| Interface or choice | Meaning |
|---|---|
| `FreeAgents.Recommend(World)` | Existing caller-compatible API returning selected `RecruitmentTerms`, or null. |
| `FreeAgents.RecommendWithEvidence(World)` | Pure nullable `FreeAgentRecommendation`, containing `Terms` and E02 `RecommendationEvidence`. |
| CurrentRecommendation | Existing eligibility and ordering: shortage first, ability descending, age ascending, ID ascending. |
| LowerCostValue | From the same eligible pool, keep the current recommendation's natural role, allow at most five ability points less, and require both weekly wage and total signed commitment no higher, with at least one strictly lower. |
| Alternative ordering | Total commitment ascending, weekly wage ascending, ability descending, age ascending, ID ascending. |
| No alternative | Keep the current candidate and return `NoLowerCostAlternative`; if no current candidate exists, both modes return null. |
| Evidence | Decision week/world revision, both mandates/revision, current and selected player snapshots, actual weekly wage, wage-start/end weeks, total commitment and reason. E06 persists this when approved. |

The total is quoted weekly wage times inclusive wage weeks: approval week plus two through the quoted contract end. Free agents have no transfer fee. Lower weekly wages with a longer, more expensive total commitment do not qualify. A cheap player in another role cannot displace the baseline shortage priority. The bounds are registered experimental defaults, not player-research findings.

An ordinary controlled example at week two exchanges ability 90 at £2,000/week for ability 85 at £1,500/week, both through week 52. Actual commitment falls from £98,000 to £73,500 over weeks 4–52. A boundary case with a younger player's longer contract costing more in total retains the current recommendation despite a lower weekly wage. Equal-cost candidates also retain the current recommendation.

Review/window, retirement, squad/role cover, wage limit and cooldown filters remain unchanged. Ranking does not bypass or search around a blocked reserve preview. The owner still approves the approach, and execution still checks availability, affordability and acceptance. Pending approved terms and their acceptance attempt remain unchanged after switching. Paid recruitment and rival policies are unchanged. Preview/recommendation spends no cash, consumes no RNG and adds no saved evidence.

## Validation

The initial focused run passed 22/22 `DirectorMandateTests` at `artifacts/owner-mandate-e04-20261028/focused-1/director.trx`. It covers quoted costs and purity, deterministic ties, total-versus-weekly boundaries, eligibility/cover/windows, blocked reserve previews and pending-offer behavior. The combined E03/E04/E05 regression run passed **122/122 tests**, with no failures/skips, including these tests, free agents, owner/rival minimum squads, recruitment contention, command/state persistence and manager behavior. The TRX is `artifacts/owner-mandate-e05-20261028/relevant-2/combined-relevant.trx`; its parent directory, `artifacts/owner-mandate-e05-20261028/`, contains successful `build-2.log` (zero warnings/errors) and `format.log`.

`scripts/diagnostics/verify-director-previews.ps1` builds a narrow driver for the two registered E01B saves. It checks original gameplay hashes, loads each real `.ftsave` via the production vault/codec, verifies both modes through `Proposals.Preview`, checks exact terms/commitments and empty blockers, and confirms that source files and queried world bytes remain unchanged. Its coherent E02 diagnostic mandate transition is explicitly a behavior-test setup; it is not evidence for E05's public command. No candidate, cash, roster or eligibility override is used.

Both original E01B checkpoints passed actual Current and Value production previews with empty blockers, exact quoted commitments, pure world bytes and unchanged source saves. Evidence: `artifacts/owner-mandate-e04-20261028/previews-1/results/results.json`, with each seed's complete previews in the same directory. Run the script with `-CheckpointRoot artifacts/owner-mandate-e01b-20261025/run-3 -OutputDirectory <new-directory>`.

| Seed | Current → Value | Ability | Weekly wage | Signed commitment | Wage weeks |
|---|---|---|---|---|---|
| 2026 | Kai Clarke → Isaac Jarvis | 85 → 83 | £1,950 → £1,550 | £302,250 → £79,050 | 54–208 → 54–104 |
| 2030 | Kyle Sutton → Andrei Fletcher | 86 → 81 | £2,170 → £1,650 | £223,510 → £84,150 | 54–156 → 54–104 |

Lower total commitments include shorter contracted cover; they are not evidence of a costless long-term improvement. These are feasible proposals, not completed signings or measured season outcomes. Source gameplay hashes were checked against the registered E01B values; the diagnostic Core SHA-256 is `A33E245D87DF06C3314EF3621D7EBCEE40BC93EF079FCDD381F8DFFD3C72B458`.

Local diagnostics compile the shared E04/E05 working tree; E05's separate command helper changes the assembly hash but is not called by this driver. The task PR isolates E04 files, and its exact-head CI independently checks that scope. Raw evidence records the actual assembly hash rather than claiming it equals the E04-only commit.

## Review and next boundary

Independent non-author reviewer `e05_review` inspected the E04 source, tests and diagnostic scripts against the contract and found no actionable issues. The reviewer also checked the final handoff, combined test results and both full checkpoint previews; no material concerns remain. The review corrected the validation-log directory wording.

E06 may use the returned evidence at the actual original approval, preserving the origin when a bid resolves later. E04 deliberately does not write `MandateApproaches`, classify execution failures or render UI. E05 supplies public mandate confirmation; E08 must run all registered matched policies and actual previews; E09 still requires five real participants. A feasible preview proves neither acceptance nor product value, staff relationships or autonomous spending.

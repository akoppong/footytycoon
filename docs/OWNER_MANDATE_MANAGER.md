# E03 — Manager selection handoff

E03 implements the manager behavior registered in [the experiment contract](OWNER_MANDATE_EXPERIMENT.md). Its prerequisite is reviewed E02 commit `43dfb2520a827e46c5db6b8619640b64159325ff` ([PR #49](https://github.com/akoppong/footytycoon/pull/49), all seven CI jobs passed), which contains the E00 contract, E01 baseline and E01B playable checkpoints. The task branch is `codex/owner-mandate-manager`; PRs remain open for owner review. This is a technical behavior result, not player-study evidence.

## Behavior and interface

| Entry point or output | Contract |
|---|---|
| `Matchday.Select(Club, int)` | Existing readiness selection, unchanged. Rivals continue to use this overload. |
| `Matchday.Select(World)` | Pure owned-club selection at `world.Week`; returns a `SelectionDecision` without consuming random draws or mutating the world. |
| `SelectionDecision.Lineup` | Immutable array of player/position tuples in original lineup slots. |
| `DecisionWorldRevision`, `Origin` | Decision-time world revision and both mandates plus mandate revision. |
| `Swaps` | Immutable E02 player snapshots and ability sacrifice for each accepted replacement; empty for readiness or no credible alternative. |
| `Matchday.Play` | Uses the world overload for the owned club, at home and away, and the unchanged club overload for rivals. |

Development starts from the readiness lineup and its original available bench. An available player aged at most 23 can replace a native-role outfield starter older than 23 in the same role, sacrificing zero to five ability points. Pairs rank by sacrifice ascending, young player's ability descending, age ascending, role enum order, young player ID, then displaced player ID. At most two distinct pairs are accepted; players cannot be reused and replacements do not create new candidate pairs. Goalkeepers, out-of-role fallbacks and short-squad behavior retain readiness selection unless a qualifying native outfield pair exists.

An ordinary controlled example replaces two 30-year-old forwards of ability 70 with available forwards aged 23/19 and ability 68/65: the owner exchanges two and five readiness points for actual appearances. A 24-year-old, unavailable player or ability-64 alternative is ineligible; with no other credible pair, readiness is retained. These ages and thresholds are explicit experimental defaults, not research findings.

Actual saved match appearances feed the existing annual development calculation. The change adds no immediate ability bonus and guarantees neither improvement nor better results. Pure selection consumes no RNG; when a different lineup actually plays, subsequent football outcomes and random-stream consumption may differ normally.

## Validation and review

The final relevant run passed **45/45 tests**, with zero failures or skipped tests, across `ManagerMandateTests`, `MatchRealismTests`, `PlayerDevelopmentTests` and `TrainingTests`. It covers ranking/ties, the two-swap bound, availability, age and ability boundaries, goalkeeper and short-side fallback, default equivalence, owned home/away selection with unchanged rival selection, and a full controlled season with saved appearances and exact replay after a week-20 save/load.

Local evidence:

- `artifacts/owner-mandate-e03-20261027/relevant-2/manager-relevant.trx`: final relevant test results. Run with the Release test assembly and filter `FullyQualifiedName~ManagerMandateTests|FullyQualifiedName~MatchRealismTests|FullyQualifiedName~PlayerDevelopmentTests|FullyQualifiedName~TrainingTests`.
- `artifacts/owner-mandate-e03-20261028/build.log`: Release test-project build, zero warnings/errors.
- `artifacts/owner-mandate-e03-20261028/format.log`: solution `dotnet format --verify-no-changes --no-restore` passed.
- The earlier `focused-1/manager.trx` is retained: assertions compared immutable-array containers instead of their elements; the tests now compare element arrays. This was a test assertion repair, not a policy change.

Independent non-author reviewer `e03_review` inspected the source and tests and requested direct execution coverage for rival isolation. Both home and away cases were added, passed and rechecked; no source/test findings remain. The same reviewer inspected the final handoff document and validation evidence; no actionable concerns remain. The full suite is delegated to PR CI; this Core-only change needs no desktop UI smoke test. Controlled fixtures demonstrate mechanics, while E01B separately establishes the genuine playable scenario.

## Next-task boundary

E05 still owns the public mandate-change transaction. Tests currently construct a coherent E02 mandate transition solely inside controlled fixtures. E06 must capture the returned decision inside the actual match-selection hook, before post-match availability changes, and persist its snapshots against the executed fixture; it must not reconstruct them from later world state. E03 does not yet populate `World.MandateSelections` or expose a UI explanation. When serializing the tuple lineup for diagnostics, project named `Player`/`Position` properties explicitly rather than relying on default tuple-field serialization.

No schema, wages, financial guards, formation, match probabilities or growth formula changed. E04 director behavior, E05 commands, E06 evidence, E07 UI, E08 matched playable runs and E09 real participant evidence remain separate deliverables. This work establishes neither staff relationships nor autonomous spending, nor that players will value the tradeoff.

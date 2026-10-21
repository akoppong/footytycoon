# E03 — Make manager selection respond to the mandate

Depends on: integrated E02. Type: Core behavior. Read [shared instructions](README.md), AGENTS.md and the E00 contract.

## Outcome

A development mandate gives credible young players more opportunity while visibly accepting a bounded readiness cost. The owner still never picks a lineup.

## Work and ownership

- Own `Matchday.cs` selection and focused selection/development-integration tests. Inspect callers before changing the `Select` interface; preserve legacy/default call behavior.
- Implement the readiness/default and bounded development rules fixed in E00. Restrict mandate effects to the owned club; rival selection remains unchanged.
- Return the minimal deterministic explanation data specified in E02, where needed by E06. Keep selection free of random draws and unrelated side effects.
- Let actual saved appearances feed the existing annual development calculation. Do not add an immediate ability bonus or guarantee improvement.

## Boundaries

No new formation/tactics controls, match probability tuning, fatigue, morale, direct player training bonuses, roster generation, staff hiring or shared schema changes. If shared contracts must change, ask the coordinator to integrate that change before proceeding.

## Acceptance

- On a controlled plausible same-role roster, the two mandates select different eligible players as specified; explain the readiness/development tradeoff.
- Injured/suspended players are never selected, IDs are unique, roles/fallbacks and short-squad behavior remain valid, and insufficient credible youth produces a sensible fallback.
- Default mandate reproduces current selection; rival policy and RNG consumption do not change merely from enabling the code path.
- An integrated sequence records changed appearances and later development opportunity, without asserting that every prospect improves or every result differs.

Run the relevant `MatchRealismTests` and development integration checks. Obtain independent review. Hand off policy-sensitive selection behavior and its evidence output; E06 owns durable presentation linkage.

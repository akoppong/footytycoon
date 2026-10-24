# E04 — Make free-agent recommendations respond to the mandate

Depends on: integrated E02. Type: Core behavior. Read [shared instructions](README.md), AGENTS.md and the E00 contract.

## Outcome

The director can recommend readiness or a defensible lower-cost alternative from the existing free-agent pool, with an inspectable reason and real wage implications.

## Work and ownership

- Own `FreeAgents.Recommend` and a focused helper/test file if useful. Implement only the alternative ordering defined in E00; default behavior stays equivalent.
- Preserve eligibility/window/retirement/squad/wage/cooldown rules. The mandate never overrides proposal reserve checks or execution affordability. A recommendation may be blocked by later preview checks; explain that boundary rather than claiming it is guaranteed executable.
- Apply priorities to real candidates and actual quoted wages/contract duration. Use deterministic tie-breaking. When there is no meaningful alternative, return the sensible supported result and say so.
- Return policy/reason data through the E02 contract for E06; previewing/recommending consumes no RNG and spends no money.

## Boundaries

No paid-transfer shortlist redesign, rival policy change, new candidate generation, player acceptance changes, automated approval, or budget framework. Existing owner approval and shared contention remain intact. The paid opening/January recommendations are explicitly outside this initial mandate's effect; the UI must say that.

## Acceptance

Controlled candidate sets demonstrate changed recommendation and actual cost/readiness tradeoff. On the migrated E01B start, verify the newly selected alternative through the real proposal preview; its earlier diagnostic feasibility check is not a substitute. Equal candidates, no candidates, scarce-role cover, outside-window review, cooldown and financial limits retain correct behavior. Default mode matches baseline. Switching mandates must not rewrite a pending approved offer or reopen its chance of acceptance. Verify existing free-agent/minimum-squad/contention tests as applicable; obtain independent review. Hand off ranking and reason behavior to E06, with any no-op scenarios documented.

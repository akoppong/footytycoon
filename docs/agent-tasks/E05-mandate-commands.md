# E05 — Add transactional mandate changes

Depends on: integrated E02. Type: Core/Application commands. Read [shared instructions](README.md), AGENTS.md and the E00 contract.

## Outcome

An owner can preview and confirm a prospective manager/director mandate change, safely save it, and reload without duplicate history or altered pending commitments.

## Work and ownership

- Own a narrow typed mandate-command/preview implementation and its `GameSession` integration. Reuse existing revision/fingerprint, command receipt and persistence concepts; avoid putting unrelated new operations into the investment switch or rewriting the entire command architecture.
- Use the E00 eligibility/timing rules and E02 state/history contract. Show what the mandate changes and which existing commitments remain in force. No hidden costs or football-outcome guarantees.
- Validate stale terms, invalid enum/state, repeated command ID and same-revision/different-world loads. Resolve how receipts share the existing command namespace so IDs cannot be reused across different command families to apply two effects.
- Write a candidate and publish only after durable persistence. Preserve source snapshots and branch-on-write behavior. Record the original mandate and effective date/revision.

## Boundaries

No UI, behavior ranking/selection, hiring, new salaries or capital-plan unlock. A mandate change must not resolve existing unrelated required capital decisions. No new required decision type is necessary for this optional experiment control.

## Acceptance

Preview/query are pure. Exactly one change/history entry occurs for duplicate submissions and retries after loading. Stale/invalid commands fail without mutation. A failed candidate write leaves the prior live state authoritative; retry succeeds once. Pending approved bid terms and existing financial/RNG state are preserved. Load/season-boundary/terminal-status behavior follows E00. Add meaningful application/persistence tests and independent review. Hand off APIs and failure semantics to E06/E07.

# E06 — Connect mandate, execution and outcome

Depends on: E03, E04 and E05 integrated and their combined relevant tests passing. Type: Core/reporting integration. Read [shared instructions](README.md), AGENTS.md and E00.

## Outcome

A player can follow “what I asked for → what the staff did → what changed,” including when a signing fails or football results disappoint.

## Work and ownership

- Own the integration of E02's evidence fields into match/recruitment outcomes and immutable views. Coordinate edits in `Matchday.cs`, `FreeAgents.cs`, `Simulation.cs` and `GameSession.cs`; prior behavior authors must have finished.
- Preserve the originating mandate/revision on decisions that resolve later. Report selection choices and the recommendation's real wage/readiness compromise. Distinguish a recommendation, owner approval and completed signing.
- Record the actual binding reason for tested failed free-agent approaches (unavailable, deadline, squad/financial checks, refusal), with deterministic check order. Do not imply generic prose proves a cause.
- Provide a bounded season review connecting policy periods, actual appearances, signed wages/cash movements and development reports. Distinguish measured facts from possible contributions: one win/loss or one development roll cannot establish a mandate's causal success.

## Boundaries

No generic event bus/event-sourced rewrite, universal causality engine, new supporter/relationship effects or speculative valuation. Do not rewrite pre-migration history or reinterpret old evidence under today's mandate. E02's narrow contract should be reused; any new schema change is a separate coordinated migration.

## Acceptance

Saving/loading retains the origin and outcome of a delayed approval. Changing policy after approval does not relabel the old action. A controlled selection/signing sequence produces inspectable linked evidence. Failure reasons agree with actual checks and money moved. Reports preserve uncertainty and missing historical data. Querying them consumes no RNG. Integration tests cover policy changes around negotiation and season boundaries. Obtain independent review and hand off view fields and wording responsibilities to E07.

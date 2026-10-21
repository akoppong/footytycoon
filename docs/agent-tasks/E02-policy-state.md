# E02 — Persist minimal mandate state

Depends on: E00 and E01B delivered and reviewed (including E01's baseline). Do not start if the playable-start feasibility gate is unmet. Type: Core persistence/read contract. Read [shared instructions](README.md), AGENTS.md and the E00 contract.

## Outcome

The owned club can retain the experiment's manager/director mandates across save/load, without yet changing gameplay behavior.

## Work and ownership

- Start with `Model.cs`, `WorldFactory.cs`, `WorldCodec` and `GameSession.GameView`. Prefer a focused new Core file for new types. Add only state/read contracts agreed in E00: current mandates, prospective mandate history and the minimal evidence shape required downstream.
- Define explicit current-behavior defaults for new and migrated careers. Initialize no fictional historical decisions, staff promises or past development effects.
- Add the next sequential schema migration and invariant checks. Coordinate the version update; update exact-version fixtures without weakening their assertions. Keep older migration coverage.
- Expose immutable state through the application query. Publish the resulting type/API contract for E03/E04/E05, including where evidence may be appended and how revisions identify its origin.

## Boundaries

No manager behavior, recommendation changes, command execution, appointments, new salary obligations, UI or unrelated migration rewrite. Existing fixed leaders remain the presenters. Do not create a general employee database for two mandate values.

## Acceptance

- Current-schema round trip preserves the new values and valid history; invalid enums/references fail validation.
- Pre-change saves import prospectively without rewriting contracts, pending bids, forecasts, people, cash or RNG state; original files remain unchanged.
- Default worlds advance with equivalent gameplay/financial/RNG behavior to baseline after accounting for declared schema/state metadata differences and consistently mapping newly generated forecast/proposal IDs and references whose hashes include encoded world bytes. Previously stored history/IDs remain exact; the comparison must not discard arbitrary identifiers or overlook broken references.
- Querying is pure and returns immutable state; meaningful migration and default-behavior tests pass.

Obtain independent review and provide the exact shared contracts and schema behavior. E03/E04/E05 may start only after this unit is integrated. If E00's evidence contract proves too broad, reduce it with the coordinator before building unused fields.

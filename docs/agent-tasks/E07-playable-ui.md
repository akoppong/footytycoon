# E07 — Expose the experiment in the existing UI

Depends on: E06 integrated. Type: Desktop. Read [shared instructions](README.md), AGENTS.md and E00.

## Outcome

A player can inspect mandates, compare their tradeoffs, confirm a change and understand the subsequent evidence without leaving the existing Owner Desk/People/History structure.

## Work and ownership

- Own the relevant `Main.cs`, `Main.Design.cs`, `Main.Decision.cs` and `Main.Smoke.cs` changes. Use E05 APIs and E06 immutable views; do not calculate gameplay or mutate Core from UI callbacks.
- Add concise manager and director mandate controls to People, a clear route from Owner Desk, and dated changes/outcomes in History. Say explicitly that director priority currently applies to free-agent recommendations; paid transfers and approvals retain their existing rules.
- Preserve preview/final confirmation, time-held behavior, busy/error handling, keyboard focus and recovery. Keep the default brief short and detailed evidence inspectable.
- Display no material change when the staff have no suitable alternative; do not invent activity to make the feature seem useful.

## Boundaries

No navigation rewrite, new app shell, generalized inbox overhaul, staff hiring UI, reputation meters, or automatic spending. Do not make player selection or tactics editable.

## Acceptance

Player can complete change → advance → inspect → save/reload using keyboard. Cancel and failed save leave truthful UI state. Current mandate and history agree after loading a different same-revision checkpoint. Existing acquisition, capital-plan, recruitment and renewal flows still work. Desktop build and applicable headless/graphical smoke checks pass; inspect screenshots at 100%, 125%, 150% without treating screenshots as human usability evidence. Obtain independent review. Hand off a playable build/invocation and known limits for E08/E09.

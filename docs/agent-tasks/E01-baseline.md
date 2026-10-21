# E01 — Capture a reference season

Depends on: none; can run alongside E00. Type: diagnostics. Read [shared instructions](README.md) and AGENTS.md. Expected output: `docs/OWNER_MANDATE_BASELINE.md` plus new isolated raw artifacts.

## Outcome

We can compare the experiment with actual existing behavior and determine whether the starting world offers credible development and recruitment choices.

## Work

- Inspect the Headless runner before invoking it. Record source revision, runtime, seed, owner commands, capital plan, recruitment approvals, renewal choices and terminal week. Start with seed 2026; inspect a small additional fixed set only if needed to measure opportunity availability. Do not choose seeds only because they favor a desired result.
- Run/reuse an appropriate one-season path through existing production rules. Measure closing cash, minimum cash, points, squad age/ability/cover, competitive appearances for players aged 23 or younger at the start of the measured period, and offered/signing wage commitments. Separate proposed, approved and executed actions.
- Record how often there are plausible same-role young selection alternatives and multiple eligible free-agent candidates. Explicitly report zero-opportunity cases. Capture the differences between direct-core simulation and application persistence; time a small application sample rather than presenting core timing as save-inclusive performance.
- Prefer existing diagnostics. If an observer is necessary, add the smallest diagnostic-only reader/harness with a meaningful read-only/no-RNG-effect check. Do not change gameplay or create an extensible analytics platform.

## Boundaries

No tuning, artificial injuries, silent cash injections, population changes, policy implementation, old artifact overwrites or long 50-season campaigns. The current executable may play three seasons by default; report the first-season checkpoint explicitly rather than calling a three-season result a one-season run.

## Acceptance and handoff

The report includes reproducible commands and input provenance, distinguishes measured from missing fields, and lists whether E00's desired comparisons are possible in the unmodified scenario. Baseline replay is consistent for identical commands/seed. Any added harness gets appropriate validation and independent source review; the report gets independent evidence review. E02 receives the baseline semantics, not an assertion that the current game is fun or balanced.

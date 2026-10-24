# E01B — Prepare a valid playable experimental start

Depends on: reviewed E00 contract and E01 baseline. Type: scenario preparation/diagnostics. Read [shared instructions](README.md) and AGENTS.md. Expected output: a starting-checkpoint manifest and control-run addendum in `docs/OWNER_MANDATE_BASELINE.md`, with isolated saves and preparation traces.

## Outcome

The experiment can actually exercise both manager selection and director recruitment in a real playable season. This is an entry gate for E02, not an optional fixture improvement.

## Evidence and work

- Verify the current constraints: `OpeningSquadSize` and `Academy.SquadLimit` are 22, `FreeAgents.CanReview` requires fewer than 22, and opening player contracts expire at season boundaries. Injuries do not create vacancies. Ordinary opening-season play therefore gives the director experiment no free-agent signing opportunity.
- Prepare a disclosed season-two start through existing production commands: play the first season, use ordinary renewal/release choices and academy-intake choices within their checks, then select a documented capital plan if necessary for recommendations to become available. Do not edit roster/cash fields, raise squad caps, cancel live contracts, alter results, or invent prospects. Explain the prelude to testers; do not present it as an untouched new career.
- Follow E00's preregistered suitability criteria and seed order. Record all inspected seeds, rejected starts and reasons. Verify a real vacancy and the current recommendation through its actual production preview. The current API previews only its single recommended free agent; it cannot yet quote an arbitrarily selected alternate. Check alternatives using a pure diagnostic applying the existing eligibility, quoted-wage/contract, wage-limit and `Finance.ProjectCash` reserve guards, with no state/RNG changes or simulated approval. Also verify credible manager selection alternatives. E04/E08 must verify actual alternative previews once the new ranking can select them. Suitability is judged before experimental outcome comparison; do not choose a start because one policy later wins.
- For each experimental seed, save one common pre-treatment checkpoint with a reproducible command trace, version/hash and preparation summary. Record the control policy's measured-season results from that same start. Preparation-season outcomes are excluded from the experiment's season metrics.
- Verify the checkpoint loads through the existing application/desktop recovery path and can complete its measured season and review within the three-season limit. Use a dedicated vault with no user saves overwritten. Later experiments import it through the supported schema migration and fork each policy from the same imported state.

## Boundaries and fallback

No gameplay changes or generic scenario editor. A tiny preparation harness is allowed if existing tooling cannot record the required ordinary commands; it must use production command/validation paths and receive independent review. Controlled test-only world mutation cannot satisfy this task.

If the existing rules cannot produce an eligible start, document the failed criteria and propose one separate bounded scenario-content task. Mark this gate incomplete; E02–E09 must not proceed on the assumption that a later agent will solve the missing opportunity. Natural no-op seeds can remain in the broader diagnostic sample, but the primary player-test start must meet the declared opportunity criteria.

## Acceptance and handoff

An independent reviewer reproduces or verifies the preparation trace, checks saved state and feasible terms, and confirms the same start supports both policy comparisons without already containing their outcome. The manifest links every checkpoint to the current baseline evidence. Deliver the playable start, baseline control, loading instructions and explicit limitations to E02/E08/E09.

# E00 — Define the experiment contract

Depends on: none. Type: documentation/design. Read [shared instructions](README.md) and AGENTS.md. Expected output: `docs/OWNER_MANDATE_EXPERIMENT.md`.

## Outcome

The following agents share one narrow, implementable hypothesis: owner mandates change manager selection and director recommendations, creating traceable choices between readiness, development and financial resilience.

## Work

- Inspect `Matchday.Select`, `FreeAgents.Recommend`, current proposal/commit flow and the assessment. Define two manager modes (current readiness and bounded development) and two director modes (current recommendation and lower-cost value). These are staff mandates, not selectable personal traits or new hires.
- Specify eligibility, ranking/tie-breaking, a limited acceptable ability sacrifice for development, what “lower cost” means (actual wage/term commitment, not a fictional transfer fee), and fallback when no credible alternative exists. Preserve all availability, role, squad-cover and financial checks. Express behavior as examples and invariants; avoid prescribing a general policy framework.
- Define permitted mandate-change times, confirmation semantics, pending-offer treatment, season rollover and legacy defaults. Suggested initial rule: changes apply prospectively to selection/recommendations; already approved bids keep their terms and originating mandate unless they fail existing execution checks. No rerolls or refunds.
- Specify the minimal saved/read contract needed by E02–E07, including mandate history and prospective evidence fields, so parallel agents do not invent incompatible types. Do not preassign schema numbers.
- Register the seed list, comparison policies, actionable-opportunity checks, material-difference definitions, human questions and provisional pass/revise/stop criteria before experimental runs. Define starting-checkpoint eligibility before selection: a genuine squad vacancy, credible selection alternatives, eligible free-agent alternatives with feasible terms, and a remaining full playable season plus its review. E01B verifies the current recommendation through production preview and alternatives through pure diagnostic checks until E04 makes them selectable. E04/E08 then verify their actual previews. Treat thresholds as exploratory judgments, not statistically established targets. Do not inherit the broader assessment's hiring/relationship criteria into this smaller experiment.

## Boundaries

No source edits, new salaries, authority limits, hiring, relationship scores or demand model. Maintain the existing capital-plan rule. Opening squads are full and no ordinary midseason release opens a slot: use a disclosed later-season checkpoint prepared through ordinary renewals/releases in E01B. Controlled unit-test fixtures alone cannot establish a playable human-test scenario. If a valid checkpoint cannot meet the entry criteria, feature work waits for a separately scoped scenario solution; do not secretly change the opening world.

## Acceptance and handoff

A reviewer can trace each mode through one ordinary and one boundary example, identify the actual player tradeoff, and assign E02–E07 without unresolved behavior/interface contradictions. The contract states exactly what remains unproven, including staff relationships and autonomous spending. Obtain independent documentation review. Deliver the contract and a compact interface/decision table; mark choices as explicit experimental defaults, not user research findings.

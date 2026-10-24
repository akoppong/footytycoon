# E00: owner mandate experiment contract

Status: design contract; no mandate behavior implemented. Registered 23 October 2026 against gameplay source `57e640dd411ab064210287478cd614e3949e025a`, schema 23, with the planning pack published in `442f3d63161f718d4b2ef936529c7d53c912b758`. This delivers [E00 / issue #34](https://github.com/akoppong/footytycoon/issues/34) under [tracker #33](https://github.com/akoppong/footytycoon/issues/33), the first step of the [ownership-loop task pack](agent-tasks/README.md).

Hypothesis: asking the existing manager to favor credible young players, and asking the existing director to favor a defensible lower-cost free agent, creates observable decisions and understandable readiness/development/cash tradeoffs worth trying again. These are mandates for the existing named leaders, not hires, personality traits or autonomous spending authority.

All numeric bounds and gates below are explicit experimental defaults, registered before experimental checkpoint selection or policy comparisons. E01 can independently measure the existing baseline; those measurements are not treatment evidence. These defaults are exploratory judgments, not user-research findings or statistical acceptance thresholds. Changing a bound requires a dated amendment, independent review and a new comparison; retain the original results.

## Scope and source boundaries

Keep the existing club, squads, league/cup, formations, signed commitments, academy generation, paid-transfer rules, annual capital-plan rule and three-season playable limit. The manager still selects every lineup. Owners still explicitly approve every signing. Rivals retain their current policies. Shared player competition and downstream football/financial effects remain real.

Source anchors at the baseline are [selection and availability](../src/FootballTycoon.Core/Matchday.cs), [free-agent recommendations and execution](../src/FootballTycoon.Core/FreeAgents.cs), [proposal and commitment checks](../src/FootballTycoon.Core/Finance.cs), [state and codec](../src/FootballTycoon.Core/Model.cs), and [application transactions](../src/FootballTycoon.Application/GameSession.cs). Check callers and tests when implementing; these links are not permission to replace unrelated systems.

Do not add staff salaries, hiring/dismissal, relationships, supporter demand, development bonuses, reserve exemptions, new candidates, artificial injuries, loans, generic agents or a storage rewrite. Proving these mandates would not prove the full staff-relationship experience. E00 changes documentation only.

### Compact decision table

The numbers in these examples are illustrative inputs, not observations from a playable checkpoint. Every example assumes the unchanged eligibility checks pass unless a boundary is specified.

| Mandate / interface | Ordinary example | Boundary and fallback | Authority |
|---|---|---|---|
| Manager `Readiness` → ordered lineup | A 62-ability midfielder starts ahead of a 59-ability youngster. | If the 62-ability player is unavailable, choose the best available native-role player, then existing nearest-role fallback; play short if needed. | Automatic selection; owner never chooses players. |
| Manager `Development` → lineup + swap evidence | Replace that older native-role starter with the available age-21 youngster, sacrificing three ability points. | A six-point gap, unavailable youngster or no qualifying pair leaves the readiness lineup unchanged. | At most two same-role outfield swaps, each costing at most five ability points. |
| Director `CurrentRecommendation` → terms or none | Among eligible same-role players, recommend ability 65 at £1,000/week over ability 61 at £900/week. | Full squad/pending offer means no recommendation; a subsequent reserve failure blocks approval without selecting a different candidate. | Owner previews and approves a free-agent approach. |
| Director `LowerCostValue` → terms + reason or none | With the same end date, recommend the ability-61 player at £900/week. | A cheaper weekly wage with a higher total commitment does not qualify; retain the current recommendation if no alternative qualifies. | Same approval and execution checks; no spending authority. |
| `ChangeMandates` → preview, receipt, history | Confirm a new pair; the next computed decision uses it. | Cancel, stale quote or failed save has no live effect; an identical pair is a no-op rejection. | Optional owner command; existing capital-plan decision remains required. |

## Manager behavior

Two modes: `Readiness` (default) and `Development`. Existing `Matchday.Select(club, week)` retains its exact readiness meaning; the owned-club match path may select a new policy-aware pure function. Rival and legacy/default calls must continue to produce the existing lineup, ordering and random-stream behavior.

**Readiness:** use the current availability-aware 1–4–4–2 selection unchanged: ability descending, person ID ascending, native-role selection followed by existing nearest-role fallback. No new random draws.

**Development:** first produce that exact readiness lineup. Consider only pairs consisting of (a) a selected native-role outfield player older than 23 and (b) an available, originally unselected player aged 23 or younger with that same natural role. The younger player's ability must be between the selected player's ability minus five and the selected player's ability, inclusive. Goalkeepers and players assigned outside their natural role are never swapped by this mandate.

Build the pairs once from the readiness lineup and original bench. Order by ability sacrifice ascending, younger-player ability descending, younger-player age ascending, role enum order, younger-player ID ascending, then displaced-player ID ascending. Take at most two pairs, skipping any pair whose selected player or replacement was already used. Replace those players in their original slots; preserve the normal final position ordering. Do not recompute pairs, introduce replacement chains, or reroll when no pair remains. This changes at most two starters and sacrifices at most ten summed native-role ability points; it does not promise any match-result change.

| Case | Required behavior / actual tradeoff |
|---|---|
| Native-role midfielder, age 29/ability 62, starts ahead of an available age-21/ability-59 midfielder | Development can select the younger player, accepting three ability points of readiness cost for a real appearance opportunity. Existing development rules later use actual appearances; no immediate ability bonus. |
| Only alternative is age 22/ability 56 against ability 62, or is injured/suspended | No swap: the six-point gap or unavailability fails eligibility. Preserve the readiness pick. |
| Three independent credible pairs | Apply the fixed ordering and cap at two; the owner cannot select the pair or formation. |
| Readiness already starts the young players; only goalkeeper or out-of-role alternatives exist; squad plays short | No invented opportunity or altered fallback. Report zero qualifying swaps when appropriate. |

Selection uses current age/ability at the fixture. Evaluation separately freezes a start-of-period young cohort (age ≤23 at the experimental checkpoint), so an annual birthday cannot remove a player's earlier appearances from measured development exposure. Store the decision-time ages and ability values needed to explain a swap after later development or departure.

## Director behavior

Two modes: `CurrentRecommendation` (default) and `LowerCostValue`. Scope is **owner free-agent recommendations only**. Opening paid recruitment, January paid shortlists, renewals, academy intake and rival policies are unaffected; the UI must state this.

Start with the exact current recommendation `b`. Retain `CanReview`, availability in the free-agent pool, retirement age, eligible role/window or emergency cover, ability-upgrade/working-mix rules, annual wage limit and four-week candidate cooldown. Current ranking remains missing-role cover first, ability descending, age ascending and person ID ascending. Do not add a reserve filter to this default ranking: today its subsequent cash/reserve preview can block approval, including arrears in projected cash. Execution separately requires no club arrears.

Concretely, `CanReview` requires Active status, a chosen annual capital plan, week earlier than season-end minus one, no paid negotiation or free-agent bid, and fewer than 22 squad members. Each candidate must be below the role's retirement age, pass `EligibleRole`, and either satisfy `NeedsCover` or exceed the weakest contracted player in that role. `NeedsCover` retains the current minimum-role checks and, below 16 players, working-role targets (2 goalkeepers, 6 defenders, 6 midfielders, 4 forwards). Total annual wages including the candidate must remain within the existing 75% eligible-revenue limit. An approved approach to that person within four weeks excludes them even if it failed. Pool availability is distinct from match fitness: do not invent a signing injury/suspension filter.

If there is no current recommendation, both modes return none. `LowerCostValue` considers candidates passing the same filters, **in b's natural role**, with ability at least `b.Ability - 5`. Their quoted weekly wage and total remaining signed wage commitment must each be no higher than b's, with at least one strictly lower. This preserves the baseline role priority rather than using a cheap player in an unrelated role to ignore a shortage. Order qualifying alternatives by total commitment ascending, weekly wage ascending, ability descending, age ascending, then person ID ascending. If none qualifies, retain b and explain that no qualifying lower-cost alternative exists.

Use the actual current quote rules, not age-independent proxy costs:

- Wage in integer pence: `max(10000, ((max(previousWeeklyWage, divisionStandardWage / 2) + 500) / 1000) * 1000)` with the current integer arithmetic.
- Contract end: current season end plus 104 weeks for age ≤24, 52 weeks for age 25–29, or zero for age ≥30.
- Approval at week `w` resolves next week; successful wages start at `w + 2`. The quoted remaining commitment is `weeklyWage * (contractEndWeek - w - 1)`, using checked integer arithmetic, exactly as production preview. Fee is zero. Dates, remaining duration and total commitment must be displayed alongside weekly wage.

| Case | Required behavior / actual tradeoff |
|---|---|
| Week 52 in season two; same-role age-30 candidates, abilities 65/61, weekly wages £1,000/£900, end week 104 | Totals are £51,000/£45,900. Value can recommend the second player: four ability points less, 10% less weekly and total commitment. Owner approval and later acceptance remain required. |
| A younger £900/week candidate runs to week 208 against the same £1,000/week candidate ending at 104 | £139,500 exceeds £51,000. Lower weekly pay alone does not qualify that candidate as lower cost. |
| Cheaper alternative is six ability points below b, has the wrong role, fails wage/window/cooldown eligibility, or costs exactly the same weekly and overall | Retain b unless another candidate qualifies. Equal costs alone are not a value alternative. |
| The selected candidate passes ranking but the normal cash/reserve preview (including arrears in projected cash) blocks approval | Show the actual blocking reason and do not spend. No silent alternative search, reroll, reserve exception or guarantee of signing. |

Value mode may nominate an affordable alternative when the current recommendation's subsequent reserve preview is blocked; measure and disclose this instead of changing default behavior. At execution retain all existing availability, deadline, status, squad, role, arrears, wage and reserve checks and the existing acceptance draw. No payment occurs on approval. Actual signed commitment uses the real signing/start dates; do not count proposed or approved totals as paid wages.

## Changes, timing and authority

The owner reviews and confirms a typed `ChangeMandates` command containing both target mode values. Only the changed values create one joint history entry. A request identical to the current pair is blocked as a no-op; it does not increment revisions or create history.

Changes are optional and permitted while in `Active`, including before the annual capital plan, or in a nonterminal `SeasonReview`. They are unavailable during acquisition, administration, lost control or the final completed endpoint. A pending paid/free-agent offer does not prevent a mandate change. The normal serialized application worker prevents mid-operation changes. Changing policy does not resolve any required capital/renewal decision, advance the week, set `AllocationChosen`, alter cash, consume RNG or cancel a pending offer.

On successful confirmation the modes apply to the next selection/recommendation computed after commit. There is no retroactive recalculation. Already approved bids retain exact terms, original policy evidence and their original acceptance stream; ordinary execution checks still apply. Current mandates persist across rollover without automatic reset. Existing terminal careers remain terminal.

Preview includes old/new modes, what will change, pending commitments that stay in force, no immediate charge, and no guaranteed results. It is pure and bound to world identity plus expected revision, not merely the mandate revision. Reject same-revision/different-world stale previews. Reuse the existing durable candidate-publication rule and recovery/fork semantics.

Cancellation creates no receipt or history. A successful command increments world revision once and mandate revision once; changing both modes is one transaction. Failed validation/persistence leaves the live state authoritative and unchanged. No rerolls, refunds, expiry extensions, cooldown resets or new negotiation attempts arise from changing a mandate. Loading and continuing a saved branch retains the existing branch/replay rules.

Use a single persisted command-ID namespace across investment and mandate command families. Once an ID has a receipt, any retry returns that existing receipt without a second effect, consistent with current investment idempotency. The UI generates a fresh ID for a distinct action. Test cross-family ID reuse explicitly; do not create parallel receipt maps that can apply two commands under one ID.

## Minimal shared state and read contract

E02 owns the exact C# types, validation, migration and immutable application projection. The following semantic fields and relationships are required; agree exact symbols in that reviewed change before E03/E04/E05 proceed. Do not reserve a schema number here.

| Contract | Required data and invariants | Writer / reader |
|---|---|---|
| Current mandate state | Manager mode, director mode, monotonic mandate revision starting at 0. Defaults are Readiness/CurrentRecommendation. | E02 initializes; E05 changes; E03/E04 read. |
| Change command / preview / receipt | Command ID, both target modes, expected world revision; preview includes old/new pair and world fingerprint; receipt identifies command, preview and resulting world revision in the shared receipt namespace. Invalid mode values fail validation. | E02 reserves the shared state/read shape; E05 implements application preview/commit; E07 confirms. |
| Mandate change history | Command ID; week; resulting world revision; old/new pair; old/new mandate revision. Positive revisions are contiguous and each transition agrees with the previous state. No fabricated change entry for migration/defaults. | E05 appends; E06/E07 read. |
| Policy origin | Both modes and mandate revision captured at decision time. Revision 0 means explicit current-behavior defaults, not unknown historical policy. A positive origin must resolve to the matching history transition. | Pure selection/recommendation returns it; E06 persists at real execution/approval. |
| Match selection evidence | Owned fixture ID; origin; chosen swap pairs with IDs, roles, decision-time age/ability and readiness ability cost. Zero swaps with a Development origin is a measured no-op; actual lineup remains the authoritative appearance record. | E03 returns pure result; E06 appends once at match resolution; E07 renders. |
| Recommendation evidence | Origin; decision week; baseline/selected person IDs, role, age, ability, weekly wage, expected wage start week, end week, total commitment; fixed reason enum (`Current`, `LowerCostAlternative`, `NoLowerCostAlternative`). No recommendation is an empty pure result, not a saved fictitious choice. | E04 returns pure result; E06 attaches only when owner approval is recorded. |
| Approach outcome evidence | Original forecast/approval link and origin; selected person; resolved week; signed/closed result and binding reason. Preserve approved quotes separately from actual signed dates/commitment. | E06 records once on actual resolution; E07 renders. |
| Immutable query | Current pair/revision, read-only change history, selection and approach evidence through existing match/decision links. Missing historical evidence is null/absent, never inferred from current settings. | E02 defines shapes; E06 projects; E07 consumes. |

For pre-feature saves: add defaults and empty evidence/change history prospectively. Existing approvals without origin keep **unknown/legacy** origin through resolution, rather than being labeled revision 0. Do not regenerate original forecasts, IDs, receipts, lineups or financial records. Empty outcome evidence is valid until E06 is integrated. Evidence retention is limited to actual decisions/matches; opening a screen or a quote creates no history. Validate references to retained people/fixtures/approvals, not only currently contracted people, so departures do not invalidate historical records.

Keep the new mandate history separate from existing investment decision history: a mandate change must not change `History.Count`-derived bid IDs or acceptance stream names. Each selection has at most one evidence record per owned fixture; each approval has at most one recommendation and one eventual outcome record, linked by its existing decision/forecast identity. Mode pairs must agree with their origin revision, including revision 0 defaults. History world revisions increase but need not be contiguous; mandate revisions are contiguous. Retained evidence is immutable and duplicate command/advance retries cannot append it twice. Annual summaries reuse existing appearance, development, obligation and ledger records rather than a second financial ledger. Paid recruitment stays outside the new recommendation-evidence contract; preserve its existing approved terms and history unchanged.

Outcome failure classification mirrors existing short-circuit order: player unavailable → deadline → inactive career → full squad → retirement age → role/window cover → wage limit → arrears → reserve → player refusal; otherwise signed. Capture only the first failing check before any acceptance draw. A failed deterministic gate consumes no acceptance draw; refusal is recorded only when all gates passed and the actual draw declined. Current code computes the cash projection before its conjunction; preserve any relevant validation/error behavior rather than treating this reason list as permission to rewrite financial calculations. Rival paths are unchanged.

The manager/director modes do not affect annual contract recommendations. Season reports connect actual policy periods, appearances, development changes, approvals/signings and cash payments, but cannot attribute every win or development roll to a mandate. Retain distinct proposed, approved, signed and paid measures.

## Registered starting-state gate

E01 records baseline behavior without tuning, starting with 2026. E01B prepares and verifies starts before E02 feature work, inspecting all five seeds in fixed order **2026, 2027, 2028, 2029, 2030**; retain every seed and its failed criteria. Do not expand, reorder or stop this list based on outcomes. If resource limits prevent completion, report the remaining inspections as missing and keep the gate incomplete. No policy treatment runs occur before this contract and the starting-state manifest pass review.

For each inspected seed, use ordinary commands to acquire at asking price and choose PreserveReserve for season one; use the current diagnostic CoverShortages approval policy, no injections or optional midseason paid purchase, and choose the existing midseason wait when a required decision needs resolution. At the first renewal, start from recommended contracts. E01B may use ordinary release overrides and decline the optional academy intake to leave a slot, but must disclose a deterministic preparation recipe, all preview failures and final commands **before** comparing treatment outcomes. Freeze that recipe for subsequent seeds; changing it is a dated protocol amendment, not a silent seed search. No direct roster/cash edits or cancelled live contracts.

The common pre-treatment checkpoint is season two, week 52, after ordinary renewal and PreserveReserve confirmation, before advancing its first week. Require all of:

1. Active control, no arrears or pending negotiation/bid, no unresolved required decision preventing advance, and 16–21 players meeting each existing role minimum. At least one genuine squad slot remains.
2. An available age-≤23 outfield bench player and older native-role readiness starter form at least one eligible Development swap under the exact algorithm above. There is no promised full-season persistence of this opportunity.
3. `CanReview` is true. The current recommendation's actual production preview has no blocking reasons with no reserve exception.
4. The alternative that would rank first under the exact Value ordering satisfies its ability and dual-cost constraints, differs in identity, and lowers weekly or total commitment by at least 10% (at least one ability point lower too, so there is a visible initial readiness tradeoff). Merely finding a qualifying candidate that would not be selected is insufficient. Until E04 exists, E01B enumerates eligible alternatives and checks the selected alternative's wage/term, annual wage, arrears and reserve feasibility with a pure diagnostic using existing rules and `Finance.ProjectCash`. Do not claim an arbitrary alternate had a production preview. E04/E08 must subsequently preview the actually selected alternate.
5. The checkpoint loads through a dedicated application vault and desktop recovery flow; a control season can finish its review within the three-season boundary. Preserve source bytes/hash and command trace. Exclude preparation-season outcomes from experiment metrics.

Retain all feasible starts. The first eligible seed in the fixed order becomes the primary player-study start, chosen before treatment results. Record natural no-op starts separately; do not use them to claim the experiment is actionable. If none qualifies, E01B is incomplete and E02–E09 wait for a separately reviewed scenario solution. Do not alter the opening world to force success.

## Fixed comparisons and measurements

From each eligible common checkpoint run a 2×2 comparison: Readiness/CurrentRecommendation (control), Development/CurrentRecommendation, Readiness/LowerCostValue, Development/LowerCostValue. Import each checkpoint through the same reviewed schema, then fork copies before policy commands. Run the full remaining season-two calendar through week 104 and its closing review, without renewing into season three. Freeze policies after the initial treatment; separate unit/integration tests cover later switching.

In all four arms, attempt the first eligible ordinary free-agent recommendation immediately at the start, approve it only if its normal preview is unblocked, and record proposed/approved/executed outcomes separately. After that initial approach, evaluate and approve ordinary unblocked recommendations only when below current role or total-squad cover, respecting pending offers/cooldowns. Choose midseason wait; no paid transfers, cash injections, further facility investment or manual selection. If required checks stop a run, preserve it as partial rather than repairing cash or completing it by direct mutation.

For reproducibility, evaluate that approval rule once before each weekly advance (after required non-spending decisions); do not repeatedly poll/approve at the same week. Use `Contracts.Shortages` for the later cover condition, as the existing diagnostic does. All experimental approvals set `ReserveException = false`. A blocked initial quote is recorded as blocked; it does not trigger a search through other candidates or a retry outside the later cover rule.

Measure closing and lowest weekly club cash, final points/position and cup progression, actual wage payments, current annual wages, signed remaining commitments, arrears/shortage weeks, squad age/ability/cover, young-cohort competitive starts, manager swaps and their actual ability costs, and ordinary annual development. Record state each scheduled match/week as appropriate; do not substitute final squad counts for season exposure or count an approval as a signing. Missing data must be named.

The frozen young cohort consists of owned-squad person IDs aged ≤23 at the checkpoint; count only their starts for the owned club in league/cup matches during weeks 53–104. Report later young recruits separately. Count arrears and shortage weeks at each completed week, separately; shortages use `Contracts.Shortages`. Lowest cash includes the checkpoint and every completed week. Remaining commitment is the positive sum of unpaid future signed wage obligations after the observation week, with arrears reported separately. Proposed savings compare the two quotes at the same decision week, using integer-pence amounts; a 10% reduction means `alternative * 10 <= baseline * 9` using overflow-safe arithmetic. Actual cash/wage differences are reported in pence, points/starts as counts; no extra sporting or cash pass cutoff is inferred from a nonzero difference.

Report raw observations and within-checkpoint pair differences. Manager contrasts hold director mode fixed; director contrasts hold manager mode fixed; the fourth arm reveals interactions. Selection/RNG and financial safety checks must pass. Stored pre-treatment identities/references remain exact; schema-derived new forecast/proposal IDs need a documented consistent semantic mapping, not blanket exclusion of arbitrary IDs. A same-seed deterministic world does not imply identical later acceptance/match streams after actions diverge.

Register the following exploratory technical gates:

- All completed arms preserve identity, cash reconciliation, authority, replay, save/load, quote purity and failed-publication guarantees. Any invariant failure blocks product testing until a separately reviewed repair and affected rerun.
- In at least one eligible start, Development creates at least **six additional young-cohort competitive starts over the measured season**, with the two-swap/five-point per-swap limits always met. Report both manager contrasts and every no-op start, not only the favorable contrast. Changed starts are opportunity evidence, not guaranteed ability gain.
- In at least one eligible start, Value's initial real preview selects a distinct candidate and lowers weekly or total commitment by **at least 10%**, with neither cost increasing and no more than five ability points sacrificed. Report actual acceptance and all later obligations separately. If no changed candidate signs across the registered sample, mark the executed financial comparison **inconclusive**, even when ranking/approval works; no forced acceptance or extra seed fishing.
- Compare each pair of the four policy arms across all eligible seeds. If the same arm is no worse on points, closing cash and young-cohort starts (higher is better), and on both arrears and shortage weeks (lower is better), and strictly better on at least one measure at every seed, flag that pair as a **dominance warning**, not proof of global optimality. This requires complete arms for every eligible seed. Revise the tradeoff/scenario before deeper systems. Missing and partial arms cannot support a dominance or success claim.

E08 reports pass/revise/inconclusive against these unchanged gates and publishes the build/provenance, all commands, measured fields, failures and artifact paths. Core-only timing is separate from query/preview/advance including durable writes. No fifty-season campaign or statistical generalization is needed for this bounded experiment.

## Player questions and decision gate

E09 prepares a neutral protocol for **five adult volunteers** interested in football management or business strategy, with at least two who have not used this prototype. No recruitment/contact is authorized by this document. Obtain actual participants and appropriate consent before sessions; missing participants mean **Awaiting player evidence**, never synthetic observations. Disclose the ordinary season-one prelude and existing limitations without coaching the desired policy.

Ask each participant to diagnose the starting club, choose mandates, explain what they give up, play the measured season, inspect an unexpected event, and decide whether to try another approach. Neutral debrief questions:

1. What did you ask the manager and director to do? Which decisions still required you?
2. Show one selection and one recruitment example. What changed, what cost did it carry, and which part was uncertain?
3. What did the rejected/failed approach or unexpected match result tell you? What does it not establish?
4. What would you change next time, and why? Offer the replay option without asking for a favorable rating.

Provisional product gate: at least **four of five** explain both mandates and retained owner signing approval without coaching; at least **three of five** trace two real examples through request → action → observed consequence, distinguishing a recommendation from a signing and uncertainty from certainty; at least **three of five** voluntarily start a different policy replay when offered, rather than only saying they might. Count unavailable evidence or missing sessions explicitly, not as successes. If nobody experiences a real recruitment outcome, that question and the product gate remain incomplete; inspect the actual opportunity rather than staging success.

Proceed to a recommendation for deeper staff work only if technical gates, credible tradeoffs and these observed product criteria pass. If behavior changes but is unclear, revise evidence/UI; if no meaningful choices or one sampled policy dominates, revise the bounded policy/scenario; if technical guarantees fail, repair first. Record stop/revise/await/proceed and the smallest next task. Passing supports another experiment, not proven retention, release readiness or automatic execution of the backlog.

Explicit stop default: if a technically valid, actionable five-person study yields **zero voluntary different-policy replay starts**, recommend stopping this mandate approach rather than adding deeper staff systems. One or two replay starts, or other failed comprehension/tracing thresholds, recommend a bounded revision. Missing sessions or missing recruitment evidence mean await evidence, not a stop/pass result. Safety failure or an infeasible checkpoint stops progression until its separate repair/scenario gate passes; neither establishes product rejection. Staff attachment, hiring/relationships, autonomous spending, long-term retention, economic balance beyond these seeds and release readiness remain unproven even after a pass.

## Handoff and acceptance

E00 delivers this behavior/interface contract only. E01 still owes reproducible reference measurements and application timing; E01B still owes the reviewed playable checkpoint, preparation recipe and control run. E02 must not start without both. E02 then freezes types/schema and default equivalence; E03/E04 implement isolated behavior; E05 implements transactions; E06 joins durable evidence; E07 exposes it in existing People/Desk/History screens; E08/E09 provide technical/player evidence. None is completed by this contract.

Working branch: `codex/owner-mandate-contract`, based on planning commit `442f3d6` with unchanged gameplay source `57e640d`. The PR base is `codex/owner-mandate-task-plan`; the assessment, task pack and assessment evidence belong to that prerequisite change. No gameplay/tests/build/export were changed or rerun for this documentation-only unit; source inspection and independent documentation review are the applicable checks.

Acceptance checks on 23 October 2026: verified both full prerequisite revisions are ancestors of the task branch and match the live heads of [code PR #31](https://github.com/akoppong/footytycoon/pull/31) and [planning PR #32](https://github.com/akoppong/footytycoon/pull/32). All six repository-relative Markdown links resolve. The three quoted commitment examples independently calculate to £51,000, £45,900 and £139,500. Source/tests/CI remain unchanged. No baseline run, feasible checkpoint, treatment run, player session or downstream implementation is claimed here. Integration of this reviewed document, or its linked durable PR artifact for documentation handoff, completes E00 only; E01B's reviewed feasibility evidence is still required before E02.

Independent non-author documentation review checked all four modes against production code, the E02–E07 interfaces, save/financial/replay guarantees, and the E01B/E08/E09 protocol. No material findings were identified. The review's wording clarification distinguishes the cash/reserve preview's treatment of arrears from execution's explicit no-arrears guard; that clarification is applied above. Experimental feasibility and product risks remain open as stated, not unresolved implementation claims.

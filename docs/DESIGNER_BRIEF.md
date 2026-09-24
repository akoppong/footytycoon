# Football Tycoon — UI/UX product brief

Designer handoff · 16 September 2026 · Working title

## The product

Football Tycoon is a premium, offline, single-player Windows PC game about owning a football club. Buy a club, appoint its leaders, decide where scarce money goes, and live with the sporting and financial consequences. Build an institution worth keeping—or accept a buyer's offer and sell.

The player fantasy: **“I bought a club with potential, chose the people and investments that mattered, survived the consequences, and built something worth owning.”**

The player controls budgets, leadership, strategy, major commitments, and the decision to sell. Staff handle lineups, tactics, training, and routine recruitment. The UI must make ownership itself engaging through understandable choices, anticipation, accountability, and attachment to the club.

## Who we are designing for

Primary players are football fans who enjoy tycoon games and club rebuilding, including management-game players who prefer building an institution to coaching matches. Secondary players enjoy business simulations and sports franchise modes.

Assume football interest, but no accounting qualification. Explain financial concepts when they affect a decision. Target sessions of 30–60 minutes, with satisfying stopping points at major decisions and monthly reviews. An experienced player's season should take roughly 45–75 minutes. These are design targets to validate.

## Design assignment and scope

Design the full game's information architecture and core ownership journey, then prioritize a polished, clickable opening-loop prototype. The immediate design problem is helping players see what matters, compare competing uses of money, commit confidently, and understand the result.

The release vision includes one club per career; six acquisition scenarios; a fictional country with three 16-club divisions and a domestic cup; three hireable leadership roles; delegated recruitment; stadium, training, academy, and hospitality investment; loans and finite owner capital; supporter pressure; valuation and actual sale offers. A ten-season assessment allows continuation, with a final career limit of 50 seasons.

**Current implementation is much smaller:** one acquisition, one opening choice between cash retention, hospitality, and recruitment, fixed leaders, financial forecasts, match reports, local saves, and a 52-week endpoint. Hiring, loans, promotion/relegation, academy development, multi-season progression, valuation, and sale offers remain future work. Design those as V1 target flows, not as already functioning features.

Out of scope: tactical controls, playable matches, 3D football, multiplayer, multi-club ownership, real club/player licenses, and mobile-first layouts.

## Core experience

**Understand the club → choose a priority → compare proposals → commit money and delegate → advance time → observe results → review evidence → hold, correct, reinvest, or sell.**

Each important choice should answer:

- What problem are we solving, and which executive recommends this?
- What does it cost now and over time?
- What else could this money fund or protect?
- When could benefits arrive, and what remains uncertain?
- What happens next, and when will we review it?

The emotional rhythm is conviction, commitment, anxious anticipation, then pride or regret and renewed judgment. Named people, the ground, supporter concerns, and remembered decisions should make the numbers personal.

## Priority journeys

1. **New career and acquisition.** Compare club identity, price, inherited obligations, ambitions, club cash, and personal money retained. Confirm the purchase with a concise terms summary.
2. **First allocation.** Receive a short CEO brief, compare two credible investments with retaining cash, inspect downside and ongoing obligations, then confirm a plan. Aim for the first meaningful allocation within ten minutes.
3. **Delegation and progress.** Understand each executive's authority, approve a mandate or material exception, then continue to the next meaningful event. Show pending execution without implying success is guaranteed.
4. **Outcome and review.** Present the match or business development, compare actual results with the original forecast, explain supported causes and uncertainty, and offer relevant next actions.
5. **Save and return.** End with what changed, what is decided, and what awaits. On return, restore the player's context in under one minute.
6. **V1 expansion journeys.** Hiring and dismissal, recruitment exceptions, project investment, financial distress and recovery, season planning, and an actual buyer offer leading to an exit report.

## Information architecture

Use the PRD's six primary workspaces. Existing prototype tab names are provisional and do not prescribe the final navigation.

| Workspace | Main purpose |
|---|---|
| Owner Desk | Prioritized inbox, club brief, headline metrics, pending decisions, next review, and Continue |
| Finances & Value | Cash history and forecast, costs, debt, owner capital, valuation, distributions, and sale review |
| Leadership & Strategy | CEO, sporting director, manager, hiring, mandates, budgets, and accountability |
| Football Operations | Roster and needs, contract risks, recruitment proposals, academy, and results |
| Club Development | Stadium, training, academy, hospitality, demand, supporters, identity, pricing, and sponsors |
| League & World | Tables, fixtures, cup, rivals, significant transfers, and club history |

Use contextual dossiers and reports for deeper detail. The inbox belongs to Owner Desk. Supporting screens include acquisition, settings, save/load and recovery, match reports, and career endings. Keep decision history accessible from relevant reviews and commitments.

Owner Desk has six headline measures: club cash; lowest forecast cash and its date; spending headroom; league position versus ambition; estimated equity-value range; supporter approval. Personal reserve and detailed breakdowns remain easy to inspect. Target designs may include future measures, but prototype implementation must not present unsupported values as live data.

## Critical interaction rules

- One or two issues should usually dominate the page, while all material deadlines remain accessible. Players should find the next action and main risk within 30 seconds.
- Continue advances to the next material decision. It must stop for required decisions and critical deadlines. In the opening inbox, Continue opens the pending allocation; while that proposal is selected, disable advancing and explain why.
- Deadlines use simulation time only. Quiet periods can be skipped, and match presentation is skippable without changing outcomes.
- Plans are editable before commitment. Purchases, sales, loans, dismissals, and projects need a final concise terms summary. Clearly distinguish approval, rejection, revising, and retaining cash.
- Every monetary amount needs its basis: cash now, weekly upkeep, annual wage, or total commitment. Separate personal money, club cash, forecasts, and estimated equity value.
- Cash charts need labeled base/downside paths, reserve threshold, minimum and date, plus exact values and a text explanation. Compare identical periods; distinguish known obligations from uncertain receipts.
- Reviews show original intent, commitment, responsible person, actual versus original forecast, uncertainty, and next actions. Never claim a rejected investment would certainly have produced a better result.
- Keep unresolved decisions visible and dismissed information retrievable. Recognize prudent restraint as well as spending.
- Surface up to three genuine open questions across near, seasonal, and multi-season horizons. Leave space empty when nothing warrants attention.

## Visual direction

Build on the existing refined owner-inbox studies: a familiar desktop management-game interface with a compact toolbar, persistent navigation, selectable inbox rows, and a contextual reading pane. Use navy/ivory contrast, strong typography, restrained financial colors, and club-color accents.

Give the club a recognizable crest, ground, and identity. Use selective executive/player portraits and facility imagery to support decisions. Establish hierarchy through layout and typography; dense tables need aligned columns and clear labels. Major milestones deserve distinctive, skippable treatment.

The current prototype relies heavily on prose panels. Improve scanning, make the latest relevant information prominent, and visually connect commitments with their outcomes. The supplied visual studies are references for exploration, not final interaction specifications or canonical artwork.

## First prototype scenario

Use Stonebridge FC to make the design concrete:

| Item | Example from the current prototype |
|---|---|
| Acquisition price | £2.8m |
| Personal reserve after purchase | £2.2m |
| Opening club cash | £1.4m |
| Club reserve target | £300k |
| Hospitality option | £650k upfront; 28-week build; £1k weekly upkeep after opening |
| Immediate club cash after hospitality payment | £750k, before subsequent costs and receipts |
| Immediate headroom above reserve target | £450k; not a forecast of safely spendable cash |
| Competing choices | Retain cash, or authorize a £550k forward search with a separately disclosed wage ceiling |

Recruitment authorization does not guarantee a signing or immediate fee payment. Use actual simulation forecasts for financial charts. Any invented future outcome or V1 screen data must be labeled illustrative in the handoff.

Design both the routine inbox and selected proposal, then the confirmation, active commitment, first review, and resumed-career states. Show that the player can compare alternatives and retain cash.

## Accessibility and state coverage

Target mouse and keyboard at 1280×720 and higher, with 100%, 125%, and 150% text settings. Allow reflow or scrolling without clipping essential terms or controls. Provide visible keyboard focus, logical focus order, remappable essential shortcuts, non-color status cues, reduced motion, and independent music/effects controls. Guidance can expand or collapse without removing critical warnings.

Specify normal, selected, focused, disabled-with-reason, empty, pending, completed, rejected/expired, insufficient-funds, and critical-risk states. Include saving, save failure, recovery, and return from a dialog. Larger text must preserve usable navigation and financial comparisons.

## Requested designer deliverables

Recommended handoff package:

- Information architecture and annotated flows for the priority journeys, separating prototype scope from V1 extensions.
- Wireframes for all six workspaces and required supporting screens.
- High-fidelity opening-loop screens and a clickable prototype from acquisition through first review and resume.
- A reusable component library: navigation, inbox, decision comparisons, financial summaries/charts, tables, dossiers, confirmations, warnings, and history entries, including interaction states.
- Typography, color, spacing, iconography, club-identity and imagery guidance; responsive/text-scale examples and implementation annotations.
- A usability-test script and a short list of unresolved design decisions. Delivery format, schedule, and asset-production scope should be agreed with the designer.

Test whether players can find the main risk, explain club versus personal cash, compare ongoing obligations, retain cash without feeling they failed, understand delegated authority, and explain an outcome without treating forecasts as promises. Test both novice and experienced players. These are acceptance goals; no successful user testing is claimed.

## Reference pack

- [Full product requirements](PRODUCT_REQUIREMENTS.md): canonical release scope, especially C–G, H, I, and M. This brief summarizes the design assignment; it does not replace the PRD.
- [Implementation status](IMPLEMENTATION_STATUS.md): what is implemented and what remains.
- [Refined inbox direction](../artifacts/design-refinement-2026-09-15/DIRECTION.md).
- [Routine inbox visual study](../artifacts/design-refinement-2026-09-15/01-routine-inbox.png).
- [Investment decision visual study](../artifacts/design-refinement-2026-09-15/02-investment-decision.png).
- [Existing interface review](../artifacts/design-review-2026-09-15/DESIGN-REVIEW.md): problems and supporting prototype screenshots.

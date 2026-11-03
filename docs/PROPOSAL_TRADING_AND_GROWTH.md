# Proposal: trading and growth vertical slice

Status: **proposal for discussion**, not a decision. Written 3 November 2026 as a follow-up to [UX_AUDIT.md](UX_AUDIT.md). Nothing here is implemented. The owner's local build differs from `main` (see the version caveat in the audit), so check this against that build before acting on it.

## 1. Why this exists

The audit found the game is bland because the player has about four decisions a season, sees no growth, and can't do the thing the owner enjoys most: buying and selling players for profit and reinvesting the proceeds into a bigger club. A UI re-skin would not fix that.

The owner's stated preferences:

- The business side is the appeal; tactics are not.
- Favourite part of football management games: transfers, and making money from buying and selling players.
- Plays city builders: wants visible growth and systems that react.
- Wants to set strategy, hire people to execute it, and be able to fire them (the delegation model of the business sim Capitalism Lab: you set policy and hire managers per department).
- A small club should have a more hands-on owner; delegation should depend on club size and resources.

Much of this matches the existing PRD: hireable leaders, mandates, a "player trading" mandate, a three-candidate shortlist with salary and dismissal cost, and explicit dismissal compensation ([PRODUCT_REQUIREMENTS.md](PRODUCT_REQUIREMENTS.md) lines 243 to 248, 375 and 459 to 472). **It is not fully consistent with the PRD**, though. The hands-on early game, owner scouting and agent costs reverse or exceed explicit PRD rules; see section 3. The current build simply stops before the part that makes the delegated version a game.

## 2. What `main` has today (grounding)

| Area | Current state in `main` |
|---|---|
| Player market | One forward search in the opening plan and one January window with two options or "keep squad" (`RecruitmentMarket.cs`). Targets are chosen by a fixed rule from a rival with more than two forwards; the fee is a flat `TransferFeeCeiling` from balance data, not a market price. |
| Player value | `Player` has ability, age, wage and contract end. There is no market value, potential or scouting uncertainty. |
| Selling | No owner-initiated sales. Players leave only by release at contract end. `IMPLEMENTATION_STATUS.md` lists protected-player sales, a free-agent market and rival bidding as open. |
| Leadership | Three fixed named leaders. Hiring, firing, competence and mandate negotiation are open. |
| Facilities | Hospitality and training, three levels each, one project at a time. No stadium or academy investment. |
| Growth feedback | Cash chart and a league position. No visible club growth. |

So the main gap is in the simulation, not the screens.

## 3. Where this departs from the PRD and brief (needs an explicit decision)

These are not details. Each conflicts with a written rule, so adopting the proposal means amending the PRD deliberately, not by drift.

| Proposal element | Conflicting source |
|---|---|
| Owner scouts, shortlists and bids personally at a small club (5.2) | PRD 468 to 469: the major-deal threshold "is not a slider that can be reduced to zero" and V1 has no manual approval of every routine signing. PRD 11 and 221: staff handle routine recruitment. `DESIGNER_BRIEF.md` lists delegated recruitment as a release-vision pillar. |
| Owner-directed scouting to narrow value estimates (5.1) | PRD non-goal at line 1070 excludes owner control of scouting assignments. |
| Agent costs and fee structure (5.1) | PRD non-goals at lines 1070 to 1075 exclude agent negotiation systems and transfer instalments. |
| Never showing a competence number (5.3) | PRD 463: show broad ratings and evidence. Resolve by showing broad ratings plus a track record, or amend. |
| Home screen led by the ground and gauges (5.4) | PRD 413: cash, forecast low and headroom are always visible. The ground and gauges should sit alongside those, not replace them. |
| Supporter cost when selling a favourite (5.1) | PRD 470 already requires owner approval to sell a top-three contributor or major supporter favourite. Reuse that rule rather than inventing a second one. |
| A weekly attention budget (5.2) | Not in the PRD; a new mechanic. |
| Escalation (5.3) | Use the PRD's escalation and materiality rules (PRD 470 to 472), not a parallel model. |

There is also a **positioning risk**. PRD 111 says business features alone do not distinguish the game, and PRD 117 says that if players mainly ask for more transfer and tactical control, the core loop has failed. The owner's appeal is transfer trading, so this proposal bets directly against that warning. It may be the right bet, but it should be made knowingly and tested early.

## 4. Design principles

1. **The owner earns the right to delegate.** Early game is hands-on; growth unlocks hires.
2. **Delegation must stay interesting.** Executives are distinct and fallible, so the owner keeps judging them.
3. **Profit funds visible growth.** Trading is the engine; the club getting bigger is the reward.
4. **Every market rule needs friction.** If buying low and selling high is always profitable it is a grind, not a game.
5. **Prove one department first.** Recruitment, one growth step, one season loop, before extending the pattern.

## 5. The vertical slice

**Recommended minimum first cut:** the policy-only version. A real market (5.1) and a sporting director who trades inside a mandate (5.3), plus the stadium step (5.4). Hands-on bidding (5.2) comes second, and only if the PRD amendment in section 3 is accepted. This is still large, so treat 5.1 to 5.4 as an ordered list, not a single drop.

### 5.1 A real player market

- Every player gets an **estimated value** (a price clubs would currently pay) and a hidden **true value** driven by age, ability, potential and recent form.
- The owner sees *estimates with a confidence band*, narrowed by scouting spend and time (see the scouting non-goal in section 3). The hidden true value must stay consistent with the existing `Ability` model, the development rules in [PLAYER_DEVELOPMENT.md](PLAYER_DEVELOPMENT.md) and rival squads; the spec in step 1 of section 8 has to cover that. The gap between estimate and true value is where profit and loss come from.
- **Friction:** transfer fees paid up front, wages while a player is held, agent costs, injuries, an asking-price premium on players a rival values, and rival bids on the same target.
- **Selling:** the owner can list, accept or refuse offers. Selling a long-serving fan favourite carries a supporter-approval cost, so it is a real choice.
- **Visible per-player ledger:** bought for, wages paid, current estimate, sell-on profit or loss. Club-level transfer profit becomes a headline measure.

### 5.2 Hands-on at small scale

- At the start there is no sporting director. The owner scouts, shortlists and bids personally, spending a small **attention budget** per week (for example two or three actions, to be tuned). Without a limit, hands-on trading becomes tedious.
- The attention budget is the reason the first hire feels like relief, and it keeps hands-on play a choice.

### 5.3 The first hire: sporting director

- Unlocks when affordable (revenue and reserve thresholds, to be tuned), and is offered as a **shortlist of three** with distinct traits (for example: youth-obsessed and overpays, cautious and misses bargains, savvy but asks for a raise and can be poached). The PRD already describes a three-candidate shortlist with salary and dismissal cost.
- The owner sets a **trading mandate** with enough dials to matter: age range, fee ceiling, wage ceiling, profit target, development versus quick resale, and risk appetite.
- The director executes inside the mandate and escalates exceptions. Reports and disagreements become the inbox's content.
- **Firing has a real cost:** compensation, a vacancy gap, and lost shortlist and relationships. The replacement shortlist is shown before the firing is committed.
- Results are judged by outcomes. Proposed: show broad ratings plus an accumulating track record, not a precise competence score (PRD 463 asks for broad ratings and evidence).

### 5.4 One visible growth step

- Trading profit funds a **stadium expansion** (capacity, with a build time and a cost), which changes a simple home-ground image and raises attendance potential. Hospitality and training already exist and should feed the same view.
- Add two or three **pressure gauges** in the City-Skylines spirit: ticket demand versus capacity, wage bill versus revenue, and possibly squad depth versus fixtures (this one has no metric yet; define it or drop it). **Capacity does not exist in the core today:** attendance is demand times a ticket divisor with no cap (`Simulation.cs`, around lines 152 to 160). Adding capacity means a new model and re-balancing match receipts, not just a stadium step. The PRD already specifies stadium capacity upgrades, which this should follow. A red gauge tells the owner what to build or sell.
- Proposed: the home screen leads with the ground and the gauges, alongside the cash, forecast low and headroom figures the PRD keeps always visible.

### 5.5 Out of scope for the slice

Other executives (CEO, manager hiring), loans, valuation and sale offers, the full six-scenario acquisition set, rival-club AI beyond bids, and any tactical controls.

## 6. Premortem: assume it ships and it is still not fun

| Likely cause | Mitigation to design in |
|---|---|
| A trading loop that always prints money (the community finds an exploit) | Uncertainty, fees, wages, rival bids, supporter cost; a balance sweep with an "optimal trader" bot before building UI (this does not replace the PRD's own gate of 100 varied 50-season runs). |
| Hiring makes the game passive again | Distinct executive traits, escalations, policy dials, and a poaching or departure risk; review whether the owner still has something to decide each week. |
| Hands-on play is tedious at small scale | Attention budget; batch actions; a quick "ask my advisor" shortcut that costs attention. |
| Growth is cosmetic | Capacity must change attendance and revenue, and gauges must react within weeks, not seasons. |
| Scope or non-goals drift: the slice quietly adds scouting, agents and instalments that the PRD excludes | Section 3 decision before any code; keep the first cut policy-only. |
| Scope balloons into the full PRD | Ship the slice (recruitment plus one growth step plus one season loop) and playtest before extending. |

## 7. Six-hats sketch

- **White (facts):** the sim lacks a player market and selling; the PRD already specifies hiring and mandates.
- **Red (feeling):** the owner is bored now; trading and visible growth are what excite them.
- **Black (risks):** exploitable trading, passive delegation, tedium, scope.
- **Yellow (benefits):** gives the game verbs, feedback, a growth arc, and an inbox that fills itself.
- **Green (alternatives):** a simple policy-only version (set rules, watch staff trade) as a cheaper first cut; event cards layered on later.
- **Blue (process):** write the rules on paper, balance-test with a bot, then build screens.

## 8. Proposed sequence

0. Decide the section 3 amendments (delegation, scouting, agents, always-visible metrics).
1. Write the market rules (value model, uncertainty, friction) as a short spec and review it. No code yet.
2. Add the market and selling to `FootballTycoon.Core` with tests, plus a minimal per-player ledger view so the bot sweep is inspectable, then run the sweep.
3. Add a stadium capacity model, re-balance receipts, and add the gauges.
4. Add hiring and firing of the sporting director, with mandate dials.
5. Only then redesign the UI around the market, per-player ledger and growing ground.

## 9. Open questions for the owner

- Is a simple policy-only first cut (set rules, watch the staff trade) acceptable, or must the owner personally negotiate each deal in the first version?
- How large should the attention budget be, and should it grow with the club or stay fixed?
- Should supporter reaction to selling a favourite be a hard cost or a soft signal?
- Are we amending the PRD's delegation and non-goal rules for the opening game (section 3), or keeping it delegated and finding another source of hands-on trading?
- Which build is the baseline for this work: `main`, or the owner's local build with the seller counteroffer?
- Is there an art budget for the growing ground, or should the first version use stylised placeholder shapes?

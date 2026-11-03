# Proposal: trading and growth vertical slice

Status: **proposal for discussion**, not a decision. Written 3 November 2026 as a follow-up to [UX_AUDIT.md](UX_AUDIT.md). Nothing here is implemented. The owner's local build differs from `main` (see the version caveat in the audit), so check this against that build before acting on it.

## 1. Why this exists

The audit found the game is bland because the player has about four decisions a season, sees no growth, and can't do the thing the owner enjoys most: buying and selling players for profit and reinvesting the proceeds into a bigger club. A UI re-skin would not fix that.

The owner's stated preferences:

- The business side is the appeal; tactics are not.
- Favourite part of football management games: transfers, and making money from buying and selling players.
- Plays city builders: wants visible growth and systems that react.
- Wants to set strategy, hire people to execute it, and be able to fire them (a Capitalism Lab style of delegation).
- A small club should have a more hands-on owner; delegation should depend on club size and resources.

This is consistent with the existing PRD (hireable leaders, mandates, a "player trading" mandate, explicit dismissal compensation, [PRODUCT_REQUIREMENTS.md](PRODUCT_REQUIREMENTS.md) around lines 219 to 248 and 459 to 472). It is not a pivot. The current build simply stops before the part that makes it a game.

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

## 3. Design principles

1. **The owner earns the right to delegate.** Early game is hands-on; growth unlocks hires.
2. **Delegation must stay interesting.** Executives are distinct and fallible, so the owner keeps judging them.
3. **Profit funds visible growth.** Trading is the engine; the club getting bigger is the reward.
4. **Every market rule needs friction.** If buying low and selling high is always profitable it is a grind, not a game.
5. **Prove one department first.** Recruitment, one growth step, one season loop, before extending the pattern.

## 4. The vertical slice

### 4.1 A real player market

- Every player gets an **estimated value** (a price clubs would currently pay) and a hidden **true value** driven by age, ability, potential and recent form.
- The owner sees *estimates with a confidence band*, narrowed by scouting spend and time. The gap between estimate and true value is where profit and loss come from.
- **Friction:** transfer fees paid up front, wages while a player is held, agent costs, injuries, an asking-price premium on players a rival values, and rival bids on the same target.
- **Selling:** the owner can list, accept or refuse offers. Selling a long-serving fan favourite carries a supporter-approval cost, so it is a real choice.
- **Visible per-player ledger:** bought for, wages paid, current estimate, sell-on profit or loss. Club-level transfer profit becomes a headline measure.

### 4.2 Hands-on at small scale

- At the start there is no sporting director. The owner scouts, shortlists and bids personally, spending a small **attention budget** per week (for example two or three actions, to be tuned). Without a limit, hands-on trading becomes tedious.
- The attention budget is the reason the first hire feels like relief, and it keeps hands-on play a choice.

### 4.3 The first hire: sporting director

- Unlocks when affordable (revenue and reserve thresholds, to be tuned), and is offered as a **shortlist of three** with distinct traits (for example: youth-obsessed and overpays, cautious and misses bargains, savvy but asks for a raise and can be poached). The PRD already describes a three-candidate shortlist with salary and dismissal cost.
- The owner sets a **trading mandate** with enough dials to matter: age range, fee ceiling, wage ceiling, profit target, development versus quick resale, and risk appetite.
- The director executes inside the mandate and escalates exceptions. Reports and disagreements become the inbox's content.
- **Firing has a real cost:** compensation, a vacancy gap, and lost shortlist and relationships. The replacement shortlist is shown before the firing is committed.
- Results are judged by outcomes. The owner is never shown a competence number up front, only a track record that accumulates.

### 4.4 One visible growth step

- Trading profit funds a **stadium expansion** (capacity, with a build time and a cost), which changes a simple home-ground image and raises attendance potential. Hospitality and training already exist and should feed the same view.
- Add two or three **pressure gauges** in the City-Skylines spirit: ticket demand versus capacity, wage bill versus revenue, squad depth versus fixtures. A red gauge tells the owner what to build or sell.
- The home screen shows the ground and the gauges, not the generic cash chart.

### 4.5 Out of scope for the slice

Other executives (CEO, manager hiring), loans, valuation and sale offers, the full six-scenario acquisition set, rival-club AI beyond bids, and any tactical controls.

## 5. Premortem: assume it ships and it is still not fun

| Likely cause | Mitigation to design in |
|---|---|
| A trading loop that always prints money (the community finds an exploit) | Uncertainty, fees, wages, rival bids, supporter cost; a 100-run balance sweep with an "optimal trader" bot before building UI. |
| Hiring makes the game passive again | Distinct executive traits, escalations, policy dials, and a poaching or departure risk; review whether the owner still has something to decide each week. |
| Hands-on play is tedious at small scale | Attention budget; batch actions; a quick "ask my advisor" shortcut that costs attention. |
| Growth is cosmetic | Capacity must change attendance and revenue, and gauges must react within weeks, not seasons. |
| Scope balloons into the full PRD | Ship the slice (recruitment plus one growth step plus one season loop) and playtest before extending. |

## 6. Six-hats sketch

- **White (facts):** the sim lacks a player market and selling; the PRD already specifies hiring and mandates.
- **Red (feeling):** the owner is bored now; trading and visible growth are what excite them.
- **Black (risks):** exploitable trading, passive delegation, tedium, scope.
- **Yellow (benefits):** gives the game verbs, feedback, a growth arc, and an inbox that fills itself.
- **Green (alternatives):** a simple policy-only version (set rules, watch staff trade) as a cheaper first cut; event cards layered on later.
- **Blue (process):** write the rules on paper, balance-test with a bot, then build screens.

## 7. Proposed sequence

1. Write the market rules (value model, uncertainty, friction) as a short spec and review it. No code yet.
2. Add the market and selling to `FootballTycoon.Core` with tests, then run a bot-trader balance sweep.
3. Add the stadium step and gauges to the core.
4. Add hiring and firing of the sporting director, with mandate dials.
5. Only then redesign the UI around the market, per-player ledger and growing ground.

## 8. Open questions for the owner

- Is a simple policy-only first cut (set rules, watch the staff trade) acceptable, or must the owner personally negotiate each deal in the first version?
- How large should the attention budget be, and should it grow with the club or stay fixed?
- Should supporter reaction to selling a favourite be a hard cost or a soft signal?
- Which build is the baseline for this work: `main`, or the owner's local build with the seller counteroffer?
- Is there an art budget for the growing ground, or should the first version use stylised placeholder shapes?

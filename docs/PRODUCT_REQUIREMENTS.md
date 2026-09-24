# Football Club Ownership Tycoon — Product Requirements Document

**Version:** 0.2 — consolidated product and player-experience handoff  
**Date:** 15 September 2026  
**Working title:** Football Tycoon; naming is not yet validated.  
**Immediate goal:** A complete, paid, single-player PC game on Steam.  
**Status:** Product baseline for architectural planning; player validation, production estimates, and funding remain unverified.  
**Boundary:** This single document defines player experience, rules, content, scope, and acceptance criteria. The receiving team should use it to prepare architectural design and the subsequent TRD; it does not prescribe that architecture.

All game prices, percentages, pacing targets, and balance ranges below are **design assumptions to test**, not claims about real football economics. External platform facts and competitor descriptions are linked where used; reviewed on the date above. “Must” denotes a release requirement. “Should” and “Could” are optional within the remaining budget.

### How to use this handoff

- This revision incorporates the ownership PRD, motivation/flow design, and challenge progression into one source of product requirements. No companion document is needed.
- The eight player-experience requirements in D are part of the V1 baseline and appear in the priority matrix and release gates. They refine existing systems rather than add new departments, competitions, or simulation modes.
- Fictional examples, illustrative session lengths, and scientific design analogies explain intent; they are not guaranteed outcomes or additional content quotas.
- Use the stated defaults for architectural planning. Record the open assumptions in O, propose how to validate high-risk ones, and flag the impact of any suggested product change. No playtest, market-validation, or production-feasibility result is claimed by this document.
- Read A, F, and N first for scope; D, G, H, and I for required behavior; K and M for priorities and acceptance; O and P for the architectural handoff.

### Contents

| Product and scope | Systems and experience | Delivery and handoff |
|---|---|---|
| [A. Executive Summary](#a-executive-summary) | [G. Detailed Systems Requirements](#g-detailed-systems-requirements) | [L. Risks and Mitigations](#l-risks-and-mitigations) |
| [B. Product Vision](#b-product-vision) | [H. UX / Screen Requirements](#h-ux--screen-requirements) | [M. Success Metrics](#m-success-metrics) |
| [C. Target Player](#c-target-player) | [I. Game Progression](#i-game-progression) | [N. Explicit Non-Goals](#n-explicit-non-goals) |
| [D. Core Gameplay Loop](#d-core-gameplay-loop) | [J. Steam Launch Requirements](#j-steam-launch-requirements) | [O. Open Questions and Handoff Assumptions](#o-open-questions-and-handoff-assumptions) |
| [E. Design Principles](#e-design-principles) | [K. Feature Priority Matrix](#k-feature-priority-matrix) | [P. Architectural Design Handoff](#p-architectural-design-handoff) |
| [F. Version 1 Scope](#f-version-1-scope) | | |

## A. Executive Summary

### Start with the strongest loop

**Choose an ownership thesis → diagnose the club's limiting problem → commit scarce capital and delegate execution → experience uncertain results → explain what changed → hold, correct, reinvest, or sell.**

Acquisition opens this loop. A credible exit closes it. The repeatable pleasure sits between them: choosing which problem deserves money, trusting someone to solve it, and discovering whether the choice worked.

The proposed acquire–invest–simulate loop is a sound framework but is not sufficient by itself. It risks becoming a sequence of budget sliders followed by waiting. Three additions make it playable:

1. **A binding constraint.** At least two worthwhile uses of money compete with a necessary cash reserve. The player cannot fund everything.
2. **A commitment with a review horizon.** Hiring, contracts, projects, and strategy take time. Reversing them has an understandable cost.
3. **An accountable result.** Every review distinguishes decisions, executive execution, and football uncertainty. Players can learn without being promised victories.

Example: a promotion contender can finance a reliable striker, expand sold-out hospitality, or preserve enough cash to survive failure. The striker could accelerate promotion; hospitality could support future wages; cash could protect the entire investment. A named executive recommends one route. The owner commits, sets a review date, watches the season, and then receives evidence of what happened.

### The recommended commercial product

Build **one club per save**, bought at the beginning and optionally sold to end the career. Include a fictional three-tier football pyramid, three hireable leadership roles, meaningful cash constraints, a delegated transfer market, four investment categories, supporter pressure, and a transparent club valuation with actual buyer offers.

**The commercial hook:** turn a financially vulnerable football club into a more valuable institution while deciding how much sporting ambition it can afford.

### Decisions that protect delivery

| Product choice | Recommendation |
|---|---|
| Player authority | Sole controlling owner; leadership runs football operations |
| World | One fictional country; three divisions of 16 clubs; one domestic cup |
| Ownership | One acquisition at setup; no additional acquisitions in the same save |
| Financing | Club term loans, limited refinancing, finite owner injections; no outside equity investors |
| Matchday | Skippable score timeline, short text moments, and useful summaries |
| Commercial model | Premium purchase; full release after playtests and demo |
| Price hypothesis | US$19.99 base price; evaluate US$19.99–24.99 before announcement |
| Platform | Windows PC, mouse and keyboard, offline single-player, Steam Cloud |
| Core content | Six curated acquisition scenarios, 12 event families, 48 authored event templates |
| Expected experience | First meaningful allocation within 10 minutes; an experienced player's season in 45–75 minutes |
| Explicit cuts | Multi-club groups, multiplayer, tactical controls, real licenses, 3D matches, Workshop |

### Planning assumptions

- Three full-time core contributors: product/system design and production, engineering, and UI/visual design, with overlapping skills; contract QA, editing, sound, and localization as funded.
- A **15–18 month planning envelope**, roughly 45–54 core person-months plus contractors, is a hypothesis to revisit after the prototype. It is not a delivery promise. A solo or part-time team should reduce scope further or extend the schedule.
- A stylized interface and restrained club imagery carry the presentation; there is no explorable stadium or animated football production pipeline.
- The world models one senior professional men's pyramid for V1. Women's competitions and other countries require their own balanced content and are later opportunities.
- Fictional clubs retain stable identities between saves; people and outcomes can vary by seed. Money uses a single fictional-world pound currency.
- The default career has a ten-season assessment, optional continuation, and a declared 50-season limit. The design does not promise endless simulation.

## B. Product Vision

### Player fantasy and emotional experience

“I bought a club with potential, chose the people and investments that mattered, survived the consequences, and built something worth owning.”

The emotional sequence is **conviction → commitment → anxious anticipation → pride or regret → renewed judgment**. A graduate scoring the promotion goal should feel different when the player funded the academy four years earlier. A sale offer should test attachment as well as financial ambition.

The recurring reasons to return are **anticipation** (“I want to see whether my plan works”), **mastery** (“I understand this club better”), and **attachment** (“I care about the people and institution I built”). A completed session should also provide **satisfaction**: the player accomplished something and can stop comfortably while looking forward to the next chapter.

Football gives capital allocation an unusually visible consequence: the money is exposed every weekend, supporters remember decisions, and promotion changes the business overnight. Ownership gives sporting success a second interpretation: winning can create value, but reckless winning can also threaten survival.

### One-sentence pitch

**Buy a football club, hire the people who run it, and turn difficult investment decisions into promotion, profit, and a legacy worth keeping—or selling.**

### Steam store short description

> Own the club. Build its future. Buy a football club, hire its leaders, and choose where every pound goes. Balance promotion dreams against debt, develop talent and facilities, win supporters, and decide when to reinvest or sell in this football ownership tycoon.

Store copy should explicitly state elsewhere that the player does not select teams or control matches. Marketing must demonstrate an owner decision and its later consequence in the first trailer sequence.

### Why it should exist

The opportunity is to make a club's operating model, financing, leadership, and sale value into a coherent strategy game. Removing tactical work creates time for consequential ownership choices, but those choices must be satisfying in their own right.

### Competitive positioning

The following positioning is a product inference from the linked feature descriptions, not proof of market demand.

| Reference | Established experience | Our proposed distinction and obligation |
|---|---|---|
| Football Manager | Deep football management, recruitment, and tactical expression; FM26 explicitly connects its formation and role systems to training and transfers. [Official feature overview](https://www.footballmanager.com/fm26/features/possession-out-possession-fm26s-new-tactical-evolution) | The owner controls resources and leadership. Financial runway, investment alternatives, and exits must be substantially more central to the experience. |
| WE ARE FOOTBALL 2024 | Broad club management, finance and sponsors, training and tactical controls, a 3D match presentation, and editors. [Steam product page](https://store.steampowered.com/app/1951410/WE_ARE_FOOTBALL_2024/) | Compete through decision clarity and focused ownership consequences. Business features alone do not distinguish this game. |
| Football Chairman / Pro 2 | Already covers hiring managers, stadium and facility development, transfers, fans, and progression; some editions include takeovers and scenarios. [Official product site](https://www.football-chairman.com/) | This is the closest conceptual substitute. The PC premium must earn its price through executive mandates, inspectable cash forecasts, distinct investment strategies, and credible sale outcomes. |
| General tycoon and business games; GearCity as an example | GearCity emphasizes detailed business decisions, investment, and economic simulation. [Steam product page](https://store.steampowered.com/app/285110/GearCity/) | Deliver understandable opportunity costs and long-term growth, with football producing named people, rivalries, and dramatic season boundaries. Avoid requiring hours of accounting study. |

**White-space hypothesis:** an accessible PC ownership simulation where delegated football operations, liquidity risk, and realizable club value form one understandable loop. This is narrower and more defensible than “the first football owner game.”

Before production, test whether players value that distinction over existing chairman games. If they mainly request more transfers and tactical control, the pitch or core loop has failed to establish the intended audience.

### Motivation and flow: evidence and design interpretation

Steven Kotler's *The Art of Impossible* motivated the exploration of attention, motivation, connection, and flow. In his own public explanations, he discusses dopamine, norepinephrine, serotonin, endorphins, anandamide, and oxytocin. His framework supplies design questions, not a validated recipe for engineering specific chemical responses to game features. [Kotler on flow neurochemistry](https://bigthink.com/videos/the-neurochemistry-of-flow-states-with-steven-kotler/), [Kotler on purpose and flow](https://fredpinto.com/purpose-and-flow-steven-kotler/)

| Reference in Kotler's framework | Experience to investigate | Application in this game |
|---|---|---|
| Dopamine and seeking/motivation | Curiosity about a worthwhile possibility | A developing investment or an executive's pending recommendation |
| Norepinephrine and focused engagement | Attention to understandable stakes | A genuine transfer deadline or promotion run-in, with time to think |
| Oxytocin and social connection | Attachment, trust, and responsibility | Named people and a club history that remembers ownership decisions |
| Serotonin in his reward/social-bonding account | Satisfaction with a completed chapter | A review that recognizes progress and offers closure |
| Endorphins/anandamide in his flow account | Absorption, emotional payoff, and recovery | Readable interaction, selective celebrations, and quieter periods |

These are **design analogies, not one-to-one biological claims**. Experimental evidence of dopamine release during a particular video-game task does not show that our transfer offer, academy reveal, or celebration will produce a predictable chemical response or durable enjoyment. Neurotransmitter levels are not inferred product metrics. [Koepp and colleagues, 1998](https://pubmed.ncbi.nlm.nih.gov/9607763/)

More directly applicable gaming research associates perceived autonomy and competence with enjoyment; a multiplayer study also links relatedness to enjoyment and intended future play. Applying connection to fictional staff and clubs in this single-player game is a hypothesis to test. [Ryan, Rigby, and Przybylski, 2006](https://selfdeterminationtheory.org/wp-content/uploads/2020/10/2006_RyanRigbyPrzybylski_MandE.pdf)

Flow experiments have compared tasks matched to participants' skill with under- and over-demanding tasks. That motivates testing appropriate challenge, clear feedback, and control over attention. It does not establish a universal success rate or a fixed percentage by which our game's challenge should exceed a player's skill. [Ulrich, Keller, and Grön, 2016](https://pmc.ncbi.nlm.nih.gov/articles/PMC4769635/)

## C. Target Player

| Audience | Priority | Desired experience | Product implication |
|---|---|---|---|
| Football fans who already enjoy tycoon or club-building games | Primary | Building an institution, promotion stories, sensible financial decisions | Football language, recognizable stakes, clear money explanations |
| Football Manager players who prefer rebuilding clubs to coaching | Primary recruitment pool | Recruitment policy, facilities, developing club stature with less routine work | Strong delegation and brisk seasons; never promise FM's simulation depth |
| General tycoon/management players with some football interest | Secondary | Allocation, growth, risk, understandable rival behavior | Explain promotion, transfer fees, and why wages persist after revenue falls |
| Sports franchise-mode players | Secondary | Team identity, hiring, off-season strategy, a continuing dynasty | Accessible roster summaries and strong season recaps |
| Finance/business simulation enthusiasts | Secondary | Debt discipline, capital efficiency, valuation, exit choices | Meaningful financing, but no requirement for financial qualifications |
| Football fans without management-game interest | Opportunistic | Familiar sport and club attachment | Attractive onboarding helps; do not assume fandom implies appetite for financial gameplay |

Prioritize the **football-and-tycoon overlap** for V1. Designing first for finance specialists creates complexity; designing first for tactical enthusiasts creates the wrong product.

Typical intended session: 30–60 minutes, with useful stopping points after a major decision or monthly review. A player must be able to resume after a week away and understand current risks in under one minute.

## D. Core Gameplay Loop

### Minute to minute: assess, compare, commit

1. Open the owner brief: current bottleneck, next obligation, next important date.
2. Read one executive recommendation with two or three alternatives, including retaining cash when plausible.
3. Compare upfront cash, continuing cost, likely benefit range, time to benefit, and downside.
4. Approve, reject, or request one revised proposal with a specific changed constraint.
5. See the cash forecast and mandate update immediately.
6. Continue to the next relevant date; inspect what the organization did.

Routine actions must not require repetitive approval. Target 0–2 owner decisions per normal week and 3–5 in busy transfer weeks. An uninterrupted “continue to next decision” option should make uneventful weeks nearly invisible.

### Weekly: observe without coaching

- Review results, material roster changes, attendance, and cash warnings.
- Deal only with recommendations outside agreed executive authority.
- Watch or skip the match summary with identical outcomes.
- Decide whether emerging evidence warrants intervention or patience.
- Regular management reports use a monthly cadence; a match loss alone is not an inbox crisis.

### Seasonally: make the business plan

- **Preseason:** review acquisition thesis; set sporting ambition, wage ceiling, transfer envelope, minimum cash reserve, and executive mandates. Approve one major investment priority.
- **First half:** observe execution and market response; honor contractual commitments; allow projects to progress.
- **Midseason:** reassess evidence and liquidity; allocate a limited correction budget during the second transfer window.
- **Run-in:** face promotion, relegation, and cash consequences without changing tactics.
- **Year-end:** reconcile operating cash, investments, owner capital, sporting results, and valuation; review staff, distributions, new projects, and possible sale.

### Across seasons: change the problem

Stabilization should lead to promotion pressure; promotion to higher costs; success to retention demands and stadium limits; maturity to succession and diminishing returns. The main decision moves with the club's circumstances.

| Time played | Intended source of interest | Evidence needed |
|---|---|---|
| 1 hour | First budget tradeoff and early consequences; attachment to one executive or player | Player can identify what they funded and what they gave up |
| 10 hours | A promotion, failed push, or turnaround; facilities and contracts create consequences across seasons | Multiple seasons require different allocation choices |
| 50 hours | Several ownership strategies or a long dynasty; rebuilding an aging squad, executive replacement, sale timing | At least three viable strategies and distinct acquisition scenarios |
| 100 hours | Enthusiast replay through difficult starts, seeds, and self-imposed goals | Aspirational retention, primarily across saves; not a V1 content promise or release gate |

### Example of a complete decision arc

The owner has £1.2m genuinely available after protecting forecast obligations. A striker package costs £0.7m now plus wages; a hospitality project costs £0.9m and opens next year. Funding both is impossible without approved financing. The manager wants the striker, the CEO prefers hospitality, and the sporting director explains the downside of waiting.

The player funds hospitality and retains cash. The team narrowly misses promotion. The review shows that attacking quality underperformed expectations, but does not claim the rejected striker would certainly have won promotion. Next season's completed hospitality capacity produces measurable receipts. The player can conclude that the choice was financially useful and emotionally painful.

### Required player-experience loop

**Notice potential → choose a direction → commit something scarce → anticipate → see a consequence → understand the contribution → feel progress or learn → choose the next chapter.**

The following requirements make motivation, enjoyment, and appropriate challenge part of the product baseline. IDs support traceability in the architectural design and TRD; their detailed behavior is specified in the sections shown.

| ID | V1 requirement | Detailed behavior / validation |
|---|---|---|
| EXP-01 | Link important outcomes to recorded ownership decisions, commitments, original forecasts, and named people, without claiming guaranteed causation | G11, H5, M1, M4 |
| EXP-02 | Surface up to three genuine open questions across near, seasonal, and multi-season horizons; never fabricate work to fill the display | H5, M4 |
| EXP-03 | Reviews explain intent, commitment, outcome versus forecast, evidence/uncertainty, and available next actions; recognize prudent restraint as well as investment | H5, G6, M1, M4 |
| EXP-04 | Make selected staff, players, facilities, and club identity memorable through persistent history and relevant callbacks | G10–G11, H4–H5, M1 |
| EXP-05 | Provide skippable presentation, meaningful session closure, and a brief that restores context on return; deadlines advance only with simulation time | H3, H5, J5, M4 |
| EXP-06 | Grow challenge by recombining familiar ownership decisions as club circumstances change, with opportunities to consolidate | G12, H1, I4, M4 |
| EXP-07 | Let players expand or reduce guidance during a career while preserving exact obligations, critical warnings, and the same world rules | G12, H3, M4 |
| EXP-08 | Let players choose the intensity of their ambition at existing strategy reviews, using real budgets and forecasts rather than bonuses or hidden difficulty changes | G12, I4, M4 |

All eight fit within the existing workspaces, histories, reports, scenarios, and mandates. They do not require behavioral profiling, a new relationship simulation, an adaptive match engine, or additional event quotas.

## E. Design Principles

1. **Own the mandate; delegate the method.** Owners select people, objectives, budgets, and major exceptions. Staff select formations, lineups, training, and routine transactions.
2. **Spend against a real alternative.** Every major recommendation identifies what the same cash could protect or fund. Reject consequence-free upgrades and obviously superior choices.
3. **Commit before certainty arrives.** Contracts, capital projects, and development need time. Information improves through observation; waiting also carries opportunity cost.
4. **Make causes inspectable and forecasts honest.** Show reasons, ranges, and confidence. Separate known obligations from uncertain revenue. Never explain a noisy result with invented certainty.
5. **Let success change the constraints.** Promotion raises revenue and expectations; growth requires staff retention, working cash, and fresh investment. Difficulty comes from the business, not hidden penalties.
6. **Attach numbers to people and places.** A named graduate, a long-serving manager, a full stand, and an angry supporter group give financial decisions meaning.
7. **Respect the player's attention.** Delegate routine work, group reports, skip quiet time, and preserve the consequences of meaningful choices. Decision count is not a depth metric.
8. **Stretch understanding, then let competence pay off.** Teach recognizable ownership problems, recombine them as circumstances change, and let players choose ambition and assistance. Solved problems can remain solved; success does not trigger manufactured adversity.

## F. Version 1 Scope

### Smallest Steam-worthy commercial package

The purchase must deliver a full acquisition-to-exit career, replayable starting situations, reliable long saves, a legible economy, and a finished interface. A collection of disconnected systems or a one-season prototype does not meet this definition.

| Area | V1 quantity / boundary | Reason |
|---|---|---|
| Country and divisions | One fictional country, three divisions of 16 clubs | Gives promotion, relegation, and different economic tiers without multiple rule sets |
| Clubs | 48 persistent named clubs; six purchasable starting scenarios, two per tier | Simulate a credible competitive pyramid; concentrate acquisition writing and balancing |
| Competitions | Three league competitions plus one domestic knockout cup | Familiar structure; no continental or international competitions |
| Fixtures | 30 league matches per club; cup adds up to six; season spans 52 weeks with automatic skipping of quiet periods | Familiar season rhythm without daily clicking |
| Players | Approximately 1,200 at start: 48 × 22 senior players plus 144 free agents | Enough depth for injuries, aging, scarcity, and trading |
| Long-term player population | Target 1,100–1,500 active people through intake, retirement, and retirement from the professional pool | Prevent depletion or uncontrolled accumulation; no unexplained disappearance of contracted players |
| Youth | Aggregate academy pipeline; up to two named graduates per club per year | Development has consequences without a second playable league |
| Leadership | Three hireable roles: CEO, sporting director, manager | Covers business, recruitment, and football accountability |
| Staff market | 144 occupied posts plus approximately 36 available candidates initially | Enough choice; replacements keep the market supplied |
| Facility investments | Stadium capacity, training center, academy, hospitality; three upgrade steps per category | 12 upgrade definitions applied to existing club assets |
| Financial systems | Six: operations, transfers/contracts, capital projects, loans, owner capital/distributions, acquisition/valuation/exit | Depth comes from their interaction, not many financing instruments |
| Sponsor categories | One principal sponsor and one stadium-naming agreement | Creates commercial choices without inventory micromanagement |
| Strategy mandates | Balanced growth, promotion push, talent development, player trading, commercial resilience | Reuses the same rules with different priorities; no exclusive bonus trees |
| Ambition and guidance | Consolidate/build/push presentations of the existing capital plan; help adjustable during a career | Makes challenge selectable without adding strategy systems or altering world fairness |
| Events | 12 families; 48 authored contextual templates; six short linked arcs drawn from those templates | Sufficient authored coverage with simulation-driven variation |
| Career | Ten-season assessment, optional continuation to season 50 | Offers closure and long play within a testable support boundary |
| Achievements | 12 focused achievements | Recognizes varied owner behavior without grind content |

### Ownership and product-mode decisions

| Question | Decision and explanation |
|---|---|
| One club? | Yes. Owner buys 100% equity in one curated opportunity and stays until retirement, sale, or loss of control. |
| Buy another club after selling? | No in V1. Sale ends the save; a new save supplies the next acquisition. Serial acquisitions add another progression economy. |
| Multi-club ownership? | Later expansion only. Shared players, conflicts, consolidated capital, and regulation are substantial new systems. |
| Multiplayer? | No. It multiplies pacing, fairness, and save expectations before the solo loop is proven. |
| Mod support? | No supported editor or Workshop at launch. Do not promise that private modifications remain compatible. |
| Real teams or players? | No. Fictional names, identities, competitions, sponsors, and generated people throughout. No lookalike branding. |
| Generated players? | Yes. Persistent names, age, role group, ability, development outlook, wages, contracts, and history. |
| Outside equity investors? | Deferred. Sole ownership avoids cap tables, dilution, votes, and investor negotiation scope. |
| Leveraged acquisition? | Deferred. Owner purchases equity with personal capital; existing club debt may be inherited and disclosed. |
| Create-a-club? | Deferred. Buying an existing institution strengthens the opening fantasy and limits setup complexity. |

### Simplify and abstract deliberately

- The CEO combines CFO and commercial-director responsibilities; the sporting director combines recruitment and academy oversight.
- Medical care is a standardized operating cost and injury-recovery factor, not a staff hiring department.
- Scouting is a budgeted service level under the sporting director; no scout itineraries or regional knowledge maps.
- Youth football, reserve fixtures, training sessions, merchandising inventory, catering, agents, and sponsor delivery are abstracted.
- Transfer consideration is paid upfront. No loans of players, installments, sell-on clauses, or contingent bonuses in V1.
- Construction is a fixed quoted commitment paid at approval, followed by a delivery period and ongoing costs. No building placement or contractor management.
- Financial rules are fictional and explicitly described in-game; no replication of current national or UEFA regulations.
- The lowest tier is closed: no fourth-tier relegation or off-screen club replacement. Bottom finishes still reduce receipts, reputation, and supporter trust. This boundary is disclosed at setup.

### Scope protection rule

A new feature must replace comparable work or demonstrate that an existing release gate cannot be met without it. Add content to an already proven rule before adding a new rule. If the owner loop is weak, adding countries or tactical detail is not the remedy.

## G. Detailed Systems Requirements

### G1. Owner decisions, ranked

| Rank | Decision | Why it earns owner attention | Frequency |
|---|---|---|---|
| 1 | Choose the acquisition, price, and retained personal reserve | Establishes the entire risk and return proposition | Start of save |
| 2 | Set the capital plan: wages, transfers, projects, cash protection | Creates the game's principal opportunity cost | Preseason, with midseason review |
| 3 | Hire, retain, replace, and mandate the three leaders | Determines autonomous execution across the organization | As contracts or performance warrant |
| 4 | Fund a major roster commitment or approve a major player sale | Trades near-term sporting capacity against liquidity and future value | A few times per transfer window |
| 5 | Choose a facility investment and its timing | Locks cash into delayed, uncertain returns | Typically one major approval per season |
| 6 | Borrow, refinance, inject owner capital, or reduce commitments | Changes survivability and financial risk | Infrequent; tied to plans or warning thresholds |
| 7 | Maintain or change the ownership thesis | Distinguishes patience from refusal to learn | Usually annual; midseason change is costly |
| 8 | Set ticket policy and choose material sponsor terms | Balances recurring income and supporter trust | Annual; sponsorship at expiry |
| 9 | Set a distribution or reinvest surplus | Makes personal returns compete with continued growth | Year-end only |
| 10 | Seek, negotiate, accept, or reject a sale | Realizes value and concludes the ownership story | Optional after two completed seasons |

Manager firing is not a weekly interaction. The owner should evaluate results against the resources, injuries, and agreed horizon. Selecting a formation, starting a player, organizing training, giving team talks, choosing set pieces, and making substitutions are never owner decisions.

**Common acceptance rule:** every material proposal shows its owner-level purpose, affordable alternatives, cash and recurring commitments, execution owner, risk, and next review date. A new approval must never silently create an obligation beyond the displayed mandate.

### G2. Acquisition, ownership, and sale

**Acquisition flow:** compare the six opportunities → inspect a one-page diligence brief → choose to pay the asking equity price or make one lower counteroffer → receive acceptance or rejection → confirm purchase and opening club funding. A rejected counteroffer leaves the original asking offer available during setup. The negotiation is short; valuation judgment matters more than bargaining clicks.

Each opportunity provides:

- Identity, tier, recent history, supporter expectations, and investment problem.
- Equity purchase price, indicative enterprise value, cash in the club, outstanding debt, and near-term obligations.
- Starting personal capital and the reserve left after purchase; the reserve cannot be replenished from an outside business.
- Wage commitments, contract expiries, squad strengths, executive contracts, and facility condition.
- Downside and base-case cash forecasts. Known obligations are complete; recruitment potential and future performance are uncertain. No undisclosed opening debts.

**Owner cash is separate from club cash.** Paying the seller does not fund the club. Every injection reduces personal cash and increases invested capital. A distribution reverses that cash movement but does not become business revenue.

**V1 ownership rights:** the player controls the club and cannot be dismissed by a board vote. There is no minority shareholder simulation or board revolt. Pressure comes from creditors, supporters, contracts, executive retention, and financial restrictions.

**Voluntary exit:** after two seasons, the owner may initiate a sale review at year-end. Qualified offers depend on current prospects and bidder interest; an indicative valuation is not guaranteed liquidity. A review produces zero to two credible offers with a price, expiry, and total seller proceeds. The owner may accept, reject, or make one counteroffer. A sale process cannot be spammed within the same year.

Each offer identifies its valuation date, assumed assets and obligations, and a four-week expiry. Before acceptance, reconcile ordinary cash movements and outstanding debt into the current equity proceeds. Distributions, new financing, and material asset or contractual changes require the bidder to reconfirm or reprice the offer; the bidder may withdraw. Assess related changes cumulatively so several small transactions cannot evade review. Show the revised net proceeds before final confirmation. The owner cannot strip value from the club and still accept a price based on the earlier assets.

**Sale completion:** proceeds reflect the agreed equity price after shown transaction costs and any explicitly required debt settlement; never deduct assumed debt twice. A completed sale ends the career and displays realized return, club health, supporters' response, and a historical summary. No post-sale simulation or new acquisition is required.

**Acceptance:** buying, injecting cash, distributing cash, and selling must each make the destination of money obvious. Rejecting an offer preserves control and does not guarantee a better future price.

### G3. Living world and competitions

The other 47 clubs must remain economically and competitively active under the same affordability and sporting rules. They need persistent consequences, not detailed private inboxes.

| World behavior | Required minimum |
|---|---|
| League competition | Home and away fixtures, standings, seasonal records; three points for a win, one for a draw; equal points are separated by overall goal difference, overall goals scored, then head-to-head points and goal difference among the tied clubs. Any remaining exact tie, including a multi-club tie, is resolved by a recorded seeded drawing of lots. This published order applies to every position, including the title; loading cannot redraw the lots. |
| Promotion/relegation | Top two rise and bottom two fall between adjacent tiers; Tier 1 has no promotion and Tier 3 has no relegation. Final positions use the complete league tie-break rule; there are no playoff fixtures. |
| Cup | All 48 clubs enter; 32 drawn clubs play a preliminary round, 16 receive byes; 32 remain for five knockout rounds; extra time and penalties, no replays |
| Recruitment | Clubs identify needs, pursue affordable candidates, negotiate, renew, and sell; bids can fail |
| Leadership | Executives age, change jobs, retire, and are replaced; managers can be fired for sustained performance gaps |
| Player lifecycle | Aging, availability, development, contract expiry, retirement, and academy graduation |
| Business | Wages and bills get paid; contracts renew; attendance and sponsorship respond to actual conditions |
| Capital allocation | Clubs can improve facilities or preserve funds; investment follows cash and strategic needs |
| Debt/distress | Credible borrowing limits and repayment; distressed clubs sell assets, cut wages, or undergo rescue |
| Valuation | Changes with persistent earning capacity, assets, risks, and tier |
| Ownership change | Abstract rescue or takeover changes a rival's finite capital allowance and mandate; no detailed shareholder simulation |

Rivals have a small set of persistent priorities: grow, sustain, trade talent, or survive. They react to resources and sporting position. These are decision tendencies, not hidden income multipliers.

**World boundary:** no international transfer market. Domestic clubs, academy graduates, and the free-agent pool sustain the market. People leaving professional football are explicitly recorded as retired or departed; they are not a source of limitless sale revenue.

If a rival becomes insolvent, the game resolves ownership rescue and restructuring transparently while preserving its fixtures and history. Rescue funds are finite and recorded; restructuring carries asset/wage losses and supporter consequences. A player-club creditor rescue uses the same survival mechanism, but loss of ownership ends that player's career.

**Acceptance:** a rival that overspends must face consequences; a sold player remains visible at the buyer; promotion changes the next season's economics; repeated rescues cannot grant competitive advantage. Long-term tests must find neither universal insolvency nor universal richness.

### G4. Club economics

#### Financial language and presentation

The default view answers five questions: **What can we spend? What have we already promised? What do we earn regularly? What could go wrong? What are we building toward?**

Use a cash-first financial report. Label “operating cash surplus” accurately; do not present it as statutory accounting profit. A glossary explains cash, debt, equity value, transfer fee, and capital investment in one sentence each.

#### Revenue and costs

| Category | Product behavior | Owner lever |
|---|---|---|
| Broadcast receipts | Published tier payment schedule plus final-position award; next-tier receipts only after promotion is secured and the new season starts | Sporting strategy; downside planning |
| Matchday tickets | Paid attendance × realized ticket yield; demand reacts to prices, performance, capacity, reputation, and support | Annual price policy and stadium capacity |
| Principal sponsorship | Fixed-term offer with clear annual receipts and any promotion/relegation adjustment | Choose security versus higher conditional value |
| Commercial income | Aggregate merchandise and local partnerships; reflects fanbase and reputation with bounded growth | CEO quality and commercial operating budget |
| Hospitality | Separate capacity and yield, shown within total matchday receipts without double counting | Facility investment and annual pricing policy |
| Cup receipts and awards | Earned from actual progression; potential awards excluded from committed spending power | No direct lever; preserve reserves |
| Player sales | Upfront cash receipts shown separately from recurring operations | Approve major sales; set trading mandate |
| Squad wages | Contracted recurring payments; agreed promotion increases and relegation reductions are visible at signing | Wage ceiling and material contract approval |
| Leadership wages | Three contracts, renewal costs, and explicit dismissal compensation | Hire/retain/fire |
| Club operations | Match operations, administration, medical provision, maintenance, and general running costs | Scale and service envelopes, not line-item chores |
| Academy/scouting/training | Recurring development and recruitment spending; upgrades may add costs | Funding priority and service level |
| Transfer fees | Upfront acquisition outflow; wages displayed separately and in total contract commitment | Transfer envelope and exceptions |
| Capital projects | Quoted cash paid at approval, delivery later, ongoing cost after opening | Which project and when |
| Debt service | Interest and principal shown separately; repayment occurs on disclosed dates | Borrowing or refinancing |

Show opening cash + receipts − operating payments − transfers − projects − debt service + financing + owner injections − distributions = closing cash. Staff reports reconcile the actual movement using this same explanation. Broad operating costs bundle secondary expenses; no tax filing, depreciation schedule, or transfer amortization interface is required.

#### Budgets and affordability

- A wage ceiling is permission for future contracts, not a cash reserve. Reducing it does not cancel existing contracts.
- A transfer envelope allocates permission, but a deal also needs sufficient cash and a safe forecast.
- Display a 12-month base-case and downside cash path, including the lowest expected balance and next large obligation.
- Uncertain sales, speculative cup receipts, and unaccepted financing do not fund new commitments.
- Every proposal updates these forecasts before confirmation. Amounts are not deducted twice through both a budget allocation and the underlying payment.
- The owner chooses a cash reserve target. Staff cannot cross it; the owner may approve an explicit exception after seeing the forecast, except where a contractual or regulatory restriction prohibits it.

#### Debt and refinancing

One standard fixed-rate term-loan product, with at most two outstanding loans, is sufficient. Show amount advanced, fees, rate, annual debt service, maturity, early-repayment terms, and restrictions in a single offer card.

Borrowing availability depends on sustainable cash generation, existing debt service, and pledged asset support. Lenders do not treat a speculative player appraisal as spendable collateral at full value. V1 refinancing replaces an existing loan with disclosed terms and charges; **no cash-out refinancing**. New borrowing must pass a separate credit check.

Contracts may prohibit distributions and new borrowing while breached. A covenant is simply a lender's required limit; display the limit and cure date in ordinary language. Missing principal is different from recording an operating deficit.

#### Owner capital and regulation

Owner injections are limited by the personal reserve set at acquisition. Distributions occur at year-end only, require compliance with the loan terms, and must leave forecast essential obligations covered. They cannot be financed by a simultaneous new loan.

Use one fictional **squad wage affordability rule**: contracted annual squad wages must stay within a published proportion of eligible recurring revenue. Initial tuning: 75%; eligible revenue uses recent recurring receipts and confirmed next-season broadcast/sponsor contracts, excluding player sales and owner injections. The exact calculation and denominator must be visible.

Use one annual revenue denominator: the preceding 12 months of realized ticket, hospitality, and recurring commercial receipts, plus the contracted annual broadcast and sponsorship amounts for the season being assessed. Confirmed next-season contracts **replace** the corresponding current-season components when assessing next-season commitments; they are never added on top. Exclude cup windfalls, financing, and overlapping sponsorship receipts from commercial totals. Opening scenarios supply the same historical period, so a new save does not lack its denominator.

If relegation makes existing contracts breach the rule, a one-season transition permits those contracts but restricts net new wage commitments. Continued breach after that period imposes a registration embargo until cured; no surprise real-world sanctions. Rules apply to every club. This provides pressure without recreating financial regulation manuals.

#### Owner dashboard metrics

**Always visible:** club cash, lowest forecast cash balance and date, spending headroom, league position versus ambition, estimated equity-value range, and supporter approval.

**One click deeper:** operating cash surplus, annual revenue, wage/revenue ratio, debt balance and annual service, approved project commitments, estimated squad sale value, personal reserve, cumulative owner capital, and owner return. The dashboard uses six headline items, not twelve equal-weight gauges.

**Acceptance:** a player must be able to explain why a wealthy-looking club can run out of cash, why a player sale is not recurring income, and why borrowing does not itself create equity value.

### G5. Valuation and owner success

Valuation must be a readable estimate with buyers who can disagree. It is not a magic score that can always be cashed out.

**Enterprise value** estimates the operating club before cash and financing. **Equity value** estimates the owner's stake after cash and debt. In the simplified V1 financial model: equity value = enterprise value + club cash − outstanding loan principal. No unpaid transfer installments exist; any exceptional arrears reduce seller proceeds explicitly.

Use two separate lenses:

1. **Going-concern value:** sustainable recurring revenue at a bounded multiple, adjusted for cash generation, tier security, commercial demand, squad sustainability, and club reputation. Explain the leading positive and negative drivers.
2. **Orderly asset-sale value:** conservative realizable squad and facility values, less disposal and closure costs. This is an alternative reference, not a sum added to going-concern value. In a distressed forced sale, realizations may be lower.

The indicative range uses the more relevant supported basis. Do not add the full squad and stadium values to an earnings value that already depends on those assets. An unfinished project has only its recoverable value; no automatic pound-for-pound increase on approval. Valuation changes primarily at quarterly reviews, material sporting changes, or major asset transactions, with explanations.

Sanity checks:

- Borrowing £1m and retaining £1m in cash does not raise owner equity value before fees.
- An owner injection can increase club equity value, but increases owner invested capital by the same amount.
- Selling a key player can raise cash while lowering going-concern prospects; it cannot count as both retaining the player and receiving sale proceeds.
- Paying for an upgrade does not guarantee an immediate equal valuation gain.
- One lucky cup run does not permanently capitalize its prize money as recurring revenue.

#### Measuring an ownership career

| Measure | Meaning | Role in evaluation |
|---|---|---|
| Net owner value created | Current equity estimate + cumulative distributions − purchase equity cost − subsequent injections | Primary financial outcome during ownership; explicitly unrealized |
| Realized owner gain | Net sale proceeds + distributions − purchase cost − injections | Financial outcome on completed sale |
| Owner capital multiple | (Net sale proceeds + distributions) ÷ total owner capital invested; estimated equity substitutes for proceeds before sale | Accessible return-on-invested-capital summary; always show years held |
| Sustainable club health | Operating cash trend, runway, debt service, recurring revenue, wage burden | Checks whether growth can survive |
| Sporting achievement | Promotions, titles, cup results, sustained tier position | Emotional and economic achievement |
| Institutional legacy | Supporter trust, facilities, academy contributions, club reputation | Recognizes building value beyond an exit |

Do not compress all measures into one weighted leaderboard. A ten-season report shows **Owner Return, Club Health, Sporting Progress, and Legacy** side by side. Scenarios set financial and sporting objectives plus a solvency requirement; secondary legacy goals provide different definitions of a satisfying run.

Unspent personal reserve remains visible as personal wealth but is not counted as value created by the club. Display cumulative returns with holding period; annualized returns are optional later detail rather than another required dashboard metric.

### G6. Leadership and delegation

| Role | Controls and autonomous work | Reports to owner | Owner approval/override |
|---|---|---|---|
| CEO | Routine operating spending, commercial delivery, sponsor search, financial forecasting, project administration | Cash forecast, commercial demand, debt offers, facility proposals, business variance | Annual budgets, loans, distributions, price policy, major sponsors and projects; owner may change mandate or replace CEO |
| Sporting director | Recruitment service, candidate selection, routine transfers and renewals, academy progression, contract planning | Squad cost/value, age and expiry risks, shortlists, talent pipeline, manager recommendation | Material deals, exceptional contracts, protected-player sales, manager appointment; owner can reject a proposal or request a changed financial constraint |
| Manager | Team selection, match preparation, tactics, training allocation, rotation, substitutions, player usage | Results against squad expectation, injuries, morale, squad needs, development evidence | Owner sets season ambition, available resources, and retention; never overrides lineups or match instructions |

Staff are distinct through **competence, strategic preference, temperament, salary, and contract horizon**. Show broad ratings and evidence rather than dozens of hidden attributes. Better staff improve execution and report accuracy; they do not hide essential obligations from owners with cheaper staff.

Hire from a three-candidate shortlist. Explain likely fit, salary, dismissal/appointment cost, and conflicts with the chosen mandate. Salary and reputation make stronger candidates harder to attract. Hiring is not a universal upgrade ladder: a survival manager may be a poor developer, and an excellent trader may undermine continuity.

**Autonomy rules:**

- All transactions inside mandates and below materiality thresholds proceed automatically, with a digest afterward.
- Initial major-deal threshold: upfront fee of at least 10% of eligible annual recurring revenue, or annual wages of at least 10% of the squad wage ceiling. A sale of a top-three sporting contributor or major supporter favorite also requires approval regardless of fee.
- The threshold is not a slider that can be reduced to zero. Owners may request more reports, but V1 does not provide manual approval of every routine signing.
- Any breach of mandate, forecast reserve, or financial rule escalates before commitment. Multiple small deals still share the same cumulative budgets.
- A vacancy uses a limited interim appointment until the owner fills it; no softlock and no permanent free substitute with top performance.
- Midseason strategic changes require a revised proposal with contract, morale, and time consequences. They do not instantly remake the squad.

**Acceptance:** a season can proceed without approving routine recruitment. Staff demonstrably act within their authority and can explain major outcomes. The owner can meaningfully hold them accountable without doing their jobs.

### G7. Football simulation and matchday

V1 needs credible aggregate football outcomes, not a detailed spatial match engine.

Persistent named players have a role group (goalkeeper, defender, midfielder, forward), current ability, uncertain development outlook, age, availability, morale, wages, remaining contract, and sale estimate. Training and academy investment influence development with diminishing returns; prospects can fail. Injuries and aging create replacement needs.

The manager autonomously selects available players. The match outcome reflects the resulting team's attacking and defensive strength, goalkeeper quality, depth and fatigue, cohesion, morale, manager competence, home advantage, opponent strength, and bounded randomness. No hidden tactical puzzle is exposed to the owner.

The squad is more than a single purchased rating: an expensive group with no depth should be vulnerable to injuries; constant turnover should disrupt cohesion; a cheap developing squad should take time. High spending improves expected results without guaranteeing them.

**Recommended presentation:**

- A brief pre-match card gives form, availability, stakes, and expected competitiveness.
- A skippable 15–30 second score timeline shows approximately 3–6 material text moments: goals, dismissals, injuries, or a decisive late event.
- A final card shows score, shots/chances, named scorers, one or two major contributors, league consequence, attendance/receipts, and availability changes.
- A monthly review uses a sample of matches to discuss squad strength and executive execution. Do not infer managerial failure from one loss.
- “Instant result” is always available. Watching, speeding up, or skipping cannot alter outcomes or hide an owner decision.

Text moments and statistics must agree with the resolved match. No 2D player movement, 3D matches, generated video, possession-by-possession commentary, or interactive highlights in V1. Matchday art can use crests, stadium cards, sound cues, and restrained animation.

**Acceptance:** better-resourced, well-run squads outperform weaker peers across large samples; upsets remain possible. Players can identify plausible contributors to a result without seeing a tactical control.

### G8. Transfer market

The owner trades **capital commitments and organizational risk**, with the sporting director handling the search.

Flow: manager identifies a squad need → sporting director assesses candidates → routine deals proceed inside authority → material recommendation comes to owner → executive negotiates within the approved ceiling → final commitment and later performance enter the review.

Each recommendation contains at most three options, including waiting when plausible. Show fee, wages, contract length, total cash commitment, probable sporting contribution, resale outlook, uncertainty, and the consequence of the sale or signing for the existing squad.

The owner can approve a ceiling, reject, or request a cheaper, younger, more immediately effective, or lower-risk alternative. The owner cannot search the entire player pool and personally negotiate every contract. A read-only roster supports emotional connection and review.

Financial interest comes from:

- Buying readiness versus future potential.
- Accepting a strong bid before contract expiry versus retaining promotion capacity.
- Paying higher wages for a low-fee player.
- Letting the best graduate contribute versus funding the academy's next generation through a sale.
- Keeping sale proceeds as reserve versus making a replacement.
- Managing a replacement's adaptation period and an existing player's fan importance.

Two transfer windows per year; renewals may occur throughout the year. Free-agent signings outside windows can fill genuine squad shortages under the same budgets. Contracts initially span one to four years and expose any automatic tier-related wage changes. No agent minigame, player loans, installment financing, swap deals, or individual promises beyond simple contract terms.

Rivals and players may reject proposals because of price, available funds, wages, playing prospects, or club stature. There are no guaranteed buyers or universal instant-sale buttons. Distress lowers bargaining power. Bid and valuation uncertainty must make repeated buy–sell flips unattractive rather than relying only on arbitrary cooldowns.

**Acceptance:** loss of a major player changes both sporting prospects and finances; all-club trading remains affordable; staff respect cumulative budgets; transfer refusal does not block advancing time.

### G9. Facilities and capital projects

Four upgrade categories, each with three steps, create enough investment variety. Existing assets and expansion limits vary by club. Only one major construction project can run at a time; operating training and academy budgets continue separately.

The following are initial gameplay tuning ranges. “Revenue” means the club's eligible annual recurring revenue at proposal time; actual offers appear as currency amounts. Benefits depend on demand, staff, and completion, not a guaranteed percentage boost.

| Investment | Indicative cash cost | Delivery / benefit horizon | Benefit | Tradeoff |
|---|---|---|---|---|
| Stadium capacity | 40–100% of revenue per step | 12–18 months; expected recovery over several successful seasons | More sellable seats when demand exists | Cash lockup, fixed upkeep, temporary capacity disruption, empty seats if demand disappoints |
| Training center | 15–30% | 6–9 months; development effect over 1–3 seasons | Better development and conditioning within player potential | Recurring staffing/maintenance; older or frequently traded squads capture less benefit |
| Academy | 15–35% | 6–12 months; meaningful graduate output usually 3–5 seasons | Better chance of useful graduates and stronger pipeline | Long delay, uncertain prospects, ongoing funding, ties up capital needed for survival |
| Hospitality | 10–25% | 6–9 months; receipts after opening, possible recovery over 2–5 seasons | Higher-yield matchday capacity and local business demand | Limited market, possible premium-demand saturation, ongoing service cost |

Minimum cost floors, existing size, and club-specific capacity limits stop tiny clubs buying implausibly cheap large stadiums. These ranges are not final quotes or guaranteed payback periods.

Scouting service has three annual funding bands under the sporting director; it improves the quality and breadth of available recommendations. It is not a fifth building. Medical services are included in operations. Commercial infrastructure is represented by CEO capability and the hospitality investment. Standalone departments and building trees are deferred.

Every project card shows expected completion, upfront cost, operating cost afterward, assumptions, demand range, and temporary disruption. V1 pays the quote upfront; completion delays are the only construction complication and must follow a relevant event condition. Cancellation returns only the displayed recoverable amount. No surprise cost overruns or hidden installment debt.

**Acceptance:** the same upgrade can be excellent at one club and wasteful at another. Benefits are visible after delivery, and investment history links later improvements to the original decision.

### G10. Supporters and club identity

Every club has a crest, colors, town, short founding story, one rivalry, and two identity traits. Example traits: youth pride, affordable football, loyalty to local heroes, or immediate ambition. These traits change supporter responses rather than awarding universal business bonuses.

Use **one supporter-approval measure with visible drivers**, plus slower-changing fanbase size. Recent results affect mood; repeated owner choices affect trust. Never use an inscrutable network of supporter factions in V1.

| Decision or event | Supporter consequence | Business consequence |
|---|---|---|
| Raise annual ticket policy | Reaction depends on affordability identity, results, and demand | Higher yield may reduce paid attendance and renewals |
| Sell a favorite | More negative without a credible replacement or explained financial need | Attendance and merchandise can soften; cash improves |
| Fire a manager | Depends on tenure and performance against expectations | Compensation plus possible disruption; no guaranteed new-manager bounce |
| Promote graduates | Particularly valued by youth-pride clubs | Slow loyalty/reputation benefit, with football performance still required |
| Choose sponsor/naming terms | Reaction reflects a stated local identity conflict or acceptance | Extra revenue can cost trust; no licensed brands or elaborate ethics simulation |
| Break a declared commitment | Repeated reversals reduce trust | More price sensitivity and slower recovery |
| Supporter protest | Triggered by sustained very low approval and a relevant grievance | Bounded attendance/commercial impact, visible duration and recovery route |

Supporters do not directly subtract arbitrary goals from matches or dismiss a sole owner. Their influence flows mainly through demand, reputation, and leadership pressure. Recovery depends on addressing the grievance over time, not buying an instant “fan happiness” boost.

Relocation, club renaming, crest redesign, political campaigning, and a free-form social feed are excluded. Stadium naming is a bounded contract decision, not a general rebranding system.

**Acceptance:** the player can explain the three main approval drivers and forecast a likely reaction before making a material identity decision.

### G11. Events and emergent stories

Events surface the simulation's tensions. They do not replace its rules with unrelated cash rewards or disasters.

| Event family | Grounded trigger | Owner response / story consequence |
|---|---|---|
| 1. Promotion opportunity or aftermath | Actual table position or secured promotion | Protect reserve versus accelerated spending; prepare new-tier economics |
| 2. Relegation danger or aftermath | Sustained risk or completed relegation | Correct budget, retain/fire leadership, plan wage reductions |
| 3. Major player wants a move | Contract stage, club stature, credible interest, morale | Consider a real bid or retain with explicit risk |
| 4. Executive conflict or retention | Mandate mismatch, blocked resources, outside job offer | Clarify authority, change mandate, pay to retain, or replace |
| 5. Liquidity/covenant crisis | Forecast deficit, due payment, or breached threshold | Sell, inject, borrow if eligible, reduce commitments |
| 6. Sponsor renewal or material offer | Contract expiry, audience change, tier change | Trade certainty, income, and identity fit |
| 7. Takeover interest | Scheduled sale review or credible buyer interest in a valuable club | Compare realizable offer with holding; incoming interest at most once per season |
| 8. Facility problem or delay | Project stage, old asset condition, documented disruption | Accept delay, cancel at stated recovery, or adjust operating plan |
| 9. Supporter protest | Persistently low trust plus active grievance | Change the relevant policy or accept ongoing demand impact |
| 10. Academy breakthrough | Named prospect from an eligible pipeline | Allow development, approve a major bid, fund future capacity |
| 11. Major injury / depth exposure | Simulated injury and squad dependence | Let staff cover internally or approve a material replacement |
| 12. Rival change / market opportunity | Rival distress, vacancy, sale listing, or takeover | Consider a real recruitment opportunity or revise expectations |

Investor ultimatums are deferred with outside equity. Lender deadlines provide the V1 financing pressure.

Each template defines eligibility, evidence shown, plausible choices, immediate and delayed effects, expiry/default response, and repeat protection. At least six arcs link existing templates over time: academy graduate to transfer dilemma; promotion push to wage strain; favorite sale to supporter reaction; project delay to cash pressure; manager conflict to replacement review; distress to rescue or exit.

**Investment-to-story connection (EXP-01, EXP-04):** the academy arc should be able to recall when the owner funded development, which staff executed it, a named graduate's actual emergence and senior contributions, and any subsequent material bid. Illustrative outcome: the owner funds an academy instead of immediate squad strength; years later the manager independently gives a graduate opportunities; the player contributes to a promotion push; a real bid creates a choice between sporting continuity and financing the club's future. Keeping and selling can both produce satisfying careers.

This is a possible sequence, not a guaranteed wonderkid or promotion script. Interim reports expose real project or pipeline progress without accelerating academy payback. Failed prospects can also produce useful reviews. Use the same historical approach for the manager retained through a crisis or the stand opened after years of saving. No extra relationship meter, personal-life simulation, or separate event quota is required.

Frequency targets: 8–12 substantial contextual event decisions per player season; this is a tuning target, not a forced quota. Standard budget, hiring, and material transfer decisions are additional. Batch related notices and avoid consecutive duplicate prompts. Resolved consequences persist in club history. Incidents arise from bounded probabilities conditional on the world, never from a hidden demand that a successful player must suffer a setback.

**Acceptance:** every event can point to a real state or recorded incident; no bid exists only inside a text card; an expired offer cannot be accepted; mutually exclusive events cannot both resolve.

### G12. Difficulty, crisis, and failure

The challenge is planning under uncertainty with limited money and contractual inertia. Good decisions improve prospects; they do not remove risk. Bad leadership choices matter through repeated execution, not unexplained random incompetence.

| Setting | Transparent differences | Constant across settings |
|---|---|---|
| Guided Owner | Larger disclosed starting personal reserve, more explanations, earlier forecast warnings | Rival economics, match rules, contract obligations, visible restrictions |
| Standard | Scenario's baseline personal reserve and normal forecasts | No hidden player advantages or rival subsidies |
| Constrained Owner | Smaller disclosed reserve, tougher existing acquisition situation, less optional guidance | No biased match rolls or magically richer rivals |

Difficulty selects the initial economic situation and default assistance at career creation. Starting resources and inherited obligations remain fixed by that selection; changing help afterward does not reset or modify them. Display the original scenario and difficulty on final reports; do not compare them as an unqualified leaderboard.

**Three layers of challenge (EXP-06–EXP-08):** choose an appropriate starting scenario; let actual club development create new constraints; allow player-selected guidance and ambition. I4 defines the progression and ambition choices. Difficulty never changes already-signed loan terms or artificially punishes strong performance.

**Guidance during a career (EXP-07):**

| Player need | Available support | Invariant |
|---|---|---|
| Understand an apparent cash shortfall | Expand the payment timeline and explain the cash movement step by step | Same amounts, obligations, and deadlines |
| Compare affordable alternatives | Request the executive's comparison of cost, timing, and downside | Same options and honest uncertainty |
| Reduce repetitive explanations | Collapse guidance and use the compact brief | Essential facts and critical warnings remain accessible |
| Seek greater challenge | Choose a more ambitious plan at an existing strategy review, or a harder acquisition in a new save | Same current world rules and signed contracts |

Guided Owner's earlier prompts are optional coaching about emerging risks; mandatory commitment and crisis warnings cannot be disabled at any setting. Players can access exact known contractual facts in every presentation. There is no hidden skill score or behavioral profiling; decision speed, losing streaks, loading a save, and avoiding risk do not automatically change assistance or the world.

**Crisis ladder:** forecast warning → explicit payment or covenant breach → four-week recovery period with spending/distribution restrictions → creditor resolution if still insolvent. If essential payroll cannot be met, the club enters administration immediately and the recovery period proceeds under restrictions. Club assets are not allowed to fund unlimited additional commitments while in recovery.

Recovery options are finite personal injection, executable player sale, eligible financing, or a real offer to acquire the club. Budget cuts reduce future discretionary costs; they do not erase wages already owed. If insolvency remains unresolved, creditor action forces loss of control; the career ends with a transparent proceeds/loss report. An abstract rescue preserves the club in the world.

- Operating loss alone is not game over.
- Negative estimated equity alone is not game over; inability to meet obligations drives insolvency.
- Relegation alone is not game over.
- Low supporter approval alone is not game over.
- There is no board dismissal or investor revolt under sole ownership.
- Voluntary sale and retirement are valid endings, including modest financial outcomes.
- Manual saving and loading are allowed. There is no required ironman mode.

**Acceptance:** players see a preventable financial crisis coming, understand what remains actionable, and receive a causal explanation at failure. No single unforecastable routine event can instantly liquidate a previously sound club.

Distinguish a **productive setback** (the owner can explain what went wrong and identify a viable next move), **overload** (the owner cannot understand the situation or available actions), and **earned insolvency** (real resources and recovery options are exhausted under known rules). Improve explanation and presentation when the player is overloaded; do not manufacture money, bids, favorable matches, or postponed contractual deadlines. Recovery still depends on the resources and options described above.

## H. UX / Screen Requirements

### H1. First 30 minutes

Use one recommended acquisition: a solvent middle-tier club with a constrained budget, credible staff, an aging key player, and an underused development opportunity. Guided information can be scripted; subsequent match results remain live. Other scenarios are accessible immediately with a clear difficulty indication.

| Time | Experience | Learning demonstrated |
|---|---|---|
| 0–3 min | See the club, town, history, price, and retained reserve; complete purchase | Understand ownership, club cash, and personal cash |
| 3–6 min | CEO gives a three-item brief: ambition, cash constraint, and executive responsibilities | Identify what the owner controls and who runs football |
| 6–10 min | Make the first allocation between two credible uses of capital and cash retention | Understand opportunity cost and recurring obligations |
| 10–14 min | Approve a leadership mandate; inspect one material recruitment proposal | Learn delegation and why staff may act autonomously |
| 14–18 min | Advance to opening results using the optional timeline | Understand that football is observed and interpreted |
| 18–23 min | Review a material proposal or scheduled first-month variance report | See actual spending and evidence; a quiet start does not require a fabricated crisis |
| 23–27 min | Compare forecast against actual cash; see the next commitment | Distinguish available cash, annual budget, and future bills |
| 27–30 min | Set the next review horizon; see career objectives and save | Know what success means and how to resume |

The introduction must not require learning all financial vocabulary. Explain a concept at the decision where it matters. No mandatory tour of every tab, financial exam, tutorial debt trap, or scripted guaranteed win. Tutorial prompts remain dismissible and replayable from help.

Introduce one unfamiliar concept at a time, let the player practice it, then combine familiar concepts into a more demanding choice. Keep one major learning decision visually central during the guided opening. This is presentation scaffolding; it must not hide material obligations, lock experienced players out of core systems, or postpone real deadlines.

### H2. Minimum navigation

Use **six primary workspaces**, with dossiers and setup/end screens rather than a dozen separate destinations.

| Workspace | Required contents and owner action | Acceptance condition |
|---|---|---|
| Owner Desk | Six headline metrics, ranked decisions, club brief, next review date, continue controls, recent actions | Within 30 seconds, player finds what needs attention and the main risk |
| Finances & Value | Cash history/forecast, revenue and costs, debt, personal capital, valuation drivers, distribution and sale review | Every headline number has an understandable breakdown; forecasts distinguish certainty |
| Leadership & Strategy | Three executives, comparison/hiring, contracts, mandates, budget plan, accountability reviews | Player can assign authority and assess execution without tactical tools |
| Football Operations | Read-only roster, needs, major proposals, contract/age risks, academy pipeline, results | A material deal can be judged without opening dozens of profiles |
| Club Development | Four project categories, demand/condition, supporter drivers, identity, price and sponsor proposals | Every investment exposes time, cost, demand assumptions, and fan consequence |
| League & World | Tables, fixtures, cup, rival club dossiers, significant transfers, club history | World activity remains inspectable and selected favorites are easy to follow |

The inbox is the Owner Desk's prioritized queue, not a separate mandatory screen. Match results appear as an overlay or report. Acquisition comparison, settings, save/load, and end-of-career reports are required supporting screens. A full Club Market workspace is deferred because ongoing acquisitions are absent.

### H3. Interaction requirements

- Every monetary amount states the period: cash now, annual wage, monthly payment, or total contract cost.
- Forecast comparisons use identical periods and units. Show rounded values by default and detail on request.
- Decision cards have a clear recommendation, two or three plausible choices, financial effect, uncertainty, deadline, and the executive accountable.
- The primary action advances to the next material decision; optional week/month advance must stop before unresolved owner commitments or critical deadlines.
- Dismissed informational notices stay in a history. Unresolved required decisions cannot silently disappear.
- Allow editable plans before commitment. Purchases, sales, loans, dismissals, and projects require a final concise terms summary; routine navigation does not.
- A decision history records the selected action, original forecast, and later observed outcome. It must not assert unobserved alternatives as facts.
- Read-only detail is progressively available, but the game is fully playable from recommendations and summary screens.
- Guidance can be expanded or collapsed during a career independently of the startup difficulty, following G12. Help preferences must not remove critical warnings or alter the financial and sporting rules.
- Keyboard navigation, visible focus, remappable essential shortcuts, non-color status cues, text scaling, motion reduction, and independent music/effects controls are required.
- Initial desktop layout targets 1280×720 and higher, tested at 100%, 125%, and 150% text settings. Content may reflow or scroll; no essential controls or financial terms may clip.
- No flashing goal sequences by default. Critical financial warnings must remain comprehensible without sound or color.

### H4. Visual and audio direction

Use a readable club boardroom identity: strong typography, a restrained financial palette, club colors as accents, legible crests, stadium silhouettes, and selective player/staff portraits. Avoid a wall of red/green numbers.

Minimum art brief: 48 simple distinct crests and club color sets; six polished starting-club identity cards; reusable portrait components for generated people; four facility illustrations with visible upgrade states; 12 event-family illustrations/icons; 12 achievement icons; complete Steam capsule/library assets. Variants reuse a coherent visual system rather than demanding bespoke art per event.

Sound provides short crowd reactions, restrained decision feedback, and an unobtrusive music set. No voice acting, licensed chants, commentary performances, or motion-captured content. Major moments—promotion, a beloved player's sale, opening a stand, and accepting an exit—receive a distinct but skippable presentation.

### H5. Owner brief, decision memory, and session rhythm

**Open questions (EXP-02):** surface up to three important questions drawn from the current simulation in the Owner Desk, alongside the existing metrics and decision queue. They are optional points of attention, not a task quota or a new workspace.

| Horizon | Illustrative question | Existing source of the question |
|---|---|---|
| Near: next few in-game weeks | Will recruitment find a replacement within our budget? | Sporting director mandate and pending proposals |
| Medium: this season | Can the promotion push succeed while we protect our reserve? | Sporting ambition, competition, and cash forecast |
| Long: several seasons | Can the academy and hospitality fund a stable top-tier club? | Investment plan, pipeline, and owner history |

Questions can resolve without intervention. Empty slots are allowed; no fabricated offer, progress, or emergency may fill them. Quiet weeks remain skippable.

**Decision memory and reviews (EXP-01, EXP-03, EXP-04):** monthly and season reviews must connect important developments to the relevant recorded decisions and selected people. Show:

1. What the owner intended.
2. What the club committed and who was responsible for execution.
3. What happened compared with the original forecast.
4. Which explanations are supported and which remain uncertain.
5. What actions are available now.

The original forecast remains distinguishable from later revisions. Recognize prudent restraint: rejecting an unaffordable contract, retaining a capable manager during noise, or keeping a reserve through relegation. A poor sporting outcome does not automatically mean the financial judgment was unsound. An executive recommendation must explain its priorities rather than imply an always-correct answer.

Use a few memorable details—a graduate's debut, a manager's tenure, a completed stand, a supporter's stated grievance—to make the club's progress personal. A name or portrait alone does not establish attachment; M1 and M4 test whether players actually care. G11 specifies the illustrative academy story and protects uncertain outcomes.

**Session rhythm (EXP-05):** orientation → meaningful choice → anticipation → outcome → reflection. An illustrative 30-minute session can begin with a resume brief, include a material transfer proposal and several weeks of results, and finish at a monthly review. This is a pacing example, not a forced timer or guaranteed incident schedule.

Group linked information: an injury, the replacement proposal, and its cash effect should be understandable as one situation. Outside onboarding, aim to make one or two issues visually dominant while keeping every material deadline accessible. If several genuine crises coincide, prioritize and explain them; do not hide them or change their dates to enforce that presentation target.

Use short, skippable celebrations and sound selectively for meaningful milestones; ordinary wins do not require modal rewards. At a natural stopping point, summarize what changed, what is already decided, and what awaits. On return, the brief must restore context in under one minute. Deadline clocks advance only with simulation time; there are no expiring real-world rewards, daily streaks, absence penalties, or manufactured near misses.

**Acceptance:** the player can recall a consequential choice, identify relevant evidence, name something accomplished, and understand what awaits. History and pending questions remain correct after saving and loading. Skipping presentation or collapsing help does not lose critical information.

## I. Game Progression

### I1. Career stages

| Stage | Dominant question | Changing constraint |
|---|---|---|
| Acquisition / year 1 | What did I buy, and what must be fixed first? | Limited reserve, inherited contracts, incomplete future knowledge |
| Stabilization / years 1–3 | Can this club fund its present commitments? | Leadership fit, cash timing, roster depth |
| Growth / years 3–6 | Which advantage can compound? | Development delays, promotion costs, capacity, renewal pressure |
| Consolidation / years 6–10 | Can success become durable? | Aging talent, richer rivals, wage demands, diminishing returns |
| Legacy / after year 10 | Keep the institution, realize a return, or rebuild? | Succession, saturation, reinvestment, attractive sale offers |

These are typical arcs, not scripted unlock tiers. A struggling club can remain in stabilization; a fortunate club can accelerate. All core owner systems are available from the start when resources and conditions permit.

At the ten-season checkpoint, provide an assessment and the choice to continue or retire. Selling requires an actual buyer. Retiring without selling reports unrealized equity separately; it does not pretend an appraisal was paid in cash. At season 50, conclude with a final sale review or retirement report.

### I2. Six starting scenarios

Names are placeholders; each scenario needs authored identities and its own disclosed financial baseline.

| Scenario | Tier | Opening tension | Illustrative success objective |
|---|---|---|---|
| Community Foundation | 3 | Strong loyalty, small ground, limited personal reserve | Reach Tier 2 sustainably while preserving affordable access |
| Debt-Burdened Name | 3 | Recognizable club with inherited debt and expensive veterans | Restore positive operating cash and regain a higher tier |
| Academy Opportunity | 2 | Good pipeline, weak immediate squad, impatient supporters | Build a competitive side with material academy contribution |
| Promotion Gamble | 2 | Strong core nearing expiry, narrow financial cushion | Earn promotion while avoiding a debt-led collapse |
| Established Underdog | 1 | Stable club, limited demand, richer rivals | Improve cash generation and achieve sustained sporting overperformance |
| Expensive Decline | 1 | High revenues, aging squad, large wages, skeptical supporters | Rebuild the roster and grow owner value without insolvency |

Scenario objectives specify time horizon, baseline, and measurable thresholds during balancing. They use common mechanics, not bespoke scripts. A new seed varies people, potential, market activity, and match outcomes while retaining the scenario's intended economic challenge.

### I3. Replayable strategies

- **Youth-first:** patience and recurring academy funding in return for uncertain future contribution and sale opportunities.
- **Player trading:** recruitment quality, contract timing, and willingness to sell; turnover costs continuity and requires actual buyers.
- **Commercial growth:** use existing demand, pricing, sponsors, and hospitality to fund football; saturation and supporter sensitivity constrain returns.
- **Promotion push:** commit to current ability and wages with a credible downside plan; promotion is not assured.
- **Long-term dynasty:** retain talent, develop replacements, and maintain leadership through slower financial realization.
- **Debt-backed turnaround:** use club borrowing for a disclosed investment plan. This is available within credit limits; an acquisition funded by personal leverage is deferred.

These strategies can be blended and changed at a cost. Strategy presets change staff priorities and budget suggestions; they do not grant exclusive percentage buffs. Different countries and holding-company strategies are later expansions, not claimed V1 replay content.

Long-term variety comes from aging, expiring commitments, scarce recruitment opportunities, executive succession, rival improvement, and club-specific demand. Use stable nominal economic baselines with bounded growth in V1; there is no macroeconomic inflation simulator. When growth saturates, retaining cash and distributing profits are valid decisions. The game must not require perpetual expansion to feel successful.

### I4. Challenge progression and voluntary ambition

**Target experience (EXP-06):** “I understand the problem. I have a plausible plan. I need to think to make it work.” The progression is **learn → practice → combine → stretch → consolidate**.

| Club situation | Main ownership problem | Skill practiced | Additional demand |
|---|---|---|---|
| Guided acquisition | Fund one priority while covering known obligations | Read cash and recognize opportunity cost | Benefits arrive at different times |
| Stable club | Choose current squad strength or future earning capacity | Compare horizons and downside | Money, staff fit, and timing interact |
| Promotion contender | Decide how much to commit before promotion is certain | Assess risk and preserve a fallback | Future revenue depends on sporting results |
| Newly promoted club | Improve enough to compete without unaffordable commitments | Coordinate recruitment, wages, and liquidity | Higher revenue accompanies stronger rivals and larger demands |
| Established successful club | Replace aging talent and retain leadership | Sequence commitments and sustain an advantage | Decisions interact across several seasons |

These are typical circumstances, not mandatory unlock stages or a scripted difficulty curve. Strong players can begin with harder existing scenarios. Consolidation is valid, and a solved problem can remain solved until circumstances genuinely change. More paperwork, more statistics, and more approvals do not count as greater strategic depth.

Player skill is multidimensional: understanding cash does not imply equal skill at hiring or judging development. Use G12's selectable assistance to address individual learning needs. Do not measure one hidden global skill rating or force a numerical challenge-to-skill ratio.

**Choosing a stretch (EXP-08):** at the existing preseason strategy review, present financially plausible plans using the following ambition language. Midseason revisions remain subject to the existing review and commitment costs.

| Ambition | Plan intent | Relationship to the existing five strategy mandates |
|---|---|---|
| Consolidate | Protect reserves and sustain the club's current position | Often expressed through balanced growth or commercial resilience |
| Build | Improve squad or infrastructure over a longer horizon | Can emphasize talent development, player trading, or balanced growth |
| Push | Commit more available resources toward a demanding sporting objective | Usually a promotion push, or a higher target within the current tier |

Ambition describes alternative capital plans; it is not a sixth strategy, an independent difficulty modifier, or a mandatory matrix of 15 presets. Selecting a plan updates the existing strategy, budgets, and expectations. Offer only plausible alternatives, show their commitments and downside forecast, and preserve the option of a slower path. There are no ambition bonuses or new game-over rules.

Choosing Consolidate does not create an artificial penalty for insufficient ambition. Existing supporters and executives can still react to their established identity, agreed mandates, and actual results. The player sees those plausible consequences rather than receiving a universal punishment for playing cautiously.

Experts can seek a harder acquisition, pursue an existing youth-first strategy, or attempt sporting progress within a smaller wage envelope. These use current systems. Strong performance never triggers hidden rival subsidies, biased matches, automatic injuries, or manufactured crises. A mature successful club can remain enjoyable or conclude through a voluntary exit.

**Acceptance:** novices can learn the relevant tradeoff; experienced players can choose a more demanding plan; both retain the same world rules and known obligations. Failure rates and match win percentages are not targets for automatic adjustment. M4 defines the challenge playtests.

## J. Steam Launch Requirements

### J1. Release model and price

**Recommend a full 1.0 launch after closed playtests and a public demo.** A small team already faces substantial economy and save-support work. A prolonged paid Early Access campaign adds public promises and support obligations before the core experience has been proven.

Early Access is a fallback only if the complete ownership loop is already enjoyable and funded through completion, with remaining work largely balance, content, and usability. It is not a way to sell an incomplete concept to finance its essential systems. This is consistent with Valve's stated purpose for Early Access. [Steamworks Early Access guidance](https://partner.steamgames.com/doc/store/earlyaccess)

**Price hypothesis:** US$19.99 at full release; consider US$24.99 only after external players consistently regard the replayable package as worth that amount. Evaluate localized pricing separately, close to launch. A 10% introductory discount is a commercial option subject to the applicable Steam setup and rules, not a requirement.

The recommended price is a positioning judgment, not a sales forecast. Paid football management and business simulations provide reference points, but their scope, age, discounts, and audiences differ. Recheck comparable store prices before locking the offer. No editions, paid currency, subscriptions, or day-one DLC are needed.

### J2. Demo

Ship a **one-season demo of the recommended acquisition**, using the actual ownership loop. Target 60–90 minutes for a new player; no wall-clock cutoff. Include purchase, budgets, delegation, material recruitment, league/cup consequences, finances, supporters, a project choice, and the season review. Mark the endpoint clearly before starting.

The demo must show delayed-investment progress and the next season's opportunity, while explaining that long-horizon projects may not mature during the demo. Do not fake immediate academy payback for marketing. It supports local saves and replay. Target continuation of the final public demo save into 1.0; any earlier testing-build compatibility limitation must be disclosed before play.

At the end, show the player's own decision story and a clear Steam wishlist/store action. Do not require email registration to play or to recover a save. Leave the demo available after the festival if ongoing maintenance is feasible.

### J3. Wishlists and Steam Next Fest

Use one main call to action: **wishlist the game on Steam**. An optional email list and Steam community updates support retention, but do not interrupt the store conversion path.

Recommended sequence:

1. Validate the loop in private before investing heavily in trailers and public promises.
2. Publish a Coming Soon page when the team can show representative play: a funding decision, delegation, football consequence, and business review.
3. Release short examples of real tradeoffs and season stories. Target football club-building creators and management/tycoon creators with separate demonstrations of the same game.
4. Use closed playtests to repair onboarding and pacing, then publish the stable demo ahead of the chosen festival where practical.
5. Participate in one suitably timed Steam Next Fest when the demo can make a convincing impression and the team can respond to feedback.
6. Launch after acting on material findings; use milestones instead of an invented release date.

Valve currently limits a title to one Next Fest and requires an unreleased base game, public store page, and playable demo. An Early Access release before the event would remove eligibility. Its general workback guidance calls for demo review submission four weeks ahead for Press Preview or two weeks ahead for the festival start; verify the selected edition's deadlines. [Steam Next Fest documentation](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest)

Track wishlists by campaign timing and available referral reporting, demo starts/completion, and qualitative fit. Do not promise a universal wishlist threshold or conversion rate. Useful creator footage should show the consequences of one season, not a generic montage of dashboards.

### J4. Platform and commercial release checklist

| Requirement | V1 position |
|---|---|
| Operating system | Windows PC; publish minimum specifications only after measured testing |
| Offline play | Career creation, play, manual save/load, and continuation work without a live network after installation/setup |
| Steam Cloud | Must Have: continue between PCs, preserve local saves when offline, and recover safely from conflicting histories |
| Achievements | 12: acquisition, first promotion, top-tier survival, cup/title, academy contribution, sustainable trading, positive operating cash, debt repayment, completed investment, supporter recovery, positive realized exit, ten-season legacy |
| Controller | Full controller navigation deferred; store claims match tested support |
| Steam Deck | Test via Proton and trackpad controls if hardware is available; prioritize text legibility and usable scaling. Do not promise Verified status or a Deck-specific launch interface |
| Localization | Complete edited English at launch. German is the first additional-language candidate, then Spanish and Brazilian Portuguese, subject to demonstrated demand and funded professional review |
| Modding | No official V1 tools, Workshop, or mod compatibility guarantee; reassess after core stability |
| Store materials | Actual gameplay trailer, representative screenshots, clear feature boundaries, capsule/library assets, accurate languages and hardware claims |
| Customer support | Public known-issues area, reproducible bug-report instructions, owned support contact, patch notes, and assigned launch coverage |
| Content/rights | Document the rights to shipped names, artwork, music, fonts, and other assets; use the platform's current content disclosure process |

Cloud behavior is a product requirement chosen for this game; Steam provides the capability. Test the actual user experience rather than treating enabled cloud storage as proof that progress is safe. [Steam Cloud documentation](https://partner.steamgames.com/doc/features/cloud)

Valve's Deck guidance includes controller usability, legibility, and controller-friendly text input. A keyboard/mouse-first V1 should therefore make modest, tested claims about handheld usability. [Valve compatibility overview](https://www.steamdeck.com/en/verified?stream=top)

Localization order is a demand hypothesis, not established audience data. Financial wording, conditional text, currency formatting, long names, and text expansion must remain understandable. Add a language only when translated and tested throughout the product, including the tutorial and store page.

Valve requires separate store and build review and approval. Its release process requires the Coming Soon page to have been public for at least two weeks. For a developer's first few titles, Valve also states a 30-day waiting period between paying the app fee and release. Budget review time and rechecks rather than scheduling launch at the minimum. [Release process](https://partner.steamgames.com/doc/store/releasing), [Steamworks onboarding](https://partner.steamgames.com/doc/gettingstarted/onboarding)

### J5. Save expectations

- Multiple careers and clearly named manual saves; at least three rotating autosave recovery points per career.
- Save at every completed weekly advance and before a major owner commitment; show a clear saving/completion state.
- Preserve the last good save if saving fails. Explain how to retry or choose another slot without losing the running session.
- Loading must retain pending offers, contract obligations, accepted projects, deadlines, supporter history, personal funds, and simulation outcomes already resolved.
- Reloading an unchanged pre-advance save should not reroll the same upcoming simulation merely because the player loaded it.
- Patch compatibility within the 1.x release series is a product commitment. Any exceptional incompatibility requires a preservation path and clear advance communication, not silent destruction of careers.
- Cloud conflicts expose timestamp, club, season, and progress for both versions. Preserve both histories when the player is unsure; never silently replace a newer career with an older one.
- Standard play has no account requirement beyond the distribution platform. No online-only economy or anti-cheat requirements.

### J6. Community feedback loop

Before release, run focused cohorts with weekly triage: confusing decision, boring interval, unclear financial cause, exploit, or missing content. Publish concise summaries of what changed and why. Use optional feedback and aggregate diagnostics; core offline play remains available without telemetry.

For launch, assign a person to support and reserve at least four weeks for fixes. Prioritize lost progress and progression blockers above balance complaints and requested additions. Review balance monthly initially; explain meaningful rule changes and their effect on existing careers. No fixed expansion promises before retention and support load are understood.

## K. Feature Priority Matrix

“Must Have” defines the commercial baseline. “Should Have” and “Could Have” do not become implicit promises. “Later Expansion” means a separate product decision and budget. “Do Not Build” protects the identity of the game.

| System / feature | Priority | Boundary or rationale |
|---|---|---|
| Acquisition, finite personal capital, club/owner cash separation | Must Have | The player genuinely owns an investment |
| Annual capital plan and cash forecasts | Must Have | Core opportunity-cost gameplay |
| Three leaders, hiring/contracts, mandates, routine autonomy | Must Have | Creates accountable delegation |
| Three-tier world, 48 clubs, cup, promotion/relegation | Must Have | Economic and sporting progression |
| Named players, aggregate matches, development, injury, aging | Must Have | Ownership choices need persistent sporting consequences |
| Delegated transfers, wages, contract expiries, major exceptions | Must Have | Financial and sporting tension |
| Recurring revenues/costs, fixed loans, refinancing restrictions | Must Have | Business survival and investment decisions |
| Owner injections/distributions, valuation range, buyer offers, exit | Must Have | Closes the capital-allocation loop |
| Four project categories and recurring development funding | Must Have | Alternatives to spending on current players |
| Supporter approval, identity, sponsor and ticket policies | Must Have | Club attachment and commercial consequences |
| Fictional affordability rule, crisis/recovery, forced-sale ending | Must Have | Predictable constraints and meaningful failure |
| 12 event families, 48 templates, six linked arcs | Must Have | Legible consequences and story coverage |
| Six acquisition scenarios, three difficulty settings | Must Have | Replay breadth using shared systems |
| Guided introduction, six workspaces, accessibility basics | Must Have | Makes the game usable and coherent |
| Season reviews, owner history, ten-season assessment, 50-season support | Must Have | Accountability and closure |
| Player experience EXP-01–EXP-08: meaningful callbacks, open questions, evidence-based reviews, attachment, closure, challenge progression, guidance, and ambition | Must Have | Refinements of existing reports, histories, scenarios, and strategy; details in D, G12, H5, I4 |
| Save reliability, Steam Cloud, offline play, performance | Must Have | Commercial trust |
| Public demo, Steam materials, 12 achievements, English, support plan | Must Have | Complete PC launch package |
| Additional contextual writing beyond the minimum | Should Have | Reduces repetition after the core loop is proven |
| Additional illustrated moments and stronger audio variation | Should Have | Improves emotional reward |
| German localization | Should Have | Only with demand and complete QA coverage |
| Search across history and saved notes on executives/players | Could Have | Useful convenience; does not deepen core rules |
| Native controller interface / verified handheld target | Later Expansion | Requires dedicated interaction testing and design |
| More countries, competitions, and separate women's pyramids | Later Expansion | New economic balancing and content |
| CEO/CFO/commercial/academy roles split into larger executive team | Later Expansion | Only if delegation remains understandable |
| Equity investors, dilution, boards, leveraged acquisitions | Later Expansion | New ownership and financing rules |
| Serial acquisitions and football holding companies | Later Expansion | New progression and portfolio economics |
| Transfer installments, player loans, advanced contract clauses | Later Expansion | Additional obligation and market complexity |
| Medical/scouting buildings and expanded commercial projects | Later Expansion | Add only after current investments are distinct |
| Club editor, data packs, supported mods, Workshop | Later Expansion | Tools, compatibility, and support commitment |
| Multiplayer | Later Expansion | Reconsider as a distinct funded initiative, not a patch promise |
| Real-world licenses | Later Expansion | Evaluate only with a clear commercial and content-maintenance case |
| Owner-controlled tactics, formations, lineups, training, set pieces | Do Not Build | Changes the player's job |
| Touchline coaching, substitutions, tactical match interaction | Do Not Build | Undermines delegated ownership |
| Detailed spatial/3D match engine for V1's ownership feedback | Do Not Build | Disproportionate cost for the product need |
| Manual scouting of hundreds of players or full contract micromanagement | Do Not Build | Makes the player a football operations clerk |
| Accounting ledgers, tax optimization, audited-statement simulator | Do Not Build | Financial detail without the intended decision value |
| Always-online career, daily chores, premium currency, loot boxes | Do Not Build | Conflicts with the paid offline tycoon experience |
| Hidden skill profiling, adaptive match/economy manipulation, forced losses, real-world retention deadlines | Do Not Build | Conflicts with fair challenge, controllable pacing, and voluntary return |

### Development sequence and scope response

1. **Prove decisions:** acquisition brief, two competing allocations, delegation, uncertain results, clear review; test challenge comprehension, a remembered consequence, and satisfying closure alongside initial architectural planning.
2. **Prove a complete career:** economy, competitions, transfers, aging, projects, valuation, and a sale ending at minimal presentation quality.
3. **Prove replay:** three distinct strategies, six scenarios, supporter consequences, and event arcs.
4. **Make it commercial:** onboarding, art/audio, reliable saves/cloud, long-run balance, performance, demo, Steam assets.

Indicative allocation inside the planning envelope: 4–6 weeks for product validation, roughly 6–8 months for core systems and content, 3–4 months for integration and external testing, and 3–4 months for stabilization, demo, and release work, with overlap where appropriate. Re-estimate after validation; these are planning allowances, not independently additive promises.

If capacity is insufficient, first remove optional languages and extra presentation/content beyond the minimum. Reducing a Must Have requires a revised PRD and renewed experience validation. Never silently compensate by sacrificing saves, causality, or delegation.

## L. Risks and Mitigations

| Risk | Early signal | Mitigation / product response |
|---|---|---|
| Too spreadsheet-like | Players browse figures but cannot name a dilemma or person they care about | Tie reports to named decisions and people; reduce dashboard prominence; test decision cards before adding charts |
| Too complicated | Players confuse budget, cash, debt, and owner funds | Cash-first language, before/after forecasts, short contextual glossary, progressive detail |
| Too shallow / passive | Players always follow recommendations and fast-forward without thought | Introduce credible competing investments, uncertainty, and commitment; improve choice structure before adding content |
| Feels like reduced Football Manager | Players mainly ask where the tactics controls are | Demonstrate the ownership decision immediately; make capital, delegation, and exit the trailer and tutorial center |
| Weak differentiation from chairman games | Players cannot explain why they would buy this on PC | Validate capital forecasts, mandates, and realizable sale value against the closest substitutes |
| Long-run economy instability | Universal debt crises, exploding wages, or unbounded cash accumulation | Bounded markets and demand, affordable rival commitments, meaningful distributions, repeated multi-decade balance reviews |
| Poor rival decisions | Easy repeated resale profits or rivals cannot field credible squads | Same affordability rules, roster needs, contract planning, distress consequences; adversarial economy testing |
| Delegation feels arbitrary | Staff spend unexpectedly or fail without explanation | Visible authority limits, materiality rules, receipts, and evidence-based staff reviews |
| Lack of emotional connection | Players remember ratings but no names or club traits | Persistent people, histories, rivalries, identity-sensitive fan reactions, and selective illustrated moments |
| Repetitive decisions | Identical prompts recur every month | Conditional eligibility, cooldowns, story arcs, report batching; do not force event quotas |
| Football outcomes feel unfair | Every defeat prompts an accusation of randomness | Show expectations and availability; explain distributions across time; never claim deterministic success |
| One dominant strategy | Youth, trading, or borrowing wins regardless of club state | Vary demand, horizons, labor scarcity, liquidity, and initial contracts; test policies across all scenarios |
| Valuation is exploitable or meaningless | Loans inflate return or appraisal can always be sold instantly | Cash/debt bridge, separate invested capital, alternative valuation lenses, real buyer constraints |
| Rich clubs become trivial | Mature saves contain no choices except upgrading | Saturation, aging, retention pressure, rival investment, distributions and optional exit; accept successful closure |
| Challenge misses the player's skill | Novices cannot explain decisions or experts find every alternative dominated | Teach one concept at a time, offer selectable explanations and ambition, compare novice/expert responses; use M4 |
| Engagement becomes pressure without enjoyment | Longer sessions accompany frustration or difficulty stopping | Natural closure, game-time deadlines, voluntary return and time-well-spent feedback; do not reward absence-sensitive play |
| Neuroscience analogies become unsupported product claims | Features are described as reliably triggering a chemical or fixed flow state | Preserve B's distinction between evidence and design hypotheses; assess experience, not inferred neurotransmitters |
| Supporters become a nuisance meter | Reactions seem unrelated or every growth choice is punished | Show identity drivers, bounded impacts, plausible compromise and recovery; avoid arbitrary match penalties |
| Fictional world lacks appeal | Players reject clubs before playing | Strong curated starting identities; test naming and art early; do not assume licensing fixes weak attachment |
| Small world feels closed | Market dries up or promotion ceiling removes ambition | Ensure viable player lifecycle, distinct rival trajectories, domestic cup, financial/legacy goals; disclose world boundaries |
| Save failure destroys trust | Lost progress, missed deadlines after loading, conflicting cloud copies | Treat preservation and recovery as release blockers; test interruption and multi-device scenarios |
| Excessive scope | New role/country/financing ideas displace baseline work | Fixed quantities, priority matrix, explicit feature replacement rule, milestone re-estimation |
| Steam audience mismatch | Wishlists grow but demo feedback expects coaching | Accurate store claims, targeted cohorts, representative gameplay footage, monitor mismatch complaints |
| No money to finish or support | Schedule relies on speculative launch/EA sales | Confirm funded runway after prototype, preserve support reserve, reduce scope or pause commitment |

## M. Success Metrics

All thresholds below are **proposed acceptance targets**, not measured results, market benchmarks, sales predictions, or proof of statistical certainty. Diagnose failed targets rather than optimizing numbers at the expense of the experience.

### M1. Player-value validation

| Metric | Target / method | Decision it informs |
|---|---|---|
| Ownership comprehension | At least 10 of 12 prototype participants correctly identify three owner decisions and two delegated responsibilities after the opening | Is the fantasy clear? |
| Cash comprehension | At least 9 of 12 explain club cash versus personal reserve and a future wage obligation without prompting | Is the financial model usable? |
| Meaningful first choice | At least 9 of 12 can name their first sacrifice and why they made it; at least two options receive reasoned selections across the cohort | Is the core dilemma real? |
| Causal learning | At least 9 of 12 explain one plausible connection between a decision and later results while acknowledging uncertainty | Do reviews teach rather than merely report? |
| Opening pace | Median first material allocation within 10 minutes; 80% of a later 30-person test cohort completes the guided opening within 30 minutes without facilitator rescue | Can players start independently? |
| Continued interest | At least 60% of 30 qualified external testers voluntarily start a second season during a seven-day test window | Is there enough motivation after the first payoff? |
| Distinct strategies | At least three strategies produce explainable successful careers in balanced multi-scenario testing; none wins regardless of the opening situation | Is replay based on decisions? |
| Attachment | At least 70% of the later test cohort can name a person or club trait that affected a decision | Does football create emotional stakes? |
| Season pace | Median 45–75 minutes for experienced testers outside onboarding; investigate repeated intervals with neither action nor useful new information | Is delegation saving attention? |

Recruit the first 12 as eight football/club-building players and four broader tycoon players, including people unfamiliar with this project. Small cohorts supply directional product evidence; repeat testing after substantial changes.

In the later 30-person cohort, repeat the comprehension checks on the release candidate: at least **25 of 30** must identify the owner and delegated responsibilities; at least **23 of 30** must explain cash versus personal reserve and future wages; at least **23 of 30** must explain a plausible decision-to-result connection without treating uncertainty as a guarantee. Test these without facilitator coaching. These thresholds, together with the opening-pace target above, define the release onboarding gate.

### M2. Version 1 release gates

| Gate | Required evidence before shipping |
|---|---|
| Gameplay completeness | All Must Haves in K work together; every scenario supports acquisition, planning, staff autonomy, results, investment, recovery, annual review, and an ending |
| World/simulation stability | At least 100 varied world runs complete 50 seasons without blocked calendars, invalid competition membership, lost contracts, impossible player states, or runaway population outside the supported lifecycle policy |
| Economy coherence | Transfers reconcile between clubs; borrowing/injections are never revenue; all loan obligations resolve; no negative prices or unlimited financing; global cash sources/sinks are explainable |
| Economy balance | Review results across six scenarios and all difficulties, including at least 20 seeds per tested strategy/scenario on Standard; no reliably dominant loop of asset flips, borrowing, or cash distributions |
| Long-run health | At years 10, 25, and 50, document wage/revenue, cash, debt, transfer-volume, insolvency, and concentration distributions; no unexplained exponential inflation or collapse; promotion remains possible for well-run smaller clubs |
| Football credibility | Controlled comparisons show higher expected results for stronger otherwise-comparable squads, realistic possibility of upsets, and meaningful effects from depth, age, and injuries; final score/stat/text consistency holds |
| Save reliability | At least 10,000 varied save/load checks plus targeted interrupted-save, low-storage, offline, patch-compatibility, and conflicting-cloud cases; no known reproducible lost-progress bug; last good save remains recoverable |
| UX | All critical owner flows complete with keyboard/mouse; no clipped essential information at supported sizes/scales; player can inspect why a proposal is blocked and what would make it affordable |
| Player-experience behavior | EXP-01–EXP-08 are demonstrated in their existing workspaces: callbacks use actual history/original forecasts; questions reflect real state; reviews include evidence and next actions; closure/resume works; guidance and ambition respect G12/I4. Verify after save/load and with presentation skipped. |
| Challenge fairness and attention | Same situation and owner decisions resolve under the same world rules across help presentations; no hidden performance-based adjustments. All critical facts and deadlines remain available when guidance is collapsed or notices grouped. Review novice/expert and satisfaction evidence under M4 and resolve critical comprehension or control failures. |
| Onboarding | Meets M1's later-cohort opening/comprehension targets; no mandatory facilitator explanation or tutorial softlock |
| Human endurance | At least 20 independent testers complete three seasons; at least five complete ten seasons and provide decision/history feedback; cover all six scenarios across the cohort |
| Replayability | Each curated scenario has a distinct tested financial tension; three viable strategies have observed examples and explainable failure cases |
| Performance | On the designated baseline PC, normal navigation responds within 100 ms at the 95th percentile; weekly advance completes within 2 s at the 95th percentile; season rollover ≤10 s; save/load ≤5 s, including late-career saves |
| Baseline test hardware | Initial target: a four-core Windows 11 PC, 8 GB RAM, SSD, integrated graphics, at 1280×720. Name an actual test device and verify before publishing specifications; these are targets, not compatibility claims |
| Bug tolerance | Zero known release-blocking crashes, save corruption, progression dead ends, incorrect contractual deductions, or reproducible unlimited-money exploits; cosmetic defects only if bounded, documented, and not obscuring decisions |
| Content minimum | 48 identified clubs, six finished acquisition scenarios, three staff roles, four investment categories × three upgrade steps, 48 contextual templates in 12 families, six linked arcs, full introduction, 12 achievements |
| Release operations | Store/build approvals, accurate claims, working demo, tested cloud behavior, complete English, support owner, rollback/recovery process, and funded launch support |

A test run is evidence, not proof of all future saves. Investigate outliers and explicitly review severe low-frequency failures. The technical team will decide how to obtain this evidence in the TRD; this PRD specifies the observable outcome.

### M3. Launch and post-launch health

- **Primary product signal:** players completing a season voluntarily begin another and can describe a consequential ownership choice.
- **Experience diagnostics:** first-session completion, season-two starts, three-season continuation, return visits, help usage, abandoned decision types, and recurring confusion in support reports.
- **Reliability:** crash-free sessions, failed saves, cloud conflicts, time to resolve severe issues. Target at least 99.5% crash-free reported play sessions in the final external test; zero known loss-of-progress issues remains the stronger gate.
- **Commercial signals:** qualified wishlists, demo engagement, price feedback, purchases, refunds and their stated reasons, review sentiment about depth and clarity. Judge these together, not via a single wishlist number.
- **Support signal:** issues caused by missing tactical features indicate positioning failure; repeated cash confusion indicates UX failure; requests for additional club scenarios after completed careers indicate promising content demand.
- **Project break-even planning:** units required = (total development cost, including costs already incurred + launch cost + support cost) ÷ expected net receipts per paid unit. Track funding needed to finish separately as remaining unpaid development, launch, and support commitments less available funding. Net receipts must use actual platform terms, regional prices, discounts, refunds, and taxes. Populate only after a budget exists; do not invent unit forecasts now.

Use voluntary test-session observation and available aggregate reporting. Any additional gameplay diagnostics must be disclosed and optional; report cohort bias and missing observations. Do not infer whole-audience retention from telemetry participants alone.

### M4. Enjoyment and challenge validation

Research on harmonious and obsessive gaming engagement supports examining whether people feel willing and satisfied or pressured and conflicted. Time spent alone is insufficient evidence of enjoyment. This motivates our evaluation approach, not a promise that the game's design will improve well-being. [Przybylski, Weinstein, Ryan, and Rigby, 2009](https://selfdeterminationtheory.org/SDT/documents/PrzybylskiWeinsteinRyan%26Rigby2009_CPB.pdf)

Add the following prompts at natural review points in the existing M1 cohorts, distributing them across sessions to avoid interrupting play with repeated questionnaires:

| Prompt | What it investigates |
|---|---|
| What are you looking forward to finding out? | Anticipation grounded in an actual plan |
| Which decision made a difference, and what evidence supports that? | Agency and understandable consequences |
| What would you do differently next season? | Learning and an actionable response to setbacks |
| Which person or club moment do you remember? | Attachment |
| Did this session feel like time well spent? | Satisfaction |
| Was it easy to stop when you wanted? | Control over play and useful closure |
| Was this decision too easy, engaging, or overwhelming? | Perceived challenge |
| Did you understand the tradeoff? | Clarity versus strategic difficulty |
| Could you identify a sensible next action? | Capability and recovery |

These are exploratory qualitative prompts, not a validated psychological scale or a measure of neurotransmitters. Do not coach answers. Compare responses with observed choices, voluntary return, help use, and specific points of boredom or confusion. Preserve M1's stated numerical gates; this section does not invent a retention-uplift claim or a universal optimal challenge ratio.

Compare novice and experienced participants on the same ownership situation. Look for reasoned choices, awareness of a downside, and the ability to revise a plan after evidence arrives. A slow decision may reflect enjoyable thought, confusion, or distraction; a fast decision may reflect mastery or disengagement. A losing streak is not proof of weak ownership.

| Finding | Product response |
|---|---|
| Novices cannot explain the situation or financial terms | Improve framing and accessible guidance; retest the relevant comprehension gate |
| Both groups consistently identify the same option as obviously superior in varied circumstances | Rebalance the alternatives and opportunity costs |
| Player understands a setback but lacks sufficient resources to recover | Assess whether this follows the disclosed commitments and G12; do not automatically rescue the club |
| Player cannot locate a critical deadline or skips essential information accidentally | Treat as a critical comprehension/control failure and repair before release |
| Experts have mastered a scenario and still enjoy it | Allow their success; offer voluntary ambitions or a different starting scenario |
| Longer play accompanies frustration or difficulty stopping | Improve closure and attention management; do not call the extra time an engagement success |

One bounded presentation experiment can compare a financial season summary with a summary that also recalls relevant earlier decisions and named people. Keep outcome information equally accurate, balance presentation order, and assess understanding, recall, and enjoyment. A small cohort supplies exploratory evidence; it cannot establish a causal retention uplift.

Look for convergence: players understand their choices, remember their club, finish satisfied, and voluntarily return. Do not force a percentage of owner decisions to fail or a fixed match win rate. Competent ownership may accept lower sporting odds to preserve the club's health.

## N. Explicit Non-Goals

V1 does not aim to reproduce the entire football industry, the complete financial statements of a real club, or a global football database.

It will not include:

- Owner control of tactics, formations, lineups, substitutions, training schedules, set pieces, scouting assignments, or matchday talks.
- A full spatial match engine, controllable highlights, stadium exploration, or football action gameplay.
- Multi-club portfolios, serial takeovers within one career, joint ownership, outside equity investors, board voting, hostile shareholder contests, or leveraged acquisition finance.
- Real teams, players, logos, competitions, sponsors, or promised licensing deals.
- Continental tournaments, international management, multiple countries, youth/reserve league management, or a fourth-tier feeder world.
- Advanced accounting, tax planning, financial derivatives, public stock markets, transfer installments, player loans, or agent negotiation systems.
- Club relocation, create-a-club, a supported data editor, Workshop, multiplayer, or an always-online world.
- Mandatory controller/Steam Deck verification, native console releases, or a broad set of launch languages.
- Unlimited career duration, a guaranteed 100-hour content claim, exact sales forecasts, or post-launch expansion promises.
- Hidden behavioral profiling, performance-triggered changes to match odds or rival resources, a fixed failure quota, or neuroscience-based claims about reliably inducing specific chemicals.
- Daily streaks, absence penalties, real-world expiring rewards, manufactured near misses, or artificial crises used to prolong sessions.

## O. Open Questions and Handoff Assumptions

**Architectural planning may proceed using this consolidated baseline.** The following questions identify evidence still needed; they are not a claim that validation has happened and do not require a new round of upfront user questions. The receiving team should record each default, identify what a failed assumption would change, and include validation work in the proposed delivery plan. Revisit affected commitments when evidence contradicts a default.

| Question | Recommended working answer | How to resolve / responsible discipline |
|---|---|---|
| 1. Is delegation plus capital allocation enjoyable without tactical interaction? | Yes is the central hypothesis; it is not yet proven | Product designer runs the 12-person decision prototype alongside early architectural planning. If it fails, revise the loop and affected design before committing to full implementation. |
| 2. Does one-club ownership with sale ending the save satisfy the fantasy? | Yes for V1; no serial acquisitions | Test acquisition, holding, and exit storyboards with target players. Product owner records the boundary. |
| 3. Do the financial language and valuation model remain clear under stress? | Cash-first model, limited loans, finite injections, separate realized/estimated returns | Systems designer tests promotion failure, player sale, borrowing, injection, distribution, and exit examples; settle rule wording and initial balancing ranges. |
| 4. Is the chosen world/pacing sufficient? | Three × 16 clubs, closed bottom tier, 45–75 minute experienced seasons | Walk through two full season calendars and recruitment supply; product lead validates the boundary and cadence. |
| 5. Do players accept the proposed football presentation and fictional identity? | Named players, score timeline, useful summaries, strong club identity | Artist/designer test representative match and club screens at actual reading sizes. |
| 6. Can the baseline team fund and deliver the defined Must Haves? | Three core contributors plus contractors, conditional 15–18 month envelope | Producer and technical lead assess capability, risks, dependencies, and budget alongside the architectural options; revise scope if unsupported. |
| 7. Is the premium offer credible, and can support be funded? | Full release, US$19.99 hypothesis, English baseline | Publisher/product lead tests the demo proposition and price with qualified players; confirms runway and launch support before committing to production. |
| 8. Do challenge progression, callbacks, and adjustable guidance produce understandable and enjoyable play? | EXP-01–EXP-08 are the V1 requirements; their player impact remains a hypothesis | Designer/UX lead applies M4 within the existing cohorts, compares novice and experienced players, and revises presentation or tradeoffs where evidence fails. |

Exact final prices, difficulty tuning, event prose, optional languages, and a festival date can remain open during development. The current team size, 15–18 month envelope, and performance baseline are planning assumptions to assess, not approved estimates. No playtest or market-validation results have been supplied.

Engine, persistence design, schemas, infrastructure, and implementation choices belong to the architectural design/TRD. This PRD supplies their product constraints. Ongoing validation should inform those choices; an unresolved commercial or balance question does not justify silently expanding the V1 scope.

## P. Architectural Design Handoff

### Brief for the receiving team

Prepare an architectural design and TRD for the V1 product defined in this document. Preserve owner-only authority, delegated football operations, finite capital, a single-club career, and the quantities and non-goals in F/K/N. Include the player-experience requirements EXP-01–EXP-08 in the design's traceability rather than treating them as optional presentation polish.

The architectural handoff should produce:

1. **Requirement coverage:** map the proposed design to the relevant PRD sections, all Must Haves in K, EXP-01–EXP-08, and the release evidence in M. Call out omissions or proposed scope changes explicitly.
2. **Technical options and rationale:** assess implementation approaches against the intended small team, Windows/offline experience, world size, career duration, and performance targets. Select and justify the technical approach in that document, not in this PRD.
3. **Behavior and continuity coverage:** explain how the design will preserve financial obligations, staff authority, decision/forecast history, named-person continuity, pending offers, simulation-time deadlines, and fair outcomes across advancing, saving, loading, and patches.
4. **Experience coverage:** explain support for contextual reports, genuine open questions, causal uncertainty, selectable guidance, voluntary ambition, skippable presentation, and reliable resume context using the existing product scope.
5. **Validation strategy:** describe how to obtain the simulation, economy, save, performance, and player-experience evidence required in M, including high-risk edge cases. Do not report unperformed checks as passed.
6. **Delivery assessment:** identify dependencies, staffing needs, major risks, an implementation sequence, and revised effort ranges. Separate technical feasibility from unverified player appeal and funding.
7. **Assumption register:** carry forward O's unresolved items with proposed validation, responsible discipline, and the product/design consequence if a default proves wrong.

### Validation alongside architectural planning

Retain a four-to-six-week ownership-loop validation allowance within the planning envelope. Early architectural work and this product validation can proceed together; expensive implementation commitments should use the findings.

The initial validation package should contain one fully specified acquisition with its financial obligations and three competing allocations; a paper or clickable prototype covering the owner brief, executive proposal, commitment, result, explanation, and sale offer; and three worked outcomes: sustainable growth, a failed promotion gamble, and a successful trading route. Include a historical callback and an understandable stretch decision. These are design/test artifacts, not extra launch scenarios or guaranteed gameplay results.

Use the 12-person cohort in M1, add the experience/challenge observations in M4, revise weak points, and record a go/revise/stop assessment with an updated delivery envelope. If the loop is dull or unclear, revise choices and feedback before expanding implementation. More leagues, tactical controls, or financing instruments are not substitutes for an enjoyable ownership experience.

**Handoff outcome:** one coherent architecture proposal for a game in which players understand what they own, choose consequential investments, trust delegation, experience an appropriate challenge, and care about the club's next chapter. Product validation and technical feasibility remain explicit work to complete, not prerequisites falsely marked as already satisfied.

### Coverage of the requested product topics

| Requested topic | Primary location |
|---|---|
| 1. Product vision | B |
| 2. Target player | C |
| 3. Core design principles | E |
| 4. Core gameplay loop | A, D |
| 5. Player decisions | G1 |
| 6. MVP / Version 1 scope | F, K, N |
| 7. Game world simulation | G3 |
| 8. Club economics | G4 |
| 9. Club valuation and owner success | G2, G5 |
| 10. Staff and delegation | G6 |
| 11. Football simulation | G7 |
| 12. Transfer market | G8 |
| 13. Facilities and capital projects | G9 |
| 14. Supporters and identity | G10 |
| 15. Events and emergent stories | G11 |
| 16. Difficulty and failure | G12 |
| 17. Onboarding | H1 |
| 18. User interface | H2–H4 |
| 19. Steam release strategy | J |
| 20. Competitive positioning | B |
| 21. Retention and replayability | D, I |
| 22. Development priority | K |
| 23. Release criteria | M2 |
| 24. Key product risks | L |
| 25. Open product questions | O |

### Coverage of the motivation and challenge refinements

| Follow-up requirement | Canonical location |
|---|---|
| Neurochemical inspiration, evidence limits, autonomy/competence/relatedness | B |
| Anticipation, mastery, attachment, satisfaction, and emotional loop | B, D |
| Investment stories, original forecasts, named people, successful restraint | G11, H5; EXP-01, EXP-03, EXP-04 |
| Three horizons, grouped attention, skippable presentation, closure and resume | H3, H5; EXP-02, EXP-05 |
| Appropriate challenge, learning progression, consolidation, and ambition | G12, H1, I4; EXP-06, EXP-08 |
| Adjustable help, immutable startup economics, and fair simulation | G12, H3; EXP-07 |
| Genuine enjoyment, voluntary return, novice/expert calibration | M1–M4 |
| Scope, priorities, acceptance, and architectural handoff | F, K, M2, N–P |

# Football Ownership Tycoon: architecture and product fit assessment

Assessment date: 18 October 2026. Source baseline: `57e640d` (`Compact saved journal rows without losing financial history`). The checkout was clean at inspection. This report is an assessment deliverable; it does not authorize or implement the proposed development.

Publication note, 20 October 2026: links are now repository-relative and the cited evidence has been copied into [assessment-evidence](assessment-evidence/README.md). The [agent task pack](agent-tasks/README.md) defines the narrower first experiment and its execution order. Findings and test results below retain their original assessment dates; publishing these documents does not constitute a new gameplay validation run.

## 1. Executive verdict

**Yes with targeted changes.** Preserve the Godot/C# application, simulation, football competitions, financial commitments, player lifecycle, proposal workflow, save safety, and existing Owner Desk. Choose **Option B: targeted architectural refactoring**, restricted initially to staff decisions, authority, and consequence reporting. Do not begin a major rewrite.

The current game is a functioning, bounded ownership prototype with strong accounting and replay foundations. It already lets an owner commit scarce resources, delegate a specific transaction, watch uncertain football results, and inspect original forecasts. It does **not yet demonstrate the refined product's depth**: choosing people is absent, strategic direction is largely an annual allocation choice, rivals follow narrow common policies, and important feedback loops end before they influence subsequent behavior.

The largest near-term risk is building more correct simulation without proving that an owner's choices produce understandable, interesting dilemmas. The largest long-career architectural risk is retaining and repeatedly copying, validating, hashing, and saving the entire growing history. These are different problems and should have different delivery gates.

The next investment should prove **two consequential staff relationships, bounded delegated authority, and one connected season of financial and sporting tradeoffs**. A general agent framework, multi-club ownership, lending, and a 50-season storage redesign are not prerequisites for that experiment.

Confidence is high in the code-level fit assessment, moderate in the proposed development sequence, and low in estimates of entertainment, strategy balance, and delivery time until a staffed team and playtest results are available.

## 2. Current implementation summary

### Evidence and verification boundary

“Implemented and working” below means executable behavior supported by inspected implementation and relevant tests or retained runtime evidence, within the stated prototype scope. It does not mean commercially finished, balanced, or validated by players. “Partially implemented” means a working subset of the requested capability. “Designed but not implemented” identifies explicit design content without a corresponding executable system. “Not present” means no supporting state/behavior was found in the inspected source. “Unknown or unverified” means the available evidence cannot establish the claim.

The assessment inspected the Core, Application, Infrastructure, Desktop, Headless, and Endurance projects; relevant xUnit tests; version history; current and historical design/status documents; retained diagnostics; and two saved screenshots from the schema-23 package. The refined request governs recommendations; the older PRD is evidence of intent, not approval to add all its features.

Fresh validation: the Release solution build passed with zero warnings and errors using the local .NET SDK 10.0.401, serial MSBuild, and shared compilation disabled. All **281 tests passed**, with none failed or skipped, against that freshly built Release assembly. Initial ordinary test/build invocations failed without a useful compiler diagnostic; the explicit serial build succeeded. A sandboxed test run subsequently aborted on a testhost connection timeout before executing tests; the successful run used approved execution outside the sandbox. No source repair was required.

Historical evidence is identified separately. In particular, the retained 281-test log, packaged graphical probes, and 50-season pilot were produced before this assessment. The screenshots establish existing layout, not a fresh interactive usability test. Steam integration, clean-machine behavior, human understanding, and fun were not verified here.

### Stack and structure

| Layer | Observed implementation | Assessment |
|---|---|---|
| Engine and language | Godot .NET SDK 4.7.2, C#, `net10.0`, Control/Container UI, Compatibility renderer. [Desktop project][desktop-project], [project settings][godot] | Suitable for a data-heavy desktop ownership game; no engine replacement is justified. |
| Simulation | Pure C# mutable `World` aggregate, stable typed IDs, immutable records for players/results/terms, checked integer-pence accounting. [Model][model], [WorldFactory][factory] | Good separation and reproducibility; the aggregate includes both live state and all accumulated history. |
| Application | `GameSession` serializes commands, reads, loads and saves through one background channel; exposes immutable `GameView` records. [GameSession][session] | Preserve the single writer and transactional publication. This is a background worker, not parallel simulation of clubs. |
| Storage | Versioned gzip JSON snapshots, metadata-inclusive SHA-256, temporary durable write, read-back validation, immutable rename, recovery and branch ancestry. [LocalSaveVault][vault] | Strong short-career safety; bounded snapshot size and unlimited retained generations restrict growth. |
| Tooling | Headless application runner, separately compiled diagnostic endurance runner, xUnit, CI build/test/smoke workflows. [Headless][headless], [Endurance][endurance], [CI][ci] | Substantial reusable validation investment. Configured CI is not evidence that the remote checks ran during this assessment. |

### System inventory

Test filenames in this table identify existing executable coverage, not complete coverage of the refined vision. Dependencies show the implemented connections that can be reused.

| System and maturity | Functionality and supporting state/code | Dependencies and reusable capabilities | Limits and existing tests |
|---|---|---|---|
| Acquisition — **Implemented and working**, one scenario | `AcquisitionCounteroffer`, one lower offer, persisted response, separate purchase confirmation; Stonebridge FC is fixed. [Acquisition][acquisition], `Proposals.Commit` | Separate owner/seller/club money, immutable purchase history, preview/commit safety | No portfolio, choice among acquisition targets, exit, or appraisal. `AcquisitionTests`: durable response, invalid offers, seed variation, migration, failed saves. |
| Cash, commitments and distress — **Implemented and working**, narrow economy | `LedgerEntry`, `Obligation`, `Arrear`; weekly settlement, signed wages, operating costs, owner injections, reserve exceptions, 75% wage rule, administration and loss of control. [Finance][finance], `Simulation.Settle` | Recruitment and facilities consume actual cash/commitments; promotion changes future revenue; ledger reconciliation is reusable | No loans, distributions, lender behavior, full financial statements, or rival insolvency resolution. `SimulationTests`, `CashProjectionTests`, `CashflowDiagnosticsTests`, `RenewalWageBasisTests`. |
| Forecasts and approvals — **Implemented and working**, partial explanation | 52-week base/downside paths, pure previews, fingerprints, revision checks, stored original forecasts, persisted duplicate-command receipts. `Proposal`, `DecisionRecord`, `Proposals` | Execution rechecks affordability; saved terms support accountability | Downside is a fixed 72% of estimated earned receipts; not a calibrated probability interval or adviser judgment. `SimulationTests`, `CashProjectionTests`, `PersistenceTests`. |
| Calendar and competitions — **Implemented and working** | 48 clubs, three divisions of 16, 720 league fixtures and 47 cup matches per completed season; dated weeks; top-two/bottom-two tier movement; three playable seasons. [Calendar][calendar], [Seasons][seasons], [Pyramid][pyramid], [Cups][cups] | Shared world schedules rivals, changes income and competitive opposition, retains results | No open-ended playable career; 50 seasons exists only in the diagnostic build. `CalendarTests`, `SeasonTests`, `PyramidTests`, `CupTests`. |
| Matches and availability — **Implemented and working**, aggregate model | Automatic best-available 1-4-4-2, fallback roles, short sides, ability/home advantage, goals, ratings, cards, injuries, bans. [Matchday][matchday] | Recruitment and availability change selected strength; appearances feed development; cup outcomes produce prizes | No distinct manager policy, substitutions, fatigue, cohesion, morale, or tactical AI personalities. `MatchRealismTests`, including rate ranges, selection, events, availability and migration. |
| Recruitment — **Partially implemented** | Owner-authorized opening forward search; January first-choice/value/wait; recommended free-agent approaches; delayed fallible resolution and exact wage dates. `Negotiation`, `FreeAgentBid`, [RecruitmentMarket][market], [FreeAgents][free] | Shared player identities, seller receipts on paid transfers, common wage/reserve checks, pending competition | Fixed paid fees/ceilings; opening target comes from the first qualifying rival; one January decision; no owner sale command or general market. `RecruitmentMarketTests`, `FreeAgentTests`, `OwnerMinimumSquadTests`, `RecruitmentContentionTests`. |
| Rival agency — **Partially implemented** | Independent results, cash settlement, aging, renewals/releases, academy intake, retirement and competing free-agent replacement offers. `RivalApproach`, [RivalRecruitment][rivals] | Same constrained supply, financial affordability and squad cover; rivals can take a player the owner wants | Common conservative policy; no rival paid buying strategy, discretionary facility investment, management hiring, borrowing or restructuring. Rival arrears exist but do not trigger rival administration. `RivalRecruitmentTests`, `RivalMinimumSquadTests`, `RivalVacancyDiagnosticsTests`. |
| Contracts and population — **Implemented and working**, bounded lifecycle | 1,056 contracted players plus 144 free agents at creation; annual renewal/release recommendations, fixed retirement announcements, departure archives, player identity continuity. [Contracts][contracts], [RetirementLifecycle][retirement], [PlayerHistory][player-history] | Signed wage obligations survive decisions and relegation; releases enter common pool; archives retain names | Players always accept renewal terms; recommendation defaults are uniform; minimum-cover rules constrain owner releases. Long-run population/coverage acceptance unresolved. `ContractTests`, `RetirementTests`, `PlayerIdentityTests`, `OpeningPopulationTests`, `PlayerHistoryTests`. |
| Youth and development — **Implemented and working**, partial strategic depth | Annual age/exposure-based ability changes; inherited academy offers 0–2 age-17 graduates subject to cash, wages and squad space. [Academy][academy], [PlayerDevelopment][development] | Playing time and accumulated training exposure affect uncertain progress; recruits compete for the same first-team places | No academy investment, youth pipeline management, player potential model, or sale proceeds; training does not increase academy production. `AcademyTests`, `AcademyBalanceTests`, `PlayerDevelopmentTests`, `TrainingTests`. |
| Facilities — **Implemented and working**, narrow investments | Hospitality and training, three levels each, one construction at a time, upfront payment, 28-/32-week delivery, later upkeep. `Project`, [Facilities][facilities] | Hospitality earns per home date; training changes younger players' growth chance with diminishing level benefits | No capacity/demand saturation model for hospitality, project cancellation command, staffing choices, delays, stadium or academy investment. Recoverable value is stored but cannot currently be realized by cancellation. `TrainingTests`, forecast and simulation tests. |
| Leadership and relationships — **Designed but not implemented** beyond fixed presentation | CEO Mara Ellis, director Jonas Reed and manager Callum Price appear as strings; algorithms execute their described functions. `Main.People`, `Proposals.Preview`, `Matchday.ManagerLine` | Named presenters and existing decision records can become accountable staff roles | No staff entity, hiring/dismissal command, individual contract, competence, ambition, loyalty, risk preference, relationship or delegation limit. No tests can establish these missing behaviors. See [PRD G6][prd]. |
| Strategy and organizational growth — **Partially implemented** | Annual choice of reserve/hospitality/recruitment/training, plus explicit spending exceptions | Existing proposal workflow and automated execution | No persisted ambition/priority policy, authority growth, organizational capability or evolving leadership structure. `TrainingTests` explicitly enforces the one-plan constraint. |
| Supporters and stakeholders — **Partially implemented state; behavioral loop absent** | `Club.Support` rises/falls with results | Could become a bounded demand/expectation input | No read of `Support` was found in current Core behavior; no favorites, promises or conflicting stakeholder goals. Not a working supporter economy. |
| Owner Desk and reports — **Implemented and working**, partial attention model | Six workspaces, selectable inbox, cash/fixture timeline, proposal comparison, confirmation, original commitments, match/season reports, settings and recovery. [Main][main], [Main.Design][design], [Main.Decision][decision-ui] | Existing structure supports shallow decisions with deeper inspection; no routine lineup/tactics controls | Recent-item inbox rather than prioritized escalation system; filing is transient UI state; reports are long generated lists. `Main.Smoke.cs` contains engine checks; retained package screenshots cover scales. No fresh human validation. |
| Long careers and Steam delivery — **Unknown or unverified** as a product | Diagnostic runner can compile a 50-season limit; offline platform adapter and Windows export scripts exist | Useful profiling, replay, population and cashflow measurements | Playable limit remains three seasons. Steamworks/Cloud/achievements are absent; clean-machine acceptance and long-career durable saves are unverified. `EnduranceTests`, `CheckpointDiagnosticsTests`, `CompactJournalTests`, `CoverageDiagnosticsTests`. |

Promises and owner reputation are **not present** as saved systems. Lending, supporter effects, hiring, valuation/exit and additional acquisition scenarios are **designed but not implemented** in the older PRD. Event/story requirements in documentation do not establish a runtime narrative framework. Multi-club ownership is a new candidate to assess, not existing functionality.

## 3. Architecture fit

### Preserve the existing boundaries

`GameSession.CommitAsync` clones a candidate, validates the proposal again, writes recovery and candidate checkpoints, and publishes only after successful persistence. `AdvanceAsync` likewise publishes each candidate week only after its autosave succeeds. Tests explicitly cover failed publication, concurrent duplicate commands, recovery, branching and replay. This is exactly the safety needed when a director acts under authority or an owner approves a large commitment. [GameSession:90][session-commands], [PersistenceTests][persistence-tests]

Core has no Godot dependency. `GameView` carries immutable reports and the Desktop queues UI updates back onto the engine thread. Keep this separation. UI classes share considerable state and a single large read model, but that is a maintainability concern, not a reason to replace the UI or engine.

The weekly order is explicit: payments → negotiations/free-agent resolution → matches → training/project delivery → reports/season closure. Stable IDs, ordered loops, saved SplitMix64 streams, and source-level version markers support reproducibility. The existing cadence is adequate for the proposed slice; there is no evidence that a real-time scheduler, ECS, distributed services or multithreaded agent engine is needed. [Simulation:9][simulation], [WorldFactory:23][rng]

### Material limits and the smallest useful changes

| Limitation and evidence | Why it matters | Bounded architectural response |
|---|---|---|
| `OwnerCommand` is largely an `Allocation` enum; `Proposals.Preview/Commit` switches over it. Capital plans share a single `AllocationChosen` flag. | Appointments, policies, authority and plan revisions do not fit naturally into an ever-growing investment switch. Resolving a capital plan currently resolves every unresolved required `Decision`, which would be wrong once independent required decisions exist. | Introduce typed staff/policy actions at the application boundary and explicit decision identities/resolution. Reuse revision checks, receipts and transaction publication; preserve existing allocation actions during transition. |
| `World`/`Club` have no leadership entities or saved policy state. `Matchday.Select`, `Contracts.Plan` and rival recruitment are fixed static policies. | Staff cannot change behavior or be accountable for different choices. | Add a small persisted staff/mandate model and pass its policy into existing selection/recruitment/renewal functions. Avoid a universal utility engine until multiple real policies require one. |
| Owner/rival recruitment paths have distinct offer types and hard-coded owner lookups. | Autonomous owner signings and common authority limits could drift or bypass accounting. | Share eligibility, budget reservation, commitment and execution checks across those paths. Initially retain their existing orchestration and histories. Test owner/rival contention, pending commitments and changed authority before resolution. |
| `Decision` has title/due/required/resolved; `Review` mostly stores prose and optional forecast ID. | No explicit responsible actor, escalation reason, severity, supersession or structured outcome causes. The Desk must infer attention from strings/status. | Add a narrow structured decision/outcome record: actor, mandate, deadline, important inputs, checks, resulting commitments and linked outcome. Render prose from facts. This does not require event-sourcing the entire world. |
| Weekly stages mutate shared aggregate state directly; invariants refer to global lists. | Adding cross-system effects can accidentally depend on stage order or consume unrelated randomness. | Document each stage's inputs/effects and schedule staff planning/resolution explicitly. Separate named RNG streams; test phase boundaries. Keep a single writer. |
| Migrations are a sequential chain in `WorldCodec.Decode`; `Validate` has many historical-version flags. | Each new persistent feature adds compatibility complexity. Exact old future behavior is not automatically preserved by structural migration. | Add explicit migration fixtures for staff defaults, authorities, pending offers and historical decisions. Refactor migration organization when adding those schemas, retaining existing steps. State supported schema and prospective simulation-change policy. |

No current defect requires restructuring all Core files before testing the product. Extract only the boundaries a new behavior actually needs.

### Long-career storage and performance are a separate gate

`WorldCodec.Clone` is `Decode(Encode(world))`. Each weekly application step clones the full world; persistence encodes, decodes to validate, compresses, writes, then reads/decodes for validation. `Finance.Forecast` also hashes the encoded entire world, and `QueryAsync` calls it. Journal, results, fixtures, random stream entries, reviews and histories accumulate. Query arrays and desktop report controls also grow. The unbounded application channel could accumulate queued work if future callers submit faster than the worker completes it. [Model:408][clone], [Finance:81][forecast], [LocalSaveVault:27][vault-write]

The production vault limits uncompressed worlds to **64 MiB** and containers to **32 MiB**. Autosave slot names rotate, but snapshot files do not: all physical generations remain. This creates both per-save capacity and total-disk-growth problems. Merely increasing a cap or deleting old journal entries would not preserve the current safety and history guarantees.

Retained evidence gives scale, with important qualifications:

| Evidence inspected | Observation | What it establishes |
|---|---|---|
| Schema-23 compact-journal measurement, seed 2026, diagnostic policy | Week 156: 187,813 journal rows; 15,791,754 raw bytes; 3,855,007-byte durable compact container. Week 208: 20,856,102 raw bytes. [Measurement JSON][compact-data] | Compact storage materially helps; the sampled short horizon fits. Diagnostic week 156 is not a separately played production career. |
| Schema-21, seed-2028, 50-season Training then PreserveReserve/CoverShortages pilot | 416,739,256 raw bytes; 3,881 ms maximum measured core weekly call; 1,609 rival shortage club-weeks; final active population 1,077. [Raw summary][pilot-data] | Older core can finish one long trajectory; neither current-schema capacity nor acceptable population, durability or responsiveness follows. The pilot bypassed weekly application autosaves. |
| Same pilot's retained cashflow extraction | Aggregate club cash £72.8m → £4.099bn; owned-club cash £119.58m; final-year aggregate cash increase £83.53m. [Analysis][pilot-analysis] | Accounting reconciles while economic challenge can erode. This is one historical policy/sample, not a current strategy comparison or proof of a dominant strategy. |

Before extending the playable horizon, profile **end-to-end application advancement, queries, saves, recovery listing, allocations and memory**, not only `AdvanceWeek`. Then decide whether immutable season archives plus a smaller live aggregate, reusable derived indexes and a retention/catalog policy are sufficient. Preserve exact money history, person identity, original decisions and recoverable branch ancestry. This is targeted storage evolution, not justification for rewriting football simulation.

### Determinism and save promises

Preserve same-version replay: identical initial state, commands and seed should yield identical gameplay state after save/load. Do not promise identical results across changed simulation policies just because a migration loads successfully. New staff defaults must be explicit and prospective; do not invent historical staff promises or rewrite signed wages. Preview/query actions must remain pure. Persist pending authority and commitment information so loading cannot silently expand a delegate's spending permission.

## 4. Product and gameplay fit

### Fit to the six requested capabilities

| Capability | Current design fit |
|---|---|
| A. Interacting systems | **Partial, with real connections.** Spending → commitments → reserve/headroom; roster → available selection → results → tier/cup income; appearances/training → development → next renewal recommendations. Missing relationship, demand, sale and valuation connections keep these chains shallow. |
| B. Independent football economy | **Partial.** Rivals genuinely play, pay, renew, develop and compete for free agents. They do not choose business strategies, invest capital, hire managers or trade broadly. The world is active but economically homogeneous. |
| C. Distinct people | **Missing for staff.** Players differ by age, ability, role and availability. Named executives have no behavioral attributes or relationships. |
| D. Delegation and growth | **Partial execution, missing governance.** Managers select automatically and directors execute approved offers. There are no owner-defined budgets/authority thresholds for routine autonomous work, nor growth in organizational capability. |
| E. Evolving challenges | **Early foundations only.** Promotion/relegation, aging and expiring contracts change constraints. No institutional, succession, retention, ownership-return or legacy stage exists. The three-season cap prevents evaluation of a late game. |
| F. Explanation under uncertainty | **Strong financial provenance, incomplete causal explanation.** Original forecasts, payments, lineups and development reports survive. Adviser competence does not affect estimates, and some outcome text merges distinct causes. |

### Mechanics worth preserving

The owner never routinely chooses a lineup or tactics. That is a meaningful product advantage already present. Recruitment approval creates permission, not an immediate purchase; it can fail and be rechecked. Retaining cash competes with construction and player commitments. Facilities take time and have upkeep. Promotion improves future published revenue while signed wages survive relegation. Injury and retirement can expose weak cover. These are useful foundations for ownership dilemmas, rather than decorative statistics.

Training is more than an instant numerical upgrade: cash leaves now, delivery takes 32 weeks, benefit depends on actual exposure, age and uncertain growth, and appearances matter. However, the fixed manager selects the strongest available players. Weak academy graduates may rarely play, and the owner cannot choose a youth-development manager. The strategic bridge between investment and development is incomplete.

### Concrete design weaknesses

1. **Direction is an exclusive annual purchase menu.** `Proposals.Preview` blocks a second capital plan once `AllocationChosen` is true. Even cash preservation consumes that choice. January recruitment and free-agent approaches provide exceptions, but the owner cannot revise a persistent strategy or allocate a general transfer/wage/development envelope. Preserve the clear comparison UI while replacing this artificial exclusivity with budgets and a reviewable priority.
2. **People labels imply more agency than exists.** People calls Mara cautious and Jonas focused on immediate readiness, but those traits cannot vary and do not enter calculations. Callum's post-match voice is generated from the result, not an individual assessment. Staff should change observable decisions, not simply add percentage bonuses.
3. **Supporter feedback stops at a counter.** `Simulation.Resolve` changes `Club.Support`, but receipts use tier-scaled historical amounts and random demand. No behavioral consumer of support was found. The monthly-review sentence that receipts vary with results overstates the direct implemented relationship: results can affect cup progression and later tier income, but an ordinary win does not currently increase that match's demand. Correct the explanation when this area is next changed.
4. **Hospitality is a fixed increment with stochastic demand.** Each level adds the same receipt amount per home fixture; there is no sold-out demand, capacity saturation or different operating strategy. Upfront cost, upkeep and timing create a tradeoff, but additional levels do not create a distinct business problem. Keep the investment; extend the demand constraint before adding more upgrade categories.
5. **Renewal detail can become administrative work.** Owners can accept the default recommendations in one commitment, so per-player clicks are not mandatory. But the default presentation exposes long lists of individual reversals and terms. Use a concise delegation summary, flag exceptional contracts, and retain the full roster/terms as drill-down. Do not remove informed oversight.
6. **Uncertainty is often a fixed rule, not informed judgment.** Paid signing success is a 70% draw after eligibility, and free-agent acceptance uses a similar rule. Player renewal acceptance is certain. Exact ability is visible. Competence-dependent estimates and conflicting advice are absent. Keep contractual cash exact; uncertain sporting estimates can be adviser-dependent without making arithmetic unreliable.
7. **Failure explanations are too broad for learning.** Recruitment can report “failed or no longer met” availability, deadline, reserve or wage checks together. The game has the checks necessary to state the actual binding reason. Record that reason, then distinguish staff choice, external competition and football variance. Avoid claiming a single match proves manager competence.
8. **Optimal strategy and challenge remain unproven.** Fixed investment menus and growing long-run cash suggest limited adaptive pressure, but no inspected matched strategy experiment proves reserve, hospitality or recruitment universally best. Measure that before balancing by intuition. Accurate bookkeeping alone does not create financial tension.

### Can the four example stories emerge now?

| Story | Existing ingredients | Missing causal ingredients |
|---|---|---|
| Academy gamble | Graduates, training, ages, appearances, uncertain growth, wages and cover | Academy investment, manager development priorities, sale market and stakeholder pressure. A smaller training-versus-readiness dilemma is feasible first. |
| Manager dilemma | Poor results, named manager reports, development records | Appointments, manager influence on development/selection, dressing-room support, tenure and replacement consequences. |
| Financial gamble | Recruitment cost, continuing wages, failed promotion, finite owner capital, arrears | Borrowing/debt service. A liquidity gamble can be tested with existing capital before introducing loans. |
| Profitable but unpopular sale | Persistent named graduates, cash ledger, supporter counter | Owner sale approval, executable buyer offers, valuation/price formation, fan attachment and manager response. A release is not a profitable sale. |

### Reuse the Chairman's Desk architecture

The current Owner Desk already has a “Needs you” inbox, owner brief, commitment cards, reviews, match access, cash/fixture timeline, and an at-a-glance rail. `BuildInbox` shows the newest six reviews, rather than selecting by materiality, and filing is an in-memory `HashSet`. `QueryAsync` takes the first three generated open questions; a less important item can crowd out a later one. These are attention-policy limitations, not missing navigation foundations. [Main.Design:209][inbox], [GameSession:136][query]

The saved Owner Desk screenshot confirms an existing hub with clear navigation and financial context. The renewal screenshot illustrates dense explanatory text and long terms. These observations support progressive disclosure, not a claim that users have found the interface confusing. [Desk screenshot][desk-image], [Renewal screenshot][renewal-image]

| Existing screen/workflow | Recommendation |
|---|---|
| Owner Desk | **Keep and extend.** Lead with approvals, risks and opportunities ranked by deadline/materiality; quiet weeks should remain quick. Persist acknowledgement, distinguish decisions from information, and link each item to evidence. |
| Proposal comparison and confirmation | **Keep; simplify routine cases.** Reuse cash previews and final terms for material commitments. Routine delegated actions belong in a digest. Escalations need their actual reason and alternative. |
| Football | **Keep.** Preserve table, cup, match detail and availability. Add manager mandate/outcome context; send material recruitment approvals to the Desk while retaining detailed comparisons here. |
| Business | **Keep and extend.** Existing cash reconciliation and obligations are useful; add period comparisons and drivers. Do not call it a complete balance sheet or profit-and-loss statement. |
| People | **Substantial content change within the existing screen.** Add appointment/mandate/performance views for leaders; separate roster and archives into drill-downs. Preserve player identities and development history. |
| Club | **Keep and simplify.** Use it for long-term direction, identity and projects; replace fixed thesis text with the chosen mandate when that system exists. |
| History | **Keep and extend.** Link decision → delegated action → actual outcome → later review. Filter/paginate as history grows; retain financial evidence. |
| Continue, settings and recovery | **Keep.** Required decisions and active proposal review already pause progression. Test new deadlines and autonomy against these existing rules. |

## 5. Gap analysis

Priority: **P0** proves the next owner-loop slice; **P1** follows evidence from that slice; **P2** is later scope. Effort is relative: Low = localized work, Medium = several connected modules, High = a new cross-system behavior or persistence boundary. Risk combines regressions, tuning and product uncertainty. Rows are not independently additive estimates.

| Capability | Current state | Target state | Gap | Recommendation | Effort | Risk |
|---|---|---|---|---|---|---|
| P0: simulation/application boundary | Pure Core, one writer, immutable views, transactional commits | Reliable owner decisions and autonomous execution | New staff actions must obey existing guarantees | **Keep** transaction and replay infrastructure; it already protects money and commitments | Low | Low |
| P0: owner direction | Four exclusive annual allocations | Persistent priorities with bounded revisable budgets | No policy state; preserving cash locks plan | **Extend** direction; **Remove** mandatory one-plan exclusivity after replacement budgets are verified | Medium | Medium |
| P0: staff | Names and fixed policies | Two selectable, accountable leaders with different behavior | No staff entities/contracts/relationships | **Build** manager/director state; **Extend** existing decision functions, rather than replace simulation | High | High |
| P0: delegation | Every owner signing requires approval; rivals act automatically | Routine actions inside cumulative authority; exceptions escalate | No budget reservation or materiality governance | **Refactor** shared authorization/execution checks; **Build** mandate state and digest | High | High |
| P0: connected outcomes | Football, finance and development partly interact | Choices visibly alter multiple later constraints | Staff-development and expectation-demand loops absent | **Extend** existing selection/development and bounded demand inputs; prioritize two demonstrated chains | High | High |
| P0: Desk attention | Recent reviews, three ordered questions, transient filing | Durable prioritized decisions and risks | No structured severity, acknowledgement or supersession | **Extend** current inbox/read model; preserve shell and deep screens | Medium | Medium |
| P0: explanations | Cash evidence, generic failure prose | Actual reasons, responsible actors, uncertain contributions | Causes often not recorded structurally | **Extend** result records and reviews; separate facts from estimates | Medium | Medium |
| P0: renewals | Defaults plus detailed individual overrides | Policy-led approval with exceptional terms surfaced | Presentation burden grows with roster | **Simplify** default presentation; retain detailed terms and overrides | Low–Medium | Low |
| P0: changing required decisions | Capital plan resolves all unresolved required decisions | Each approval resolves its own request | Existing shortcut would clear unrelated new actions | **Refactor** decision targeting before adding independent required approvals | Medium | High |
| P0: same-scenario divergence | Seeded systems and strategy runner exist | Reproducible paths differing because of decisions | No product acceptance evidence | **Extend** matched-seed and human validation; preserve current harness | Medium | Medium |
| P1: football economy | Rival renewals, free agents, arrears, development | Distinct resource-constrained strategies and meaningful competition | Narrow common policy; missing investment/trading/distress response | **Extend** one rival policy choice at a time; share financial rules | High | High |
| P1: sales and attachment | No sale action; named history exists | Price versus squad/fan/manager consequences | Buyer offers, seller logic and attachment absent | **Build** bounded sale approval over existing player/ledger identities; necessary for academy-sale stories | High | High |
| P1: facilities | Fixed three-level upgrades | Demand/exposure-dependent returns and opportunity costs | Hospitality lacks saturation; academy capital absent | **Extend** demand before multiplying upgrade types; **Defer** additional facility categories | Medium | Medium |
| P1: long-career capacity | Whole-world snapshots, retained generations, historical over-cap worlds | Bounded live-state costs and safe retained history | Cloning/hash/validation/storage grow together | **Refactor** history/storage only after measurement; gate any horizon expansion | High | High |
| P1: migration organization | Sequential migrations and many validation flags | Clear supported compatibility policy and fixtures | New staff and authority state complicate old saves | **Refactor** migration organization incrementally; preserve signed and pending terms | Medium | High |
| P1: stage progression | Tier changes and aging, then prototype ends | Survival → growth → elite retention/succession → legacy | Later problems and ownership outcomes absent | **Extend** stages after early loop validates; **Defer** institutional/legacy systems | High | High |
| P2: loans, valuation and exit | PRD designs; no execution | Transparent financing and realizable ownership outcomes | New contract and buyer/lender systems | **Defer** from slice; **Build** later if still in reviewed release scope | High | High |
| P2: multi-club ownership | Single `OwnedClubId` throughout commands/views | Portfolio capital and delegated local management | Ownership, conflict and attention model all change | **Defer**; single-club depth has not yet been demonstrated | Very high | Very high |

### Additional design candidates

| Candidate | Potential value | Minimum credible cost and recommendation |
|---|---|---|
| Promises/commitments | Delayed accountability; grounds a later disagreement in a remembered choice | Existing financial commitments already work. A behavioral promise needs terms, subject, deadline, fulfillment rule and bounded consequences. **Defer** a general promise system; test one clear staff mandate in the slice. Avoid opaque dialogue traps. |
| Owner reputation from behavior | Staff willingness, trust and later opportunities can reflect repeated choices | Requires trustworthy event provenance and consumers; a number alone adds little. **Defer** a global reputation score. Preserve decision history and test local relationships first. |
| Competing stakeholders | Creates meaningful disagreement over the same resources | Start with manager readiness versus director development/reserve tradeoffs and a simple supporter expectation/demand effect. **Extend/Build** this small set; avoid a stakeholder dashboard full of unused meters. |
| Progressive delegation | Growth reduces routine work while increasing strategic responsibility | Needs authority, cumulative budget checks, competent defaults, exception reporting and visible review. **Build** the basic authority model now; **Defer** multiple organizational tiers. Unlock authority through capability/evidence, not arbitrary button removal. |
| Acquisitions/multi-club | New starts and eventually portfolio dilemmas | A second authored single-club starting scenario reuses much more than simultaneous ownership. **Defer** both from the slice; assess multiple acquisition scenarios before a portfolio. |

## 6. Development options

These are planning estimates, not measured implementation schedules. Assume two experienced full-time C#/Godot engineers, a half-time systems/product designer, and part-time QA/playtest support. Estimates include integration and a player-test iteration for the bounded slice below; exclude art/audio, Steam release work, all PRD content and a fully proven 50-season product. Team availability is unknown; allow roughly ±50% until a short technical spike and content pass establish actual velocity. With a solo/part-time team, elapsed time will be materially longer.

| Dimension | A: evolve through existing structures | B: targeted refactoring — recommended | C: major redesign/rewrite |
|---|---|---|---|
| Development effort to comparable slice | About 6–10 elapsed weeks / 12–20 engineering person-weeks | About 8–12 elapsed weeks / 16–24 engineering person-weeks | About 24–40+ elapsed weeks / 48–80+ engineering person-weeks |
| Technical risk | Low initially; medium/high as policy cases duplicate and interact | Medium, concentrated in authority, decisions and migrations | High: rebuilds already solved persistence, calendar, lifecycle and UI integration |
| Rework | Low immediate; likely repeated changes to allocation switches and hard-coded owner paths | Moderate localized changes; existing behavior retained behind verified boundaries | Very high; most current runtime integration must be recreated or adapted |
| Save implications | Additive schema changes, but growing migration complexity | Add staff/policy/decision state with explicit defaults; existing checkpoints import through copied candidates; archival storage migration later | Importer and long dual-validation period, or a new-save break; no automatic compatibility |
| Current functionality | Fastest to preserve initially, but increasingly awkward mixed behavior | Keep competitions, commitments, player lifecycle, reports and save guarantees | High chance of temporary regressions and a less capable playable build |
| Meaningful playable feedback | Early behavior increment in 2–4 weeks; full slice in range above | Early two-policy behavior demonstration in 2–3 weeks; full slice in range above | Early shell may appear sooner, but comparable ownership behavior much later |
| Long-term extensibility | Adequate for one experiment, weaker for many independent staff actions | Good for the refined single-club vision; storage expansion remains separately gated | Potentially good, but unproven and vulnerable to designing for hypothetical scale |

Option A is reasonable for a disposable behavior experiment, but pushing all staff and authority changes through the current allocation switch would create avoidable rework. Option B should look like a sequence of small product changes, each with the minimum extraction it needs. It must not become months of framework construction. Option C is justified only by new evidence that the engine, transaction model or domain structure cannot meet measured requirements; no such evidence was found.

## 7. Recommended vertical slice

**One club, one season, two accountable people, three viable priorities, and a visible change of plan.** Use Stonebridge and the existing surrounding 48-club world. Keeping the working three-tier schedule and cup is cheaper and more representative than stripping it down to a new miniature league.

Start in preseason with the existing finances, suitably tuned disclosed scenario constraints, and a small number of identifiable young and established players. The exact opening numbers need balance work: do not assume the current surplus-generating economy will produce a genuine survival dilemma. Use real wage, project and opportunity costs; do not manufacture crises with unexplained cash deductions.

### Reused capabilities and minimum new development

| Reuse now | Minimum addition for this test |
|---|---|
| Existing roster, injuries, selection and annual development | Two alternative manager profiles whose competence and readiness/development preferences affect actual selection and development opportunities; all lineups remain automatic. |
| Recruitment/renewal recommendations, exact signed wages and shared free-agent pool | Two director profiles with different readiness/value/risk priorities; persist appointment terms, a simple owner relationship and a review outcome for manager and director. Support or replace at least one leader during the season, with visible cost and continuity consequences. |
| Preview/commit pipeline, reserve checks, pending bids | Owner chooses priority, transfer/wage limits and routine authority; director can execute at least one safe routine action without a click and escalate a material or out-of-policy action. Commitments reserve shared authority so multiple offers cannot spend the same permission. |
| Training exposure and development report | A youth-focused manager actually gives plausible prospects competitive exposure. Inherit suitable prospects at scenario start; academy intake occurs at rollover, so new graduates alone cannot demonstrate a full first-season pathway. Retain real 32-week construction timing. |
| Support state and match receipts | A bounded, disclosed expectation/support effect on later demand, with documented lag; immediate results must not deterministically predict revenue. This adds a small business consequence to sporting choices. |
| Desk, history, original forecasts, manager result reports | Structured cause/effect records, meaningful risk prioritization, two competing recommendations on the same material decision, and a readable season review. |

Do not build six staff attributes merely to fill a card. For this test, competence, philosophy and risk preference need real consumers; relationships need a small observable consequence such as willingness to continue under a mandate or response to an override. Ambition and loyalty can wait until retention and succession require them. Include simple appointment/dismissal costs without accidentally charging both new staff wages and the leadership salaries already bundled into opening operations.

Three intended approaches are financial resilience, immediate readiness, and youth development. They must be preferences under the same rules, not scripted endings. The slice should demonstrate these chains:

- Readiness spending → fee/wage commitment → stronger selected squad but less cash headroom → different response to injury or failed signing → revised mandate.
- Development priority → manager selection/exposure → short-term sporting risk and wage choices → uncertain player progress → a different renewal decision.
- Sporting progress versus declared expectations → supporter response → later demand → budget review, moderated by external football uncertainty.

Selling a graduate, borrowing, valuation, owner reputation, multi-club control and late-game institutions are **outside this minimum slice**. They can deepen a demonstrated loop later. Do not introduce them merely to reproduce every illustrative story. Likewise, a random injury must not be forced into every human run; use controlled fixtures to verify edge cases, then natural seeded runs to assess frequency.

### Proposed measurable acceptance criteria

These are new product-test targets, not claims that the current game meets them. Approve or adjust them with the assessment findings before implementation.

| Gate | Proposed acceptance |
|---|---|
| Complete loop | A tester completes a 52-week season and the year-end review, sees consequences of both staff mandates, and has access to the next-season choice. Target 45–90 minutes for a first season; measure actual pace rather than accelerating until choices become incomprehensible. |
| Owner authority | Zero required tactical/lineup decisions. Routine recruitment/renewals inside authority require no owner action. Every material/over-limit action waits for approval and identifies its reason. All pending commitments count against shared limits. |
| Decision quality | Target 8–14 material owner decisions per season, excluding report navigation. Each consequential approval presents a feasible alternative and a concrete opportunity cost. Do not pad the count with cosmetic confirmations. |
| Staff causality | Under controlled identical states, changing only manager philosophy changes eligible selection/exposure; changing only director policy changes a feasible recommendation or execution choice. Both leaders produce inspectable outcomes; replacement changes behavior and incurs disclosed costs. |
| Interacting consequences | At least two chains span three systems and two different points in time. Histories link the original mandate, execution, cash/roster changes and later review. The actual binding failure reason is retained for every tested failed transaction. |
| Reproducible divergence | Run three policies across the same 20 seeds, with recorded commands and unchanged scenario rules. Exact reruns match gameplay state. At least 15 of 20 seed triplets differ materially on two of cash/runway, points/tier, young-player exposure/development, or staff relationship. Pre-register material thresholds, initially £100k cash, three league points, ten young-player appearances, or a discrete relationship/retention outcome. |
| Tradeoffs, not guaranteed victories | Across the sample, no policy is strictly superior on all tracked objectives in at least 80% of seed comparisons. If one is, inspect costs, policy behavior and measurement before inventing compensating penalties. These small samples identify tuning problems; they do not prove final balance. |
| Adaptation and uncertainty | In human tests, at least 8 of 12 players encounter a plausible development that prompts reconsideration. Controlled tests cover injury, player refusal, changed affordability and competing rival offers whether or not each occurs in a human run. |
| Understanding and desire to replay | Recruit 12 target players with a mix of football-management and tycoon familiarity. At least 9 can explain two decisions and correctly trace two consequences without coaching; at least 8 can name a different approach they want to try and choose to start another season/run when offered. Record behavior and misconceptions, not just ratings. |
| Persistence and regression | Existing relevant suite passes; new save/load, migration, failure injection and deterministic tests cover staff replacement, pending authority, simultaneous bids and escalation deadlines. Reloading cannot duplicate an obligation or bypass an approval. |
| Performance | On a named baseline PC, target p95 weekly advance including autosave ≤500 ms, p95 monthly advance ≤2 s, and p95 normal query/proposal ≤200 ms over the slice; verify memory/disk headroom and no blocking engine-thread work. These are initial targets to validate, not measured current performance. |

Review the results before adding more systems. If players see only “choose the highest rating” or cannot explain consequences, revise policy tradeoffs and reporting first. More content would conceal the failure rather than resolve it.

## 8. Implementation roadmap

This is a proposed dependency order only. No implementation is initiated by this report.

| Step | Scope and dependency | Exit evidence |
|---|---|---|
| 0. Review this assessment | Confirm single-club scope, two leaders, slice boundaries, target audience and acceptance metrics; resolve old PRD conflicts | Agreed slice and explicit deferred features; staffing-based estimate replaces planning ranges |
| 1. Establish a reference season | Reuse current headless/application runners; record three baseline approaches, cash/cover/exposure outcomes and end-to-end persistence costs | Reproducible baseline plus a financially credible scenario; no change to release horizon |
| 2. Prove staff behavior in a thin increment | Add saved manager/director identity and minimal policy inputs; alter existing selection/recommendation functions only where needed | Controlled staff-swap scenarios show different sensible behavior within 2–3 weeks of starting Option B; stop and reassess if infrastructure consumes that interval |
| 3. Add mandates and safe authority | Requires staff policy identity; separate new commands from investment switch, target individual decisions, account for pending commitments, recheck on resolution | Routine execution plus one meaningful escalation, exact accounting, no authority bypass on load, preserved existing functionality |
| 4. Connect consequences and reviews | Uses actual staff decisions and recorded execution; tune exposure, expectation/demand and staff response; add bounded appointment/continuity costs | Three-system chains and competing recommendations; staff disagreement is intelligible and does not deliberately sabotage competent delegation |
| 5. Adapt the existing Desk and People screens | Uses structured mandates, risks and outcomes; group routine renewals, prioritize decisions, preserve deep reports | Keyboard-accessible season flow, durable acknowledgement, clear risk reasons and useful drill-down; no wholesale redesign |
| 6. Validate the vertical slice | Steps 1–5 integrated; run matched policies, save regressions and first player cohort; iterate once | Acceptance table measured, failures documented, independent review and a decision whether to deepen or change the loop |
| 7. Deepen the economy | Only after loop evidence; bounded sale offers, rival strategic investment/trading, more meaningful demand and financial distress response | Academy-sale and competing-rival stories arise from rules; surplus/shortage diagnostics improve without hidden resets |
| 8. Extend career stages and storage together | Profile target horizon; isolate/archive historical data and manage retained checkpoints as required; add retention/succession and later ownership outcomes | Durable end-to-end long-career runs, recoverable histories, controlled performance and stage-specific player decisions before lifting the cap |
| 9. Prepare reviewed Steam scope | Reconfirm lending, valuation/exit, scenarios and progression against actual playtest demand; complete platform/export/onboarding/art/audio work | Clean-machine and platform acceptance plus human validation; historical diagnostics alone do not satisfy release gates |

Later stages should change the question: survival asks which obligations can be sustained; stabilization asks how to build resilience; promotion asks what risk to authorize; elite competition asks how to retain key people and sustain an expensive squad; institutional ownership asks how to handle succession and reinvestment; legacy asks when to distribute, sell or continue. Current tier movement supports the first transition. The later questions require new mechanics, not simply higher costs or another identical facility level.

## 9. Risks and open questions

| Risk or unresolved decision | Why it matters / next evidence needed |
|---|---|
| The loop may be financially legible but emotionally thin | No observed human playtest proves attachment, understanding or replay desire. Test the two-leader slice before broad content production. |
| Opening economy may make restraint unnecessary | Historical accumulation is substantial. Measure current three-policy scenarios and cash risk; do not label accounting correctness as economic balance. |
| Safe delegation may remove all tension, or bad staff may feel deliberately incompetent | Distinguish competence from different priorities. Competent delegates should obey authority and make defensible choices; disagreement should concern objectives and uncertainty. Test whether players can explain it. |
| Youth benefits may be too weak or delayed for one season | Training is delivered after 32 weeks and development settles annually; manager selection currently favors existing ability. Tune starting prospects and policy effects transparently; do not claim new intake proves a first-season academy loop. |
| New leadership salaries could be double-counted | Opening operations already includes three leadership salaries. Define migration and budget treatment before individual contracts are added. |
| Exact ability versus adviser estimates | Decide how much information players should know. Keep payments/contracts exact; uncertain assessments need stable saved observations and a clear reliability model. |
| Authority flexibility versus owner fantasy | Decide minimum autonomy and materiality thresholds. Allowing every approval threshold to reach zero would recreate operational micromanagement; hiding all detail would undermine diagnosis. |
| Mandatory annual locks and decision handling | Replacing the allocation lock requires safe cumulative budgets and explicit decision resolution first. Otherwise “more freedom” can introduce duplicated commitments or skipped approvals. |
| Prototype save support | Are all schemas 1–23 promised import support for the public demo? Current migration support is not a production promise. Decide historical staff defaults and future-rule changes explicitly. |
| Long-career population, capacity and demand balance | The old pilot failed the declared population target at two boundaries and had long shortages; schema-23 capacity is not measured at 50 seasons. Do not generalize a single sample or extrapolate compact encoding as a complete fix. |
| Product scope precedence | The old PRD includes debt, valuation/exit and broader content; this request treats additional ideas as candidates. Confirm the next milestone independently of historical release aspirations. |
| Team and schedule | Staffing, available calendar, budget, intended price and release quality bar are unknown. The estimates compare paths; they are not a launch forecast. |
| Platform and usability | Current artifacts show development-PC Windows execution. Steam integration, clean-machine support, accessibility outcomes and first-time-user comprehension remain separate gates. |

## 10. Final recommendation

Proceed with **targeted evolution of the existing game** after the findings are reviewed. The implementation has already solved enough difficult problems to make a rewrite wasteful: authoritative accounting, reproducible seasons, persistent player identities, fallible transactions, safe saves, and an owner-oriented interface all directly support the refined vision.

Concentrate the next playable milestone on choosing people, setting authority, and understanding interacting consequences. Reuse the Owner Desk as the Chairman's Desk; preserve Football, Business, People, Club and History as inspection surfaces. Add fewer mechanics with more causal connections.

Require evidence of a compelling one-season loop before broadening the economy, and require measured storage/economy/population evidence before extending the career horizon. That sequence preserves the current investment while testing the actual product risk early.

### Final validation and independent review

Fresh Release solution build: **passed, zero warnings/errors**. Fresh xUnit execution after that build: **281 passed, zero failed, zero skipped**, reported duration 3 minutes 18 seconds. Logs: [build][build-log] and [tests][test-log]. These checks establish regression-test execution, not the proposed product acceptance criteria. No fresh graphical smoke run, human playtest, Steam check or 50-season durability run was performed.

A separate subagent inspected the actual assessment, underlying source, task requirements and sampled historical evidence. It found no actionable substantive issues; its requested wording precision about weekly autosaves was applied and rechecked. Final validation wording was independently verified against the build and test logs. All 45 source/evidence/log reference targets were checked to exist. Product risks and unverified release gates remain as described above; there are no outstanding substantive review findings.

At the original assessment, only this assessment document was added to project content and ignored validation logs were generated. The subsequent publication also includes the task pack and copied evidence described above. No gameplay, tests, configuration or architecture changes were made as part of this assessment/publication, and implementation has not begun.

[model]: ../src/FootballTycoon.Core/Model.cs
[factory]: ../src/FootballTycoon.Core/WorldFactory.cs
[rng]: ../src/FootballTycoon.Core/WorldFactory.cs#L23
[session]: ../src/FootballTycoon.Application/GameSession.cs
[session-commands]: ../src/FootballTycoon.Application/GameSession.cs#L90
[query]: ../src/FootballTycoon.Application/GameSession.cs#L136
[vault]: ../src/FootballTycoon.Infrastructure/LocalSaveVault.cs
[vault-write]: ../src/FootballTycoon.Infrastructure/LocalSaveVault.cs#L27
[desktop-project]: ../src/FootballTycoon.Desktop/FootballTycoon.Desktop.csproj
[godot]: ../src/FootballTycoon.Desktop/project.godot
[headless]: ../src/FootballTycoon.Headless/Program.cs
[endurance]: ../src/FootballTycoon.Endurance/EnduranceRun.cs
[ci]: ../.github/workflows/build.yml
[acquisition]: ../src/FootballTycoon.Core/Acquisition.cs
[finance]: ../src/FootballTycoon.Core/Finance.cs
[forecast]: ../src/FootballTycoon.Core/Finance.cs#L81
[calendar]: ../src/FootballTycoon.Core/Calendar.cs
[seasons]: ../src/FootballTycoon.Core/Seasons.cs
[pyramid]: ../src/FootballTycoon.Core/Pyramid.cs
[cups]: ../src/FootballTycoon.Core/Cups.cs
[matchday]: ../src/FootballTycoon.Core/Matchday.cs
[market]: ../src/FootballTycoon.Core/RecruitmentMarket.cs
[free]: ../src/FootballTycoon.Core/FreeAgents.cs
[rivals]: ../src/FootballTycoon.Core/RivalRecruitment.cs
[contracts]: ../src/FootballTycoon.Core/Contracts.cs
[retirement]: ../src/FootballTycoon.Core/RetirementLifecycle.cs
[player-history]: ../src/FootballTycoon.Core/PlayerHistory.cs
[academy]: ../src/FootballTycoon.Core/Academy.cs
[development]: ../src/FootballTycoon.Core/PlayerDevelopment.cs
[facilities]: ../src/FootballTycoon.Core/Facilities.cs
[main]: ../src/FootballTycoon.Desktop/Main.cs
[design]: ../src/FootballTycoon.Desktop/Main.Design.cs
[decision-ui]: ../src/FootballTycoon.Desktop/Main.Decision.cs
[prd]: PRODUCT_REQUIREMENTS.md
[persistence-tests]: ../tests/FootballTycoon.Tests/PersistenceTests.cs
[simulation]: ../src/FootballTycoon.Core/Simulation.cs#L9
[clone]: ../src/FootballTycoon.Core/Model.cs#L408
[compact-data]: assessment-evidence/compact-journal-measurements.json
[pilot-data]: assessment-evidence/fifty-season-summary.json
[pilot-analysis]: assessment-evidence/fifty-season-analysis.json
[inbox]: ../src/FootballTycoon.Desktop/Main.Design.cs#L209
[desk-image]: assessment-evidence/owner-desk.png
[renewal-image]: assessment-evidence/renewal-terms.png
[build-log]: assessment-evidence/build.log
[test-log]: assessment-evidence/tests.log

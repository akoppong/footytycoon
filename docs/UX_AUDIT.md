# Gameplay and UI/UX audit

Audit of `main` at `a7dbec5` (3 November 2026). Static review of the Godot UI in `src/FootballTycoon.Desktop` (`Main.cs`, `Main.Design.cs`, `Main.Decision.cs`, `DecisionChart.cs`, `SeasonChart.cs`, `Theme.tres`), the application loop in `GameSession.cs`, and the season rules in `docs/`.

**Limits of this audit.** Godot is not installed in the review environment, so the game was not launched and nothing was seen rendered. Everything below is read from code and docs. The "feel" claims (pacing, emptiness) are inferences from the loop structure and should be confirmed in a 30-minute playthrough with a stopwatch before anyone builds on them.

## Verdict

Your instinct is right, and the cause is structural rather than cosmetic. The simulation is deep (720 fixtures, ratings, cards, injuries, contracts, development, academy). The player touches almost none of it. The game has a good *decision screen* and almost no *game* around it.

Three root causes, in order of weight:

1. **Too few verbs.** The player makes roughly four meaningful choices a season, then clicks Continue.
2. **No visible consequence.** Choices pay off weeks or months later, in a form the player rarely sees (a number in a review card).
3. **Presentation is text and numbers.** No crest, faces, ground, motion, sound, or celebration. Wins, promotions and signings land with the same weight as a rejected form.

## 1. Gameplay: why it reads as bland

### 1.1 The player's verbs per season

| Moment | Choices | Source |
|---|---|---|
| Season opening capital plan | 1 of 4 (hospitality, forward search, training, keep reserve) | `Main.Decision.cs` `OpeningOrder` |
| January window | first choice, lower cost, or keep squad | `Main.cs` `Football()` |
| Season-end renewals | toggle renew/release per expiring player; accept or decline academy intake | `Main.Design.cs` `ContractChoices`, `AcademyChoices` |
| Anytime | owner cash injection (a button for up to £250k, capped at the personal reserve) | `Main.cs` `Business()` |

About 30 owned-club matches a season are simulated with no input. Lineups and tactics belong to the manager, by design (`DESIGNER_BRIEF.md` puts tactics out of scope). That is a defensible product call, but it means the 20 to 25 weeks between decisions (and 13 forced Continue stops a season) have to carry the game through drama and attachment. Currently they don't.

### 1.2 Choices look alike and resolve invisibly

- The four opening plans are compared almost entirely by cash curve. Their real differences (hospitality demand, a forward's goals, youth development) are uncertain and arrive 28+ weeks later.
- Nothing tells the player in the moment that the choice mattered. A review card appears in the inbox; there is no "your forward scored the winner" or "hospitality opened, first sell-out".
- Restraint ("keep the reserve") is the safest option and plausibly the most cash-positive, though this is unmeasured. If `PreserveReserve` is also the dominant strategy, the opening decision is a trap for fun. The headless runner defaults to it. **Check this with the strategy/scenario balance samples that `IMPLEMENTATION_STATUS.md` lists as a required gate.**

### 1.3 Time passes in a dead loop

- `Continue` uses `AdvanceTarget.NextDecision`, but `GameSession.AdvanceAsync` also breaks on `"Monthly review"` (`world.Week % 4 == 0`, `Simulation.cs:68`). That is about 13 stops a season, most with nothing to decide. The inbox text even says "No unresolved questions. Quiet weeks can be skipped." Yet the stops exist. `CALENDAR.md` confirms reviews are every four weeks, not tied to calendar months.
- Matches are resolved in the same batch as everything else. The player never *watches* a result arrive. They read the latest one in a card.
- Between decisions the player's only action is Continue (or "Advance one week"). That is the textbook shape of a waiting simulator.

### 1.4 No stakes you can feel

- The headline measure is cash. Promotion, relegation, a cup run, a derby, a star player's form do generate data but have no distinct moment. The "Ravenswick" rival appears once, in a paragraph on the Club tab.
- The brief asks for "recognizable crest, ground, and identity", "selective executive/player portraits", and "major milestones deserve distinctive, skippable treatment". None are implemented: no image or texture assets are displayed anywhere in `src/FootballTycoon.Desktop` (the only texture call is the smoke-test screenshot capture), no audio, and `Main.cs:275` states "No animation or audio is produced" (reduced motion is hard-wired on instead of being a setting).
- Characters (Mara Ellis, Jonas Reed, Callum Price) are fixed names on text. They never disagree with the player, react to a decision, or appear twice with different information.

### 1.5 Recommendations (gameplay)

Ordered by impact over effort. Items 1 to 3 are cheap and use data the simulation already produces.

1. **Stop the empty monthly stops.** Make `NextDecision` stop only when there is a real decision, a fixture the player cares about (cup tie, derby, promotion/relegation clincher), an injury to a key player, or a cash risk. Keep the monthly digest as an inbox item, not a halt. Expected effect: Continue becomes meaningful instead of a metronome.
2. **Add a "moment" layer.** Reuse `Moments`, `PlayerOfMatch`, `Events`, `Reviews` to raise discrete, skippable beats: a goal-by-goal match ticker for chosen fixtures, a signing announcement, a first hospitality sell-out, a promotion/relegation screen, a cup-final card. Text with a tween and a sound cue is enough to start.
3. **Close the loop on each commitment.** After a plan is chosen, show a running "Did it work?" tile on Owner Desk: forward's goals/ratings, hospitality receipts vs original forecast, training exposure. `Comparison()` already computes cash vs forecast; extend it per commitment kind.
4. **Add the missing verbs, smallest first.** Ticket policy, sponsor choice, player sales and supporter protests are already in `PRODUCT_REQUIREMENTS.md` as future work. A one-off manager instruction before a big match (`play it safe` or `go for it`) is a new idea from this audit and sits uneasily with the brief's no-tactics scope (see section 4); a rival poaching a player is likewise new. Even three small recurring levers would roughly double the decision count.
5. **Make rivalry and identity concrete.** Surface the rival's fixtures, head-to-head, a derby flag in the header's "Next match", and a crest placeholder per club.
6. **Re-test the opening decision for a dominant option** (see 1.2) before polishing it further.

## 2. UI/UX audit

### 2.1 What works (keep)

- Clear mental model on the opening screen: Needs-you inbox, central reading pane, "At a glance" rail. It matches `DESIGNER_BRIEF.md`.
- Honest money handling: separate personal/club cash, explicit "NOT YET COMMITTED", "Time is held", blocked reasons shown next to disabled buttons.
- Accessibility groundwork is better than most prototypes: keyboard focus following scroll, three text scales, remappable advance key, non-colour cues.
- The hero decision screen (`AllocationCards`) is the strongest design in the build: dark panel, one expanded plan with its own cash path, collapsed siblings.
- Monetary figures carry their basis and the original forecast is retained for later comparison.

### 2.2 Findings

Severity: **H** blocks the intended experience, **M** hurts comprehension or polish, **L** minor.

| # | Sev | Finding | Evidence | Fix |
|---|---|---|---|---|
| 1 | H | Everything outside the opening decision is a stack of `Label`s in cards. Squad, forecast, obligations, history and finance are paragraphs, not tables or visuals. | `Business()` writes one label per four forecast weeks; `People()` writes each player as a two-line string; `History()` repeats full `Fact` stacks per decision. | Real tables for squad (sortable: role, age, ability, wage, contract, form) and fixtures. Charts for forecast. Use `TableFigure` and the `GridContainer` already used for standings. |
| 2 | H | Dev status and scope notes leak into the player-facing UI. | Club tab: "Development scope: one acquisition, three seasons… remain in development." People: "Appointments are fixed in this prototype." Football: "Only drawn fixtures contribute…". | Move to a Settings → About/Roadmap page. In-game text should speak as the club, not the build. |
| 3 | H | No club identity or imagery. The most emotional parts of the game (your club, your ground, your players) are plain text. | No image or texture is displayed anywhere in the UI code (only the smoke-test screenshot capture touches a texture); one hard-coded paragraph describes the club. | Generated or placeholder crest, kit colours (already in the text: navy and lime) applied to header accents, a ground illustration on the Club tab. |
| 4 | H | Navigation does not match the brief's six workspaces and hides the league. | Tabs are Owner Desk / Club / Football / Business / People / History. The league table and fixtures live under "Football" beneath the recruitment and cup cards. | Promote "League & World" to its own tab with table, fixtures, cup bracket and rival form. Rename "Business" to "Finances". |
| 5 | M | Three persistent columns squeeze the reading pane at 1280×720. Inbox 218 px + rail 258 px leaves about 800 px, and the rail repeats header cash. | `BuildShell`; `Pane(body, …, 218)` and `(…, 258)`. | Collapse the rail into the header or make it toggleable below 1500 px. The cash numbers appear in the header, the rail and the chart. |
| 6 | M | Inbox rows are one multi-line `Button` with `\n`-joined tag/title/source, so the hierarchy is faked in one string and colour/weight cannot vary. | `BuildInbox.Item`. | Use a small custom row (tag chip, title, meta) and an unread/seen dot. |
| 7 | M | Match reports are long text blocks. Home goals and cards are interleaved prose; line-ups are two plain columns. | `MatchCard`. | Scoreline header, goal timeline strip, two compact line-up tables with rating colour, collapse cards/injuries into icons. |
| 8 | M | "Continue →" label and behaviour change across five states (Choose a plan, Time is held, Review purchase, Review next season, Continue). The disabled reason lives only in a tooltip. | `BuildShell` advance button. | Show the reason inline under the button, and name what Continue will do ("Continue to Sat 14 Nov: cup round of 32"). |
| 9 | M | The season chart legend is a single run-on caption and relies on colour naming: "NAVY Actual / dashed base RED DASH Downside AMBER … GREY DOT …". | `BuildSeasonLine` (the caption asks for size 10 but `Caption()` clamps to 12). | A real legend with colour swatches rather than colour names; label the minimum point on the chart itself. |
| 10 | M | Small text. The Save/Load/Settings buttons and the chart toggle are 11 px base before text scaling. (`Caption()` clamps its calls to 12, so captions are not affected.) | `Main.Design.cs:167` and `:198`, `AddThemeFontSizeOverride("font_size", 11 * …)` | Floor body and control text at 12 to 13 px at 100%. |
| 11 | M | Reduced motion is hard-coded, not a user setting, and there is no audio at all. The brief requires independent music and effects controls. | `Main.cs:275`. | Add motion/music/effects settings, even if the first build ships a few cues. |
| 12 | M | Hard-coded names and strings in presentation code. "Stonebridge", "Mara Ellis", "Jonas Reed", "Callum Price" and club prose are literals in UI code (also `Main.Decision.cs`), not read from the view; the core hard-codes some of them too. | `Main.cs` Club/People; `Main.Design.cs`. | Source from view/content so a second club or scenario does not require UI edits. |
| 13 | L | Contract toggle buttons use long sentences that flip meaning ("Renew X: 2 years at £… · switch to release"). | `ContractButtonText`. | A two-state segmented control per player with the recommendation shown as a badge. |
| 14 | L | The default Open Questions copy for empty state reads as filler, and "Show filed reviews" is a plain button in the inbox footer. | `BuildInbox`. | Hide the section when empty (the brief says leave the space empty). |
| 15 | L | Header packs identity, next match, two cash figures and the main CTA in one row; the CTA has a 155 px minimum width; at 150% text the crowded row may reflow (not verified, since the game was not run). | `BuildShell`, `CustomMinimumSize = (155, 0)`. | Let it size to content with a minimum. |

### 2.3 Skeptic's and marketer's view

- **Skeptic (genre player):** "I bought a club, and I don't pick a single player, set a tactic, or watch a match." The ownership fantasy only works if owner decisions have *visible, personal* consequences. Right now they have audited ones.
- **Marketer:** a screenshot of this game today is a beige form. There is no image that sells it. The one strong frame is the dark "Tonight is the money" hero. Build outward from that: crest, a night-match palette, a goal-moment card.
- **Premortem (assume it fails at launch):** reviewers say "it's a spreadsheet with a fixture list". Likely causes, from this audit: no moments, no personality, a possible dominant keep-cash strategy (unmeasured), 13 empty Continue stops per season, and a UI that explains the build instead of the club. Each maps to a recommendation above.

## 3. Suggested sequence

1. Play a full season with a stopwatch and note every click and every wait (confirms or corrects this audit; no code).
2. Gameplay quick wins: 1.5 items 1 to 3 and UI findings 2, 8, 14.
3. Tables and layout: findings 1, 4, 5, 6, 7.
4. Identity pass: crest, kit colours, rival, finding 3, audio and motion settings (11).
5. New verbs (1.5 item 4), chosen after step 1 and the balance samples.

## 4. Open questions for the owner

- Is "no tactics, no lineups" a fixed principle, or can a light manager-instruction lever exist if it stays an owner-level decision?
- How much of the roadmap in `PRODUCT_REQUIREMENTS.md` (loans, valuation, sale offers) is meant to supply the missing stakes, and when? Several of those would add verbs on their own.
- Is there art budget, or should the identity pass use generated placeholders?

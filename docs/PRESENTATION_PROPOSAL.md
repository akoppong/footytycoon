# Presentation proposal: from prototype shell to a game people want to reopen

Proposal · 25 September 2026 · Scope: PRD H1–H5, G7 presentation, G10 identity, G11 stories. This proposal does not change simulation rules or balance.

## Summary

Football Tycoon's simulation is honest, but players don't yet experience that honesty as a game. Today the interface is mostly prose panels. It has no crests, sound or motion, it shows the cash forecast in Business as a list of text lines, and some screens show developer notes to the player. The fastest way to improve it a lot is to **show the club and its consequences in pictures, not paragraphs**, while keeping the one-click, table-first speed that management players expect.

The main lesson from the market comes from Football Manager 26 (November 2025). The most established game in the genre rebuilt its interface around tiles, widgets and pop-ups. At launch in November 2025 its Steam rating fell to Mostly Negative, the series' lowest. The complaints focused on extra clicks, wasted screen space, league tables and statistics buried several screens deep, and no UI scaling. Among the most welcomed patches were ones that made the interface *faster*, such as an option to turn off or slow transitions. So we should be **dramatic in identity and feedback, and conservative in navigation and data layout.**

Recommended sequence: (1) remove friction and developer notes, (2) build a component layer and give the club a visual identity, (3) replace text lists with charts and tables, (4) add matchday and story presentation, (5) add sound, motion and ceremonies, with accessibility built in at every step. Each phase can be shipped, measured and tested with players on its own.

## 1. What other games teach us

### Recent pain points

| Game (year) | What went wrong | Lesson for Football Tycoon |
|---|---|---|
| **Football Manager 26** (2025) | The new UI was described as clunky, cluttered and inefficient. "You have to click a lot more to get the info you need." Information was split across smaller widgets and tile menus that waste screen space. Tables and statistics were buried several screens deep behind pop-ups. There was no UI scaling. Its launch Steam rating (Mostly Negative) was the series' lowest. | Keep **one-click access** to the table, cash and squad. Don't replace dense tables with decorative tiles. Scaling has to work at launch. |
| **Football Manager 26 patches** (2025–26) | The most praised fixes were speed: much less lag, an option to turn off or slow UI transitions, and reworked navigation. | Every animation needs an off switch, and every screen should appear instantly. Speed is part of presentation. |
| **Football Manager inbox** (FM19–FM26) | Inboxes flooded with dozens of irrelevant transfer notices, and filters reset every day. | Limit how many messages the inbox can hold. Group related messages. Never let routine news crowd out a decision. |
| **Football Manager 25** (cancelled, Feb 2025) | Sports Interactive said the UI was unfinished when it cancelled the release after moving to a new engine. | A big-bang UI rebuild is the riskiest option. Change the interface in stages that can each ship. |
| **Cities: Skylines II** (2023–) | Notifications "talk back" in ways that are more annoying than helpful, and the same warnings repeat. Tutorial panels cover the information they explain. | Give each alert a reason to exist, stop repeating it once it's acknowledged, and never cover the evidence with help text. |
| **Victoria 3** (2022–) | Information is buried under windows, layers of tooltips and dropdowns. Tooltips explain something but give no way to act on it, and players couldn't find the control they used earlier to undo a decision. | Every explanation should link to the action it describes. Commitments need one obvious place to revisit them. |
| **Frostpunk 2** (2024) | Objective text scaled with the setting, but tooltips, dialogue and tutorial boxes kept thin fonts that were hard to read. | Scaling must reach *every* piece of text, including tooltips, chart labels and dialogs. |
| **Anno 117** (2025) | Reviewers found some statistics missing, such as resource flows between regions. It was praised for letting players switch serif fonts to sans-serif and set how long notifications stay on screen. | Offer a sans-serif body option and a setting for how long notices stay visible. Don't hide the numbers players need for planning. |
| **EA FC 26 Career Mode** (2025) | Repeated broadcast intros and commentary, lineup graphics that put players on the wrong side, and "visual fatigue" over a long career. | Presentation that repeats every week becomes wallpaper. Vary it or keep it short, and make sure it always matches the data. |
| **Football Chairman Pro 2** (closest competitor) | Players complain of scripted losing streaks and a game "aimed directly against the player". Presentation improvements such as kits and manager interactions were welcomed. | Our strength is transparency. The UI should *prove* results aren't rigged by showing the evidence, not just asserting it. |

### What players praise

| Game | Why it works | What we take from it |
|---|---|---|
| **Crusader Kings III** | Nested tooltips: highlighted terms open explanations that contain more highlighted terms, "like having the wiki beside your cursor". Victoria 3 adopted the same system. | A nested glossary for financial terms (reserve, downside, headroom, arrears). This fits the PRD rule to "explain concepts where they matter". |
| **Two Point Museum** (2025) | Reviewers called the UI "efficiently clean": lots of information without cluttering the screen, well-considered menus, and a UI scale option. | Clean doesn't mean sparse. Group information by what the player is trying to do. |
| **Balatro** (2024) | The scoring is simple arithmetic, but layered feedback (count-ups, sound, small shakes) makes it satisfying. The gap between a spreadsheet and a game is closed by feedback design. | Our money is arithmetic too. A restrained count-up, a sound cue and a clear before/after can make committing money feel weighty. Tone it down for a boardroom setting. |
| **Motorsport Manager** | Off-track preparation pays off in a race day that feels like a payoff. | Matchday is where money becomes visible. It needs a short, vivid moment that doesn't change outcomes. |
| **Out of the Park Baseball** | History and records make long careers feel immersive. | The club's own history (seasons, records, remembered decisions) is our long-term reason for attachment. |
| **Against the Storm** | Highlighting shows at a glance which building can produce the material you're looking at. | Cross-highlighting: hover a commitment and its effect lights up on the cash chart. |

### Accessibility baseline in 2025–26

- Steam now shows accessibility tags on store pages and in search (16 tags), including UI scaling, contrast and colour options. Those tags help people find the game.
- Industry guidance puts UI scaling up to about 200% alongside a 4.5:1 text contrast ratio. The Game Accessibility Guidelines list text size, colour-blindness, remapping and subtitles as the most common complaints.
- Steam Deck Verified requires text of at least 9 px at 1280×800, and 12 px is a safer target. Small text is a common reason submissions fail. The PRD targets Windows, but Deck is where this genre is often played.

## 2. Current state of the presentation

This review is based on the source, not screenshots. The UI is built in code (`src/FootballTycoon.Desktop/Main.cs`, `Main.Design.cs`, `SeasonChart.cs`, about 1,060 lines, plus the smoke probe in `Main.Smoke.cs`).

**Strengths to keep**
- Navy and ivory palette, serif headings, Archivo body text and IBM Plex Mono numbers ([DESIGN_UPDATE.md](DESIGN_UPDATE.md)). The palette and typography stay.
- A cash chart that runs through the season with base and downside forecasts, the reserve threshold, results markers and keyboard inspection. It's the strongest part of the interface.
- An inbox with a reading pane and a final-terms step before committing, plus separate labels for "what is decided" and "what awaits". These are the right structures.
- Keyboard focus, remappable Continue, and 100/125/150% text sizes.

**Problems**

| # | Observation | Evidence | Player effect |
|---|---|---|---|
| P1 | Developer notes appear in the player's UI. | "Prototype scope: …" (`Main.cs:100`). "Appointments are fixed in this prototype… future systems" (`Main.cs:179`). "This milestone supports three seasons" (`Main.Design.cs:316`). "Three-season milestone" in the status bar (`Main.cs:46`). The status banner prints raw enum names such as "PROTOTYPECOMPLETE" and "LOSTCONTROL" (`Main.Design.cs:172–173`). | This breaks immersion and makes the game look unfinished. It belongs in the docs, not the club dossier. |
| P2 | Most workspaces are prose, not structure. | Business prints the 52-week forecast as one text line every four weeks (`Main.cs:165`), repeating in prose what the season chart above it already shows, and the chart stops at season end, so the rest of the 52-week horizon exists only as text. The squad is a list of labels (`Main.cs:180`). The Club screen is hard-coded paragraphs (`Main.cs:95`). | Players have to read to find answers they should be able to spot, which is the opposite of the "find the main risk in 30 seconds" goal (PRD H2). |
| P3 | No club identity. | There are no crests, kit colours or ground imagery. The club colours "navy and lime" exist only as text. | Nothing to get attached to. PRD G10 and H4 ask for 48 crests and club colour sets. |
| P4 | The match report is a text log. | `MatchCard` (`Main.Design.cs:535`) lists moments, cards and line-ups as labels. There's no pre-match card, timeline or visual score. | Matchday, the moment money becomes visible, has no emotional peak. |
| P5 | No sound or motion at all. | "Reduced motion: always on in this build. No animation or audio is produced." (`Main.cs:255`) | Decisions and results get no feedback, so the game feels like a form. |
| P6 | Text scales, but layout does not. | Text sizes and the chart's font and height scale with the setting, which is limited to 100/125/150% (`Main.cs:35`, settings at `Main.cs:247`). Padding, the 170 px button minimum width (`Main.cs:299`), navigation margins and the chart's fixed plot margins do not scale. Panel widths change only at exactly 150% (`Main.Design.cs:183–185`), and the three-column plan grid exists only at 100% (`Main.Design.cs:354`). | Larger text squeezes into fixed spacing. Anything above 150%, which is common in 2025–26 games, would need layout work. |
| P7 | The whole screen is rebuilt on every render. | `BuildShell` frees and recreates every node (`Main.Design.cs:121`). | Transitions, count-ups and preserved scroll or focus are hard to add. This blocks feedback work. |
| P8 | Nothing to show over a long career. | History is a reverse list of cards (`Main.cs:185`). There are no records, trophies or timeline. | Weakens the OOTP-style long-term attachment the PRD relies on. |

## 3. Design pillars

1. **Evidence first, beauty second, never beauty instead.** Every visual element shows real data from the immutable `GameView`. Nothing is decorative placeholder dressed up as information (this continues the rule in DESIGN_UPDATE).
2. **Two clicks to any fact.** The table, cash position, squad and next fixture are always one click away. Details open in the reading pane, not in stacked pop-ups. This is the FM26 lesson.
3. **The club is a character.** Crest, colours, ground and named people appear wherever the club is mentioned, and the club's accent colour runs through the interface.
4. **Weight belongs to commitments.** Big feedback (count-ups, sound, a moment of ceremony) is reserved for owner decisions and milestones. Routine weeks stay quiet (PRD H5: "ordinary wins do not require modal rewards").
5. **Speed is a feature.** Every animation can be skipped, turned off and interrupted. A screen that appears instantly beats one that animates in.
6. **Accessibility by default.** Every change ships with scaling, contrast and non-colour cues. It's never saved for the end.

## 4. Proposals

Effort is relative: **S** is under a week, **M** one to three weeks, **L** more than three weeks for one developer. "Data ready" means the current `GameView` already has what's needed.

### A. Remove friction (Phase 1)

| ID | Proposal | Why | Data ready | Effort |
|---|---|---|---|---|
| A1 | **Remove developer notes from the player UI.** Move prototype disclaimers to docs or a single "About this build" line in Settings. Replace raw status names in the banner with written phrases. Unimplemented systems simply don't appear in the UI. | P1 | Yes | S |
| A2 | **Upgrade the header into one status strip:** club cash, headroom above the club reserve target (not shown today), league position and W/D/L form chips with letters as well as colour (today in the season line), and the next fixture (already in the header, adding a crest). Each item opens its detail with one click. Personal reserve moves to a secondary position. | Pillar 2 and the FM26 lesson. This consolidates pieces that already exist: the header shows club cash, personal reserve and next fixture, and the season line shows position and form. | Yes | S |
| A3 | **Inbox discipline:** at most three decision items are visually dominant. Any others are listed, counted and one click away, and never hidden (PRD H3 and H5). Related items are grouped into one situation (injury, replacement proposal and cash effect together, as PRD H5 asks), and a separate "news" digest never outranks decisions. Acknowledged notices don't repeat, but they stay in a history. | FM spam, Cities: Skylines II alert fatigue | Partly: grouping rules are needed | M |
| A4 | **"Where did I commit that?"** Every active commitment gets a permanent row in a Commitments list (Business) that links to its original terms, current effect and next review date. | The Victoria 3 lost-control problem | Partly: `DecisionRecord` has no review week and `Obligation` has no link back to its decision, so the read model needs both | M |
| A5 | **A command palette (Ctrl+K) and global shortcuts** for the six workspaces (1–6), in addition to the existing Tab focus order. Continue can currently be bound to a digit (`Main.cs:37`), so rebinding must reject keys already used by these shortcuts. | Fewer clicks for experienced players. Keyboard-first players are core to this genre. | Yes | S |

### B. Component layer and retained UI (Phase 2, enabling work)

The UI is currently built from `Label(...)` calls rebuilt on every render (P7). Before adding visual work, introduce **reusable Godot scenes that are updated in place, not rebuilt**:

- `MoneyFigure`: an amount with its basis (cash now, per week, per year or total, as H3 requires). It can count up from the old value to the new one, and is instant when reduced motion is on.
- `ClubBadge`: crest, short name and colour stripe, at three sizes.
- `PlayerChip`: initials or portrait, name, role icon, rating or ability, and availability icon (injured, suspended).
- `DecisionCard`: recommendation, two or three options, cost, uncertainty, deadline and accountable executive, following the H3 decision-card rule.
- `Sparkline`, `FormChips`, `DeltaBadge` (a +/- change with an arrow and sign, not colour alone), and `Glossary` text with nested tooltips.
- A `Theme` token file holding the semantic colours: navy, ivory, amber focus, warning, positive, negative and club accent.

Alongside it, write a one-page **art direction guide** (shape language, line weight, palette rules for club colours, icon grid) with a single named owner, so crests, grounds, icons and any portraits look like one game.

This is plumbing work, but it's what lets animation, scaling and consistency spread through every screen without a rewrite. It also makes the smoke tests less brittle, because they can target components rather than label text. **Effort M. Don't skip it:** without it, every later phase multiplies the problems in `Main.Design.cs`.

### C. Club identity (Phase 2)

| ID | Proposal | Detail | Effort |
|---|---|---|---|
| C0 | **Club identity data.** | Today `ClubSummary` holds only id, name, division and cash, and Stonebridge's town, colours and rival are hard-coded text (`Main.cs:98`). Add colours, town, founding line and rival for all 48 clubs to the world content and read model (PRD G10). Identity *traits* stay hidden until the supporter system uses them, so the UI never implies mechanics that don't exist. | M |
| C1 | **Generated crests for all 48 clubs**, drawn from a seed. | Shield shapes, divisions, one symbol from a small hand-drawn set (for example a gear or bridge for Stonebridge's engineering heritage), and the two club colours. Drawn as vectors in Godot, so they stay offline and scale cleanly. Meets the PRD H4 requirement for 48 crests with no per-club art. | M |
| C2 | **Club colour accent.** | The owned club's primary colour tints the selected navigation tab, the club's line on the chart and its row in the table, and fills the status-strip crest. Colours are checked against a 3:1 contrast floor and replaced with a safe darker shade if they fail. | S |
| C3 | **Ground card.** | A stylised stadium silhouette with stands as separate layers, so later facility upgrades can light up a new stand (supporting G9). Average attendance appears next to it. Capacity isn't modelled yet, so it's added only when facilities (G9) introduce it. | M |
| C4 | **Generated portraits** for staff and players. | Flat, abstract illustrated busts built from layered shapes (skin, hair, kit colours) using the existing `identity/{club}` random stream. Consistent and non-photographic, so there's nothing to license. Staff get hand-tuned variants. PRD H4 requires reusable portrait components, so choosing initials only (Decision 2) would need a PRD change. | L |
| C6 | **First-impression acquisition screen.** | The PRD H1 opening (0–3 minutes) and the most likely store screenshot: crest, ground card, town line, price, and personal versus club cash shown as two clearly separate `MoneyFigure` blocks. | M |
| C7 | **Remaining PRD H4 art brief.** | 12 event-family icons, achievement icons, four facility illustrations with upgrade states, and Steam capsule and library assets. These are scheduled with the systems that use them (events, facilities, release), not in this plan's phases. | Later |
| C5 | **Club dossier replaces the hard-coded Club prose.** | Crest, founding line, town, rival (with the rival's crest), ground card and "since you arrived" numbers (seasons, promotions, cup runs, cash change). | S (after C0–C3) |

### D. Show numbers as pictures (Phase 3)

| ID | Proposal | Replaces | Effort |
|---|---|---|---|
| D1 | **Business cash chart:** the season chart already sits above every workspace. In Business, extend it past season end to the full rolling 52-week horizon, draw the base/downside band, and stack signed obligations underneath. Hover or keyboard shows the exact week. The text list becomes an optional "table view" toggle, which also serves screen-reader and export needs. | The forecast list at `Main.cs:165` | M |
| D2 | **Cash reconciliation waterfall:** opening cash, then receipts, wages, operating costs, capital projects and owner injections, ending at closing cash. One picture that answers "where did the money go?". | The text lines in the journal card | M |
| D3 | **Squad grid:** grouped by role, one row per player with a `PlayerChip`, age, ability bar, a contract-end bar coloured by urgency (with the end date written as text), wage, availability, and form from recent ratings. Sortable columns. Contract risks at a glance supports PRD H2's requirement that a deal can be judged without opening dozens of profiles. | The label list at `Main.cs:180` | M |
| D4 | **League table upgrade:** crests, form chips, promotion and relegation zone bands (as edges, not just colour), movement arrows since last week (needs the previous week's table in the read model), and the table filtered to the teams around yours. | The current grid | S |
| D5 | **Forecast honesty visual:** each figure in a proposal shows a small range bar (downside to base) instead of two numbers. Uncertainty becomes something you can see rather than a note (PRD principle 4). | Pairs of numbers in the proposal rail | S |
| D6 | **Cross-highlighting:** hovering or focusing a commitment highlights its cash effect on the chart and its row in obligations (the Against the Storm pattern). | Nothing (new) | M |
| D7 | **Nested glossary tooltips** for about 20 terms (reserve target, downside, headroom, arrears, eligible revenue, wage ratio, fee ceiling, and so on). They open on hover or focus, can be pinned, and can be turned off in settings. Critical warnings never depend on them. | "Explain at the decision" prose | M |

### E. Matchday as the weekly payoff (Phase 4)

Everything below uses the recorded `MatchResult`: `Moments`, `Events`, line-ups, ratings, `PlayerOfMatch`, attendance and receipts. PRD G7's presentation brief already specifies this. We are designing the shape of it.

- **E1 · Pre-match card** (about 3 seconds, or instant): both crests, league positions, form chips, unavailable players shown as chips, stakes ("win moves you into the promotion places"), and venue and date. Expected competitiveness is shown only as a qualitative band, never as a win percentage.
- **E2 · Timeline ticker** (15–30 seconds, skippable, adjustable speed, "instant result" always available): a horizontal 90-minute bar with a live score. Goals (`Moments`) and cards and injuries (`Events`) are merged and land on it in minute order, with the player's chip and a short crowd sound cue. A 0–0 with no incidents shows the clock running to full time with the shot counts, then goes straight to the final card. The simulation has already resolved the result, and the ticker only reveals it.
- **E3 · Final card:** big score, shots, player of the match with rating, the line-up in grouped GK/DF/MF/FW rows with ratings as numbers and a shaded bar (no pitch diagram, because appearances record only a role group, not a formation position), attendance, receipts as a `MoneyFigure`, the change in league position, and new injuries or bans. The manager's quote stays.
- **E4 · Batching:** when Continue skips several weeks, results arrive as a single results reel (one row per match, and a click opens the full card), not N modals. Only matches that decide something or are marked as significant (cup ties, promotion or relegation six-pointers, and derbies once rivalries are modelled per C0) get the ticker automatically. A setting controls which matches get the ticker: all, key matches only, or none.
- **E5 · Variety guard:** the pre-match strapline draws from several templates chosen by context (derby, cup, run-in, winning streak) and avoids repeating in consecutive matches. This is the fix for the EA FC problem.

Effort: **L**. It's the most visible single improvement, because matchday is shown in trailers and it's where the PRD's "football makes money visible" promise is kept or lost. It follows Phase 3 because it reuses the charts and components built there. If an early trailer capture is needed, E1–E3 can move up to straight after the component layer.

### F. Story and memory (Phase 4; F5 in Phase 2)

| ID | Proposal | Detail | Effort |
|---|---|---|---|
| F1 | **Local newspaper front page** ("The Stonebridge Echo") at each monthly review: a headline, two sub-stories and a table snippet, all generated from recorded facts only (results, table moves, signings, commitments, cup draws). It's never used to invent offers or crises, which follows PRD G11. It's a light, varied frame for the monthly review, which is the natural session break. | M |
| F2 | **Season annual report:** a designed spread with final position (a week-by-week position chart only if weekly standings are stored first; DESIGN_UPDATE rejected an unsupported standing curve), cup run, the season's top performer, cash then and now as a waterfall, decisions made, each with its original forecast against actual result (PRD H5), and a closing line from the CEO. It replaces the plain season summary card. | M |
| F3 | **Club timeline in History:** a horizontal strip across seasons with decisions as markers, trophies and promotions as icons, and a line for league position. Clicking a marker opens the original terms. | M |
| F4 | **Records and callbacks:** club records (biggest win, top scorer, record attendance, longest-serving player). New events refer back to them where true ("his 50th appearance", "first promotion since your arrival"). The world keeps every result, but `GameView.Results` holds only the current season, so the read model needs career aggregates. This is presentation of stored data, not a new simulation system. | M |
| F5 | **Resume brief:** when a save loads, a single screen shows what changed, what's decided and what awaits, with the next fixture and cash position. It meets the PRD's under-a-minute target for restoring context, using the existing recap state. | S |

### G. Feedback, sound and ceremony (Phase 5)

- **Commitment moment:** confirming terms plays a short, weighty sound, the cash figure counts down to its new value, and the chart redraws from the old path to the new one. Under 600 ms in total and skippable.
- **Milestone ceremonies** for the PRD H4 moments (promotion, a beloved player's sale, opening a stand, accepting an exit) once those systems exist, plus a cup win. Relegation gets a quieter, sombre card that recognises what the owner protected rather than celebrating: a full-screen card with the crest, date, key numbers and one line from the CEO. It can be dismissed with any key, never shows more than once per event, and is logged in History. PRD H4 asks for this.
- **Sound set:** paper and pen sounds for decisions, a light UI click, crowd swells for goals (3–4 variants), an ambient office sound on the Owner Desk (off by default), and a short, unobtrusive music loop per phase of the season. Music and effects have separate volume controls (H3). Nothing uses licensed chants or voice.
- **Motion rules:** a 150–250 ms ease for panel changes, no bounce, and nothing that loops forever. Reduced motion (on by default until everything is tested) turns every animation into an instant change. There's also a separate "UI transitions" toggle, an option FM26 players widely welcomed.
- **Instant response:** pressing a button shows its pressed state within one frame. Busy states show a small inline progress indicator instead of disabling the whole screen without explanation.

Effort: **M** for the systems plus an asset budget for sound (a licensed commercial sound library or a contract sound designer).

### H. Accessibility and scaling (every phase, finished in Phase 5)

| Item | Target |
|---|---|
| UI scaling | 80–200% in steps of 10, applied to the whole interface (fonts *and* spacing, icons and chart margins). This goes beyond the PRD H3 bar of 100/125/150% and costs extra layout work, so it's built on the component layer (Phase 2). |
| Minimum text | 12 px at 1280×720 everywhere, including tooltips and chart annotations. 1280×800 is added if Decision 3 makes Steam Deck a target. |
| Contrast | 4.5:1 for body text and 3:1 for large text and UI graphics, checked automatically by a unit test on the theme tokens. |
| Colour independence | Every status has a shape or letter as well as a colour (W/D/L letters, arrows on changes, icons on warnings, pattern edges on table zones). A colour-blind palette preset is available. |
| Fonts | An option to switch body text to sans-serif (Anno 117 precedent) and a dyslexia-friendly spacing option. |
| Motion and sound | Reduced motion, a UI transitions toggle, separate music and effects volumes, and visual captions for crowd and alert sounds. |
| Input | Full keyboard support (as today), remappable shortcuts, and no action that only works on hover. Controller focus order only if Decision 3 opts in, since mandatory controller or Deck verification is a PRD non-goal. |
| Steam | Declare the supported accessibility tags on the store page when the features ship. |

## 5. Anti-patterns to avoid

- **The FM26 tile trap:** turning dense tables into cards and widgets that each hold one number. Tables stay tables. Make them better (D3, D4), don't replace them.
- **Pop-up stacks:** detail opens in the reading pane or a side sheet, and never more than one layer of modal.
- **Notification spam:** no notice without a decision or a meaningful change behind it (A3).
- **Invented drama:** the newspaper, ceremonies and tickers show only recorded facts. No fake near-misses, and no text that implies an unrecorded alternative would have been better (PRD H3 and H5).
- **Juice for routine:** no confetti for a Tuesday win. Save weight for commitments and milestones.
- **Scaling afterthought:** no new component merges unless it passes the capture checks at every supported scale (plus 1280×800 if Decision 3 opts in).
- **Big-bang rewrite:** FM25 was cancelled over an unfinished UI. Each phase ships to `main` on its own.

## 6. Challenging the plan

### Six Thinking Hats

| Hat | View |
|---|---|
| **White (facts)** | The simulation is solid and CI passed on `main` at the time of writing. There's no audio or motion, 0 crests, and developer notes in the UI. FM26's UI reception is documented. No player has tested the current build (see DELIVERY_PROGRESS "Product proof"). |
| **Red (gut)** | Opening the game right now feels like reading a well-written memo. A crest, a crowd noise and a scoreline ticker would make it feel like *my club* within seconds. |
| **Black (risks)** | Presentation work could hide an unproven core loop: a beautiful game that isn't fun is still not fun. Art and sound need budget and taste the team may not have. Generated portraits can fall into an uncanny, cheap look. More UI means more screenshot tests to maintain. |
| **Yellow (benefits)** | Presentation is what shows up in store screenshots and the first trailer (PRD B requires an owner decision and its consequence on screen). Clear charts directly serve the "30 seconds to find the main risk" gate. Accessibility tags widen the audience on Steam. |
| **Green (ideas)** | A "board pack" print view (PDF export of the annual report) for players who like to share. A photo-mode-style "club poster" at the end of a season. Club-colour kits in portraits. |
| **Blue (process)** | Put component plumbing (B) before visual work. Run the 5–8 player checkpoint test before Phase 3, then a short test of 5 or more players after each later phase against PRD M1 and M4 prompts. Stop and reassess after Phase 3 if comprehension doesn't improve. |

### Premortem: "It's a year from now and the presentation work failed. Why?"

1. **We polished before proving.** Playtests showed the ownership loop itself was dull, and six months of art didn't help. *Mitigation:* run a 5–8 player test of the current three-season build **before** Phase 3, alongside Phases 1–2. Those phases are cheap and worth doing anyway.
2. **It got slower.** Transitions and count-ups made every click feel sticky, and players turned everything off. *Mitigation:* a frame-time budget (under 16 ms per interaction at 1280×720 on integrated graphics), motion defaults tuned in testing, and a transitions toggle from day one.
3. **Art quality was uneven.** Generated crests looked fine but portraits looked cheap, and the result was worse than no portraits. *Mitigation:* ship crests and grounds first. Portraits only ship if a blind comparison with initials-only chips favours them. Otherwise keep initials, which is honest and clean.
4. **Scaling broke at 175%+ and nobody noticed.** *Mitigation:* extend the existing smoke screenshot probe to 200% and 1280×800, and add a layout-overflow assertion.
5. **The newspaper became repetitive or wrong.** *Mitigation:* template variety rules, a fact-only generator with unit tests comparing text against the recorded data, and a "last N headlines" repetition check.
6. **It drifted from the simulation.** A ticker showed a goal in the wrong minute, or the table flashed stale data. *Mitigation:* every presented number comes from `GameView`, and a test replays the headless strategies and compares presentation-model output with the recorded data.

### How others would see it

- **Marketer:** Phases 2 and 4 (identity and matchday) are the trailer. Get a 20-second "decision → matchday → cash consequence" capture as early as possible for the Steam page and wishlists.
- **Skeptic:** "Football Chairman already has kits and badges. Crests don't make us different." Correct. What makes us different is the **visible consequence of a commitment** (D1, D5, D6, F2), and that's what we should invest in most heavily.
- **Mentor:** Do fewer things fully. If budget forces a choice, pick A (friction), B (components), D1, D3 and D5 (honest charts) and E (matchday). Leave the rest for later.

## 7. Roadmap

| Phase | Contents | Exit check |
|---|---|---|
| **1 · Friction** | A1–A5, H (contrast test on theme tokens, non-colour cues) | No developer notes in the UI. Every workspace fact reachable in two clicks or fewer. Smoke captures pass at 100/125/150%. |
| **2 · Foundation and identity** | B (components, tokens and art direction guide), C0, C1, C2, C3, C5, C6, F5, full-interface scaling to 200% | Every club mention shows a crest. No `Label(...)` rebuilds in the converted screens. Scroll position and focus are kept on refresh. Smoke captures pass at 200%. |
| **Checkpoint** | Test with 5–8 players (PRD M1/M4 prompts) on the current loop and the Phase 1–2 UI | Can players find the main risk in 30 seconds and explain club cash versus personal cash? Do they report wanting to continue? |
| **3 · Honest pictures** | D1–D7, A4 read-model links | Players answer "where did the money go?" and "what happens if this goes badly?" without reading prose. |
| **4 · Matchday and memory** | E1–E5, F1–F4, C4 (conditional) | Match presentation is fully skippable and matches the recorded data in automated tests. Players recall a named player or moment (M4). |
| **5 · Feel and ceremony** | G, H finished, Steam accessibility tags | Interaction frame budget is met. Reduced-motion and transitions toggles work. Sound mix and ceremonies are tested for fatigue over a three-season run. |

Each phase follows the project's existing rules: an independent review, the smoke probe extended to the new screens, and screenshots at every text scale.

## 8. Decisions needed from the owner

1. **Art and sound budget:** in-house generated art only, or budget for a sound designer and an illustrator for crest symbols, grounds and ceremony cards?
2. **Portraits:** commit to generated portraits (C4), or decide now that initials-only chips are the house style? The second option needs a PRD H4 change.
3. **Steam Deck:** make 1280×800 and controller focus an optional target (affects H and every layout), or stay Windows desktop-first? Mandatory Deck or controller verification remains a PRD non-goal either way.
4. **Playtest timing:** agree to the checkpoint test before Phase 3? This proposal recommends it strongly.

## Sources

- Football Manager 26 reception and UI criticism: [PC Gamer](https://www.pcgamer.com/games/sim/football-manager-26-launches-straight-into-a-relegation-battle-as-steam-reviews-plummet-to-mostly-negative-been-playing-since-1993-and-this-is-the-worst-one/), [GameSpot review](https://www.gamespot.com/reviews/football-manager-26-review-back-to-the-drawing-board/1900-6418437/), [Operation Sports review](https://www.operationsports.com/football-manager-26-review-a-brilliant-game-trapped-in-a-clunky-shell/), [Forbes](https://www.forbes.com/sites/barrycollins/2025/11/08/football-manager-26-is-it-as-bad-as-the-steam-reviews-suggest/), [Game Rant](https://gamerant.com/football-manager-26-steam-reviews-mostly-negative/), [Football Gaming Zone](https://footballgamingzone.com/football-manager/football-manager-26-user-reviews-are-as-bad-as-expected/), [Wikipedia](https://en.wikipedia.org/wiki/Football_Manager_26)
- FM26 patches and community response: [Ingenuity Fantasy Football](https://ingenuityfantasy.com/feature-articles/the-football-manager-communitys-response-to-the-new-fm-26-patch-update/), [Operation Sports on 26.1.2](https://www.operationsports.com/fm-26-26-1-2-patch-brings-navigation-enhancements-and-stability-fixes/)
- FM26 portal and matchday features: [Football Manager official](https://www.footballmanager.com/fm26/features/where-storytelling-evolves-fm26s-match-day-experience), [The Escapist](https://www.escapistmagazine.com/football-manager-26-features/)
- FM25 cancellation: [heise online](https://www.heise.de/en/news/Switch-to-Unity-Football-Manager-25-falls-through-10273919.html)
- FM inbox spam: [Steam discussion (FM26)](https://steamcommunity.com/app/3551340/discussions/0/670600125430896649/), [Steam discussion (FM19)](https://steamcommunity.com/app/872790/discussions/0/3216031607501165869/)
- Cities: Skylines II notifications: [Gamecritics review](https://gamecritics.com/crackersoupblog/cities-skylines-ii-review)
- Victoria 3 information access: [Streams of Consciousness](https://streamsofconsciousness.blog/2025/02/24/victoria-3-has-the-worst-ui-ive-ever-seen/), [Paradox forum tooltip mod](https://forum.paradoxplaza.com/forum/threads/mod-more-informative-panel-tooltips.1620359/)
- Crusader Kings III nested tooltips: [PCGamesN](https://www.pcgamesn.com/victoria-3/nested-tooltip-system), [GameWatcher](https://www.gamewatcher.com/news/crusader-kings-3-tutorials-highlighted-text-encyclopedia)
- Frostpunk 2 readability: [Steam Deck HQ](https://steamdeckhq.com/game-reviews/frostpunk-2/)
- Anno 117 UI and readability options: [PC Gamer](https://www.pcgamer.com/games/city-builder/anno-117-pax-romana-review/), [Steam Deck HQ](https://steamdeckhq.com/game-reviews/anno-117-pax-romana/)
- EA FC 26 career presentation: [TrueAchievements](https://www.trueachievements.com/news/ea-sports-fc-26-career-mode-review), [EA Forums](https://forums.ea.com/discussions/fc-26-feedback-en/create-a-club-customization--career-mode-bugs--greater-long-term-immersion/13448109)
- Football Chairman Pro 2: [Metacritic](https://www.metacritic.com/game/football-chairman-pro-2/), [App Store](https://apps.apple.com/us/app/football-chairman-pro-2/id6499549945)
- Two Point Museum: [TechRadar](https://www.techradar.com/gaming/two-point-museum-review), [WellPlayed](https://www.well-played.com.au/two-point-museum-review/)
- Balatro feedback design: [Blake Crosley](https://blakecrosley.com/guides/design/balatro), [Indieklem](https://indieklem.substack.com/p/20-a-look-at-100-interface-games)
- Motorsport Manager: [GameSpew](https://www.gamespew.com/2016/11/motorsport-manager-review/)
- Out of the Park Baseball: [Operation Sports](https://www.operationsports.com/out-of-the-park-baseball-25-review-an-impressively-deep-managerial-experience/)
- Against the Storm: [TechRaptor](https://techraptor.net/gaming/reviews/against-storm-ambient-roguelite-city-builder)
- Accessibility: [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/basic/), [Xbox Accessibility Guidelines](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101), [Can I Play That? on Steam features](https://caniplaythat.com/2025/06/19/steam-adds-new-accessibility-features-in-beta/), [gHacks on Steam accessibility tags](https://www.ghacks.net/2025/06/13/steam-now-displays-accessibility-support-for-games-on-store-pages-and-search/), [GamingOnLinux on Deck text sizing](https://www.gamingonlinux.com/2026/01/steam-machine-verification-will-have-fewer-constraints-than-steam-deck-but-text-sizing-worries-me/)

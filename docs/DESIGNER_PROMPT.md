# Football Tycoon designer brief: identity and hero screens

29 September 2026 · Shared version: the "Football Tycoon designer brief" Claude doc. This file is the repository copy.

We need a visual identity and six hero screens for a football club ownership game, good enough that a single screenshot makes someone wishlist it. Section 1 is the prompt to hand over; the rest is the reference it points to. It supersedes [DESIGNER_BRIEF.md](DESIGNER_BRIEF.md) for presentation work and follows [PRESENTATION_PROPOSAL.md](PRESENTATION_PROPOSAL.md).

## 1. The prompt

Paste this to the designer or design tool as it stands; it carries the essential facts on its own. For a human designer, send sections 3–7 with it.

```text
Design the visual identity and six hero screens for Football Tycoon, a premium PC game where you OWN a football club rather than manage the team. You buy a struggling fictional club, decide where scarce money goes, and live with the consequences every Saturday. The player never picks line-ups or tactics; they choose investments, people and risk.

Goal: every screen we ship as a Steam screenshot must make a football fan stop scrolling and think "I want to run that club." Aim for the tension of a boardroom and the emotion of matchday in the same frame.

Mood: a modern broadsheet sports supplement crossed with a club's annual report. Confident typography, generous ruled layouts, club colours used boldly, numbers set like headlines. Crests, stadium silhouettes and a few illustrated moments carry the emotion. Premium and calm, never cluttered.

Facts: Stonebridge FC (navy and lime, River district, built by the town's engineering works; rival Ravenswick) starts in Division 2 of a fictional league of three 16-club divisions; top two are promoted, bottom two relegated, plus a 48-club cup. The owner pays £2.8m and keeps £2.2m personal reserve; the club has £1.4m cash and a £300k reserve target. Leaders: Mara Ellis (CEO), Jonas Reed (sporting director), Callum Price (manager, who picks the team). Seasons run 2026/27 to 2028/29; league Saturdays from 15 August to 15 May, cup final 22 May.

Keep from the current build: navy and ivory base, amber for focus and selection, a serif for headlines (Source Serif 4), Archivo for body text, IBM Plex Mono for aligned figures. You may refine, but explain any change.

Invent: a crest system that can generate 48 distinct fictional crests from shield shape, divisions, one symbol and two colours; a stylised stadium silhouette with separate stands; a component set (money figure with its time basis, club badge, player chip, decision card, form chips, range bar, sparkline, delta badge); a club-colour accent that re-themes the interface for whichever club you own.

Design these six hero screens at 1920x1080, all with real game content (see the brief):
1. "A club worth building": the acquisition screen for Stonebridge FC.
2. "The decision": three ways to use the club's £1.4m (a £650k hospitality build, a forward search up to £550k, or keep the reserve), with the cash forecast reacting.
3. "Saturday": the match final card, score, ratings, attendance, receipts, table change.
4. "The run-in": league table with promotion zone, form and the season cash chart.
5. "Promoted": a full-screen milestone card, restrained and earned, not confetti.
6. "The annual report": the season in one designed spread, original forecast against actual.

Rules: fictional clubs only, no real badges, kits, players or sponsors. Every number must be plausible from the brief's data. Screens must be buildable in Godot 4 with vector shapes, text and simple layered art; no 3D, no photographs, no player animation. People appear as initials chips, not faces. Never show a win probability. Body text at least 12px at 1280x720; 4.5:1 contrast; never use colour alone for meaning (win/loss, gains/losses, zones). Avoid the Football Manager 26 tile look: dense tables stay tables, one click to any fact.

Deliver: three mood directions first (one board each), then the chosen direction as an art direction guide, the component library with states, the six hero screens, a 1280x720 version of screens 2 and 3 at 150% text, and Steam key art: main capsule 1232x706, header 920x430, small 462x174, vertical 748x896, library capsule 600x900, library hero 3840x1240 (no text), library logo.
```

## 2. The game in one minute

You are the owner, not the manager: you control money, people and risk, and staff run the football.

- **Fantasy:** "I bought a club with potential, chose the investments that mattered, survived the consequences and built something worth owning."
- **Loop:** understand the club, choose a priority, compare proposals, commit money, advance time, watch results, review what happened against the original forecast.
- **Emotional arc:** conviction, commitment, anxious anticipation, then pride or regret.
- **Audience:** football fans who like tycoon and club-building games, and management players who'd rather build an institution than pick a team.
- **Platform:** premium, offline, single-player Windows PC game, played with mouse and keyboard at 1280×720 and up.
- **Not in the game:** tactics, line-up picking, playable or 3D matches, real clubs or players.

What sets it apart from Football Manager and Football Chairman is the **visible consequence of a commitment**: you can see the cash path bend when you sign, and see it again a season later. The designs should make that moment the star.

## 3. The six hero shots

Each shot is a real game screen that doubles as a Steam screenshot, so each must tell a story in three seconds with no caption.

| # | Shot | What the viewer should feel | Must show |
|---|---|---|---|
| 1 | **A club worth building** (acquisition) | "That club could be mine." | Stonebridge crest and ground, town line, the three amounts as the game labels them (asking price £2.8m from personal cash, club cash at takeover £1.4m, your reserve after purchase £2.2m), and the "Review acquisition terms" action |
| 2 | **The decision** (first allocation) | Weight of a real choice | Three options side by side (£650k hospitality build, £550k forward search, keep the reserve), the cash forecast band bending for the selected one, reserve line, accountable executive |
| 3 | **Saturday** (match final card) | The rush of a result that mattered | A Division 2 league match: big score with both crests, scorers and minutes, player of the match with rating, line-up ratings with a small "Selected by Callum Price" label (the owner doesn't pick the team), attendance, receipts, league position change |
| 4 | **The run-in** (league and season) | Pressure and hope | Division 2 table with promotion zone marked by edge and label, form chips with letters, the season cash chart with results along its axis |
| 5 | **Promoted** (milestone card) | Earned pride, not a slot machine | Crest, date (for example Saturday 15 May 2027, the final league day), final position and points, one line from the CEO, key numbers, dismiss hint. Restrained motion implied, no confetti |
| 6 | **The annual report** (season review) | "My decisions did this." | Final position, cup run, top performer, cash waterfall, each decision's original forecast against actual, what awaits next season |

Shots 3 and 5 carry the emotion; shots 2 and 6 carry what sets the game apart. If time forces a choice, finish 2 and 3 first.

## 4. Visual direction

Boardroom calm with matchday heat: the base is quiet and editorial, and the club and its big moments bring the colour.

**Keep**

- Navy and ivory base, amber for focus and selection, square ruled panels.
- Source Serif 4 for headlines, Archivo for body, IBM Plex Mono for aligned figures. All three are licensed for offline use.
- The season cash chart with base and downside forecasts, reserve line and results along the axis. It's the best part of the current build; make it beautiful, not different.

**Invent**

- **Crest system:** shield shapes, divisions and a small hand-drawn symbol set, generated from a seed into 48 distinct crests. Stonebridge's symbol should nod to its engineering-works and river-bridge heritage.
- **Club colour accent:** the owned club's colours tint navigation, its chart line, its table row and its badge. Stonebridge is navy and lime.
- **Ground card:** a stylised stadium silhouette with stands as separate layers, so later upgrades can light up a new stand.
- **People:** initials-only player chips by default. Portrait exploration is welcome as a separate option (flat, abstract, non-photographic).
- **Numbers as headlines:** money set large with its time basis (now, per week, per year, total), range bars for uncertainty, arrows and signs on every change.

**Avoid**

- The Football Manager 26 tile look: one number per widget, stacked pop-ups, tables broken into cards.
- Walls of red and green numbers.
- Casino energy: confetti for routine wins, flashing, loot-box reveals.
- Anything implying a real club, league, kit maker, sponsor or broadcaster.

**Reference points for mood only, not to copy:** broadsheet sports supplements, football matchday programmes, annual reports of well-run institutions, and the clarity of Two Point Museum's menus.

## 5. Real content to design with

Use these values so the screens could be real game states; invent only names and minor figures, and keep them plausible. Source: `src/FootballTycoon.Core/Content/opening.json`, the desktop UI and the dated calendar.

| Item | Value |
|---|---|
| Club | Stonebridge FC, River district, navy and lime, built by the town's engineering works; loyal local following |
| Rival | Ravenswick |
| League | Fictional country, three divisions of 16 clubs; Stonebridge starts in Division 2. Top two promoted, bottom two relegated, plus a 48-club domestic cup |
| Seasons | Real dates, 2026/27 to 2028/29, with a January transfer window |
| Leaders | Mara Ellis, CEO, cautious operator; Jonas Reed, sporting director, values immediate readiness; Callum Price, manager, selects the team |
| Acquisition | £2.8m price; owner keeps £2.2m personal reserve; club holds £1.4m cash; reserve target £300k |
| Opening choice | Hospitality build £650k, 28 weeks, £1k/week upkeep · forward search up to £550k fee and £2.5k/week wage · or keep the reserve |
| Annual income | Broadcast £1.3m · sponsor £520k · tickets about £1.05m · commercial about £312k · hospitality about £180k |
| Running costs | About £14k/week operations; new wage commitments must stay within 75% of recurring revenue |
| Key dates | League Saturdays 15 Aug – 15 May (Boxing Day included); January window approvals from the first Saturday in January; cup final 22 May; season review from 29 May |
| Division 2 opponents | Ravenswick, Thornbury Athletic, Upton Vale, Valechester, Westhaven City, Yewdale, Brackenfield, Copperton, Daleswick, Elmstead United, Foxborough, Greyford Town, Hartswell, Ivydale, Juniper Rovers |
| Division 1 (cup opponents or after promotion) | Ashford Athletic, Bellwick City, Cairnbridge, Dunmere Rovers, Eastport United, Fairhaven Town, Glenford, Highmoor Albion, Ironvale, Kingsford City, Larkspur Athletic, Millhaven, Northgate United, Oakbridge Town, Penwick, Queensmere |
| Match data | Score, shots, scorers with minutes and assists, cards, injuries, line-ups by role with ratings (6.5 is typical), player of the match, attendance, receipts |

The match engine records role groups (GK, DF, MF, FW), not formation positions, so line-ups are shown in grouped rows, not on a pitch diagram.

## 6. Rules

Steam only allows real gameplay in screenshots, so every hero shot must be a screen we can actually build. Illustration and drama beyond the UI belong in the key art and capsules.

- **Steam:** screenshots must [contain only gameplay](https://www.pcgamer.com/valve-tightens-restrictions-on-steam-store-screenshots/), with no concept art, marketing copy or awards. Capsules may carry [only artwork, the game name and an official subtitle](https://www.pcgamesn.com/steam/game-art-new-rules-valve), with no review scores or award logos.
- **Buildable:** Godot 4 UI with vector shapes, text and layered 2D art. No 3D, photographs, video or animated players.
- **Honest numbers:** every figure must be plausible from section 5. Never show a win probability, invented offers, or a feature the game doesn't have. Future-concept boards are welcome for internal review, but they never become hero shots or store screenshots.
- **Fictional only:** no real clubs, crests, kits, players, sponsors, leagues or broadcasters.
- **Readable:** body text at least 12px at 1280×720; [4.5:1 contrast](https://gameaccessibilityguidelines.com/provide-high-contrast-between-text-ui-and-background/) for body text, 3:1 for large text and graphics; layouts must survive 150% text and scale toward 200%.
- **Not colour alone:** win/draw/loss, gains/losses and table zones also carry a letter, sign, arrow or edge.
- **Fast:** every screen should read instantly; any motion you imply must be skippable and have a reduced-motion version.

## 7. Deliverables and sizes

Design files in Figma (or equivalent) with named components; crests and ground art as SVG so they can be rebuilt in Godot.

| Deliverable | Size or format | Notes |
|---|---|---|
| Three mood directions | One board each | Palette, type, crest sample, one hero shot sketch |
| Art direction guide | Short document | Shape language, line weight, club-colour rules, icon grid, do and don't |
| Crest system | SVG kit + 8 sample crests | Stonebridge and Ravenswick among them; rules to generate all 48 |
| Ground card | SVG, layered stands | Base state plus one upgraded stand |
| Component library | With states | Normal, hover, focus, selected, disabled with reason, pending, critical |
| Six hero screens | 1920×1080 PNG + source | Also serve as Steam screenshots (1920×1080, 16:9, as Valve recommends) |
| Scale check | Screens 2 and 3 at 1280×720, 150% text | Proves the layout holds |
| Main capsule | 1232×706 | Key art + logo |
| Header capsule | 920×430 | Must read at small size |
| Small capsule | 462×174 | Logo-led; the name must read at a glance |
| Vertical capsule | 748×896 | |
| Library capsule | 600×900 | |
| Library hero | 3840×1240 | Artwork only, no text |
| Library logo | 1280 wide and/or 720 tall | Transparent PNG |

Capsule sizes follow the [Steamworks graphical assets documentation](https://partner.steamgames.com/doc/store/assets); use Valve's current templates for safe zones.

## 8. Process, review test and open questions

Work in four checkpoints so a wrong direction is caught after a mood board, not after six finished screens.

1. **Mood directions:** three boards; the owner picks one or merges two.
2. **First hero shot:** shot 2, "The decision", in the chosen direction. It proves the chart, money figures and decision cards together.
3. **System:** art direction guide, crest system, ground card and component library.
4. **Full set:** the remaining five hero shots, the scale check and the Steam art.

**Review test for every hero shot**

- **Three-second test:** shown for three seconds, can someone say what the game is and what just happened?
- **Thumbnail test:** at 25% size, does it still read as a football club game with a clear focal point?
- **Real-state test:** could the game produce this exact screen from the content in section 5?
- **Craft test:** would it sit comfortably next to the store page of a top-selling management game?

**Open questions for the owner**

- [ ] Budget and deadline for the design work.
- [ ] Human designer, AI design tool, or both (for example, AI mood boards and a human for the final system)?
- [ ] Portraits: explore them now, or keep initials until the Phase 5 playtest?
- [ ] Logo and wordmark: in scope for this designer, or separate?
- [ ] Key art timing: commission the capsules now, or hold checkpoint 4's Steam art until the loop is validated? The product requirements advise validating in private before investing heavily in public promises.

## Sources

- [Steamworks graphical assets overview](https://partner.steamgames.com/doc/store/assets)
- [PC Gamer: Valve tightens restrictions on Steam store screenshots](https://www.pcgamer.com/valve-tightens-restrictions-on-steam-store-screenshots/)
- [PCGamesN: Steam game art cannot include reviews or awards](https://www.pcgamesn.com/steam/game-art-new-rules-valve)
- [Game Accessibility Guidelines: high contrast](https://gameaccessibilityguidelines.com/provide-high-contrast-between-text-ui-and-background/)

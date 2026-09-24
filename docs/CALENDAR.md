# Dated calendar

The game shows real dates instead of week numbers. The inbox, fixtures, chart, contracts, forecasts, saves and staff reports read "Sat 14 Aug" or "Sat 24 Jun 2028", and the header shows the season as "2026/27".

## How dates are derived

The simulation keeps its integer career-week clock for payments, obligations, forecasts and saves. `Calendar` in `FootballTycoon.Core` presents each weekly tick as a Saturday:

- Season one is 2026/27. Each season's week 1 is the first Saturday on or after 1 July; week 52 falls in late June.
- Every season re-anchors to July, so the calendar never drifts. The gap between one season's last tick and the next season's first is one or two weeks of close season.
- Career week 0 (the acquisition) is the Saturday before season one's week 1.

## Season shape

Weeks below are season weeks for the 2026/27 season. Later seasons move by up to six days because of the July anchor.

| Period | Weeks | 2026/27 dates |
|---|---|---|
| Preseason, opening capital plan and summer search (approve by week 2) | 0–5 | 27 Jun – 1 Aug |
| Domestic Cup preliminary round | 6 | 8 Aug |
| League, first half (15 rounds; international-break gaps at weeks 11, 16, 21) | 7–25 | 15 Aug – 19 Dec |
| League, second half (15 rounds, starting on Boxing Day; gaps at 32, 39, 43) | 26–46 | 26 Dec – 15 May |
| Cup: round of 32, round of 16, quarter-final, semi-final | 19, 28, 37, 42 | 7 Nov, 9 Jan, 13 Mar, 17 Apr |
| Cup final | 47 | 22 May |
| Close season and season review | 48–52 | 29 May – 26 Jun |

League and cup fixtures never share a week. The cup starts a week before the league, as in the EFL 2026/27 calendar.

## January transfer window

The midseason recruitment review is now the January window. Approvals open on the first Saturday on or after 1 January. The last approval day is the last Saturday whose following week still falls on or before 1 February, so every deal completes by 1 February. That gives three or four approval weeks, depending on how the Saturdays fall (2026/27: approve 2–23 January, deals complete by 30 January).

## Saves

Schema 6 adds `CalendarStartSeason`. New careers use the dated layout from season one. Older saves migrate in memory: their current season keeps the fixtures, cup weeks and week 24–27 window they were saved with, and the dated layout starts at the next season. Dates are shown for every season, including legacy ones.

## Not yet done

- Owner reviews still happen every four weeks, not on the first of each calendar month.
- Contracts still end at the season's final tick (late June), not on 30 June.
- The opening summer search closes in mid-July (week 2) rather than at the real 1 September deadline.
- A Saturday grid can place a festive fixture on Christmas Day (for example, 25 Dec 2027). Moving matches to other weekdays is future work.

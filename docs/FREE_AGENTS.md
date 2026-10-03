# Released players and director-led recruitment

Schema 13 (`free-agents-13`) keeps newly released players in an employable pool. A release preserves the person's identity and departure archive; their old wage and contract remain historical details, with no renewed payment obligation. Loading schemas 1–12 starts the pool empty rather than inventing careers for past departures.

## Owner decision

After choosing the season's capital plan, People can show one named recommendation from the sporting director. In the opening window (season weeks 0–2) and the existing January window, candidates must fill a role shortage or improve on the weakest current player in their role. Outside those windows only shortages qualify. A shortage uses the existing cover minimums: two goalkeepers, five defenders, five midfielders and three forwards. No approach is available in the last week, while another transfer or free-agent negotiation is pending, or with 22 players already contracted.

The director ranks shortages first, then ability, younger age and stable person ID. Proposed wages are the greater of the expired wage or half the division's standard wage, rounded to £10, with a £100 weekly floor. Annualized squad wages including the offer must fit the 75% revenue limit. The proposal shows the exact start/end dates, full wage commitment and cash forecast. No speculative resale or fee is included.

Deals end at an annual renewal review: the current season for players aged 30+, the second season for ages 25–29 and the third for younger players. The first season is partial. Approval creates a saved mandate and moves no cash. Next week the director rechecks availability, squad capacity, authority, wages, arrears and the downside reserve; an explicit reserve exception permits a nonnegative downside below the target. An outside-window shortage must still exist. Eligible players accept with a fictional 70% chance from an isolated saved stream. A failed approach adds no obligation, and the director waits four weeks from approval before approaching that person again.

A signing keeps the same identity, removes the person from the pool and starts the new wage obligation the following week. No club receives a sale receipt. The forecast charges only the quoted payment dates. The manager retains control of selection; signing does not promise appearances or improvement. Pending approaches and their outcomes remain intact after save/load.

## Aging and retirement

Free agents age and develop once at each season close under the existing player-development rules. They receive no invented training exposure or appearances. An uncontracted outfield player reaching 40, or goalkeeper reaching 42, retires with a named, dated snapshot of final age, role, ability and last club. A player already at the cutoff when released retires immediately. Retirement creates no payment and does not erase departure, academy or match history. These are fictional tuning rules, not claims about real-world retirement ages.

People shows available and retired former players who have a recorded departure from the owned club. Earlier departures remain historical records even when migration cannot establish their subsequent employment.

## Remaining work

This unit supports owner-approved free-agent approaches and retirement of uncontracted players. It does not implement rival offers, all-club replacement recruitment, competing bids, retirement during or at the end of a live contract, advance retirement announcements, or a complete player market. No live contract is shortened. The three-season boundary stays in place; the 50-season population gates in [the lifecycle plan](PLAYER_LIFECYCLE_PLAN.md) remain unproven.

Automated coverage includes release identity and expired wages, pure quotes, idempotent approval, successful/refused replay, first wage dates, veteran expiry and paid renewal, finite forecasts, changed affordability/rosters, shortages outside windows, both retirement cutoffs, migration and invalid identities. The optional desktop `--free-agent-smoke-test` with `--smoke-test --season-smoke-test` exercises the real recommendation/approval UI, two text scales, pending-save reload and negotiation outcome. See [delivery progress](DELIVERY_PROGRESS.md) for executed validation and package status.

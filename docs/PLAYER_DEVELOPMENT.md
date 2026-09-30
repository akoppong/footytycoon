# Annual player development

At each season close, every current player at all 48 clubs ages by one year. Ability changes settle before the sporting director prepares renewal recommendations. The People workspace shows age, current ability, an age-based outlook and the last recorded season's change. Closing reports retain each owned player’s name, age, before/after ability, recorded competitive appearances and explanation, even after release or transfer.

## Current rules

These are base rules before training bonuses and fictional balance assumptions, not claims about real player development. Ages below refer to age before the annual update.

| Age / role | Possible annual change | Policy |
|---|---|---|
| 16–23 | 0 to +3 | Growth chance is 35% plus one percentage point per recorded appearance, capped at 60%. Two points require a draw below 15%; three require a draw below 5% and at least 20 appearances. |
| 24–27 | 0 to +1 | Growth chance is 15% plus one percentage point per recorded appearance, capped at 30%. |
| Outfield 28–30; goalkeeper 28–33 | 0 | Holds current ability. |
| Outfield 31+; goalkeeper 34+ | 0 to −2 | Decline chance starts at 35%, increases ten percentage points per additional year and caps at 90%. A two-point decline becomes possible three years after the threshold on a draw below 30%. |

Ability remains between 1 and 100. Each player uses a separate saved `development/{id}` random stream. Clubs follow identical rules; promotion, owner spending, match presentation and loading do not supply an artificial boost. The count includes league and cup appearances this season, across clubs when a player transfers. Pre-schema-7 matches lack line-ups and contribute no invented appearances.

Reports list players on the owned club at the season close. A player sold or released earlier in the season has no entry; one who leaves afterwards keeps the recorded row and name. The reported outcome is a bounded simulation result. Appearance counts are evidence of exposure, not proof that the manager caused an individual's change. The age outlook is deliberately broad and makes no guarantee. Wages and contracts do not change during development; the later renewal proposal prices the updated squad.

## Persistence and scope

Schema 9 (`development-9`) adds immutable development rows to season summaries. Schemas 1–8 migrate in memory. Players retain their saved age and ability during migration; completed seasons keep empty development reports. An unfinished season receives its first update at its next close. Closing an already recorded season is idempotent. Original save files are never overwritten by migration.

This is an annual development foundation within the three-season milestone. Individual potential, staff effects, retirement, player morale and a balanced long career remain unimplemented. Newer source adds [baseline academy intake](ACADEMY.md); aging and intake alone do not establish a sustainable 50-season population.

## Validation

The focused tests cover every club aging at a real season close, renewal terms using the updated players, exact report retention, repeated close/save-load idempotence, deterministic replay and RNG isolation, bounded improvement/stalling/decline, pre-transfer and current-season exposure, departed-player history, schema-8 migration before and after season close, and malformed report rejection. Final run evidence is recorded in [delivery progress](DELIVERY_PROGRESS.md).

Schema 10 adds [training facilities](TRAINING.md). Actual weekly exposure can improve growth chances without changing the base age rules or random draws; season reports retain the bonus before the exposure counter resets.

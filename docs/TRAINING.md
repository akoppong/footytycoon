# Training investment

Training is a fourth annual capital plan alongside hospitality, recruitment and retaining cash. The owner authorizes construction; staff continue to select and train players. There are no lineup or training-schedule controls.

## Quotes and delivery

All values are fictional balance assumptions awaiting player and economy validation.

| Upgrade | Construction paid now | Build | Additional weekly upkeep after opening | Combined weekly training upkeep |
|---|---:|---:|---:|---:|
| Level 0 → 1 | £650,000 | 32 weeks | £1,000 | £1,000 |
| Level 1 → 2 | £800,000 | 32 weeks | £1,250 | £2,250 |
| Level 2 → 3 | £950,000 | 32 weeks | £1,500 | £3,750 |

The proposal shows the upfront payment, added weekly cost, completion date, commitment total and cash forecast. Upkeep and benefits begin the week **after** delivery. Upkeep is signed through the end of the following season, then renewed with other operating commitments at season rollover. Costs from earlier upgrades remain payable; the new quote adds to them. Training adds no forecast gate receipts or speculative player-sale proceeds.

Only one construction project may be active, and only one opening capital plan may be approved each season. The three-step limit, administration restrictions, cash affordability and downside reserve checks apply. An explicit reserve exception may cross the owner's target but cannot approve a negative downside cash forecast. Construction currently exposes a 40% recoverable value; cancellation and delays remain unimplemented, matching the existing hospitality boundary.

Club shows delivered facility levels and project dates. History retains the training decision and original cash forecast; the delivery review links to that original decision. At the season close, named development reports disclose the training bonus without attributing an individual outcome to it.

## Development effect

A full year at training level 1, 2 or 3 adds 10, 16 or 20 percentage points respectively to a younger player's growth chance. Improvements remain uncertain and ability stays bounded. Ages 24–27 receive half the bonus; older players receive none. Age is evaluated before the season-close age increment. Training does not reverse aging or directly change match odds.

Each week records weighted exposure using the club's delivered level: 10, 16 or 20 units. This happens after negotiations and before facility deliveries. A new signing trains at the buyer in the transfer week and retains exposure already earned at the seller. An upgrade never changes earlier weeks' contribution. Exposure represents time with the facility, including rehabilitation; injuries can still reduce the separate appearance-based growth component.

At the annual review, exposure divided by 52 gives the youth bonus, rounded down to whole percentage points; ages 24–27 divide by 104 instead. The report stores this value before the counter resets. For example, 32 weeks at level 1 plus 20 at level 2 yields 640 units, or 12 extra percentage points for a player aged 23 or younger. A level-1 opening project delivered in week 32 contributes only weeks 33–52, yielding three percentage points in its first season. A full year follows in season two if the player stays.

Higher levels have diminishing incremental benefits. An older squad or a short ownership horizon may make the investment wasteful. No economic return, star player or guaranteed promotion is promised. Individual potential, staff competence, conditioning effects, retirement and autonomous rival investment remain future work. Newer source adds [baseline academy intake](ACADEMY.md); admitted graduates use the same training exposure rules as other players.

## Saves and validation

Schema 10 (`training-10`) adds project kinds, training levels, saved exposure and the report's training contribution. Schema 9 saves migrate with level zero and no invented exposure; existing projects remain hospitality, and development history is retained. Older schemas continue through the existing migration chain. The new allocation is appended to preserve old serialized enum values.

Tests cover exact forecast/obligation agreement, no invented hospitality income, delivery and transfer timing, weighted upgrades, save/load replay, exposure reset and report retention, uncertain growth with unchanged RNG consumption, capital-plan/construction/level limits, operating renewal without duplicate charges, schema-9 migration and malformed state rejection. The desktop probe accepts `--smoke-test --season-smoke-test --training-smoke-test` to exercise the funded training path through all three seasons. Final execution evidence is tracked in [DELIVERY_PROGRESS.md](DELIVERY_PROGRESS.md).

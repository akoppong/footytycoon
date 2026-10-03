# Baseline academy intake

The inherited academy can supply zero, one or two named 17-year-olds at each annual renewal review. These are the existing youth pipeline's graduates, not accelerated returns from a newly purchased academy. Academy investment, multi-year pipeline reporting and links from an investment to a graduate remain future work.

Every club uses the same policy. A private stream derived from world seed, completed season and club determines cohort size, names, roles and ability. Ability ranges from 25 below the next division's standard to that standard (51–76, 43–68 or 35–60). Reopening, declining and reconsidering a quote cannot reroll the cohort or advance the world's saved random streams. No graduate is guaranteed to develop, start matches or attract a buyer.

The director recommends only as many candidates as all these limits allow, up to two:

- After the chosen contract releases, the squad has space below 22 players. This is an academy admission limit, not a new restriction on already-authorized transfers.
- The full squad's annual wages, including all recommended graduates, stay within 75% of eligible recurring revenue using the next division's broadcast and sponsor terms. No academy exception is made for an inherited wage overspend.
- Cash above the club's reserve target covers the first year's additional wages, cumulatively across the intake. Clubs with arrears admit nobody.

Each offer has no fee, a three-year contract and wages from the week after confirmation. The weekly wage is 20% of the next division's standard wage, rounded down to £10, with a £100 minimum: currently £540, £340 or £220. The quote shows each player's name, role, age, ability, wage and end date; it also shows annual academy wages and the complete three-year commitment. Those obligations enter the actual renewal forecast and the confirmed payment schedule. Confirmation itself spends no cash.

The owner can decline the entire intake and reconsider it before confirming. Contract overrides and accepting all contract recommendations preserve that academy choice. If the owner's forecast would go negative with new academy wages, confirmation is blocked until the intake is declined or funding resolves the shortfall. Existing renewal rules still allow inherited distress to carry forward; declining academy intake does not repair that underlying financial position. Rival admissions use the cash, wage, arrears and squad filters above; broader rival forecasting and rescue policy remain open.

Academy prospects do not relax the existing contract-release minimums. They are uncertain additions, not guaranteed role-for-role replacements. Rivals admit their director's recommendations at rollover; only the owner's intake can be declined by the user. After admission the manager selects graduates under the ordinary availability and ability rules, and they age, train and develop like other players.

Schema 12 (`academy-12`) retains immutable graduation snapshots and records the quoted intake in renewal history. People lists owned-club graduates with original age and ability. The archive keeps their identity even if they later leave. Schemas 1–11 migrate without inventing prior graduates, changing active squads, or altering cash or saved random states. Old previews are invalidated by the migrated world identity; an unconfirmed annual review may now recommend an intake.

This is a foundation within the three-season milestone. Schema-13 source adds a prospective free-agent pool, owner-approved approaches and uncontracted retirement; see [FREE_AGENTS.md](FREE_AGENTS.md). Contracted-player retirement, academy facility investment, guaranteed replacement coverage and the PRD's long-run population target are not implemented. The schema-12 Windows package `windows-a241ab2f` and `FootballTycoon-academy-windows.zip` include academy intake; the schema-13 additions require a newer executable. See [delivery progress](DELIVERY_PROGRESS.md) for validation evidence.

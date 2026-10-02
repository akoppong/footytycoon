# Retirement at contract expiry

Schema 15 (`retirement-15`) adds persisted retirement announcements for contracted players at every club. The fictional age policy is 40 for outfield players and 42 for goalkeepers. A player announces during the final 52 weeks of a signed contract when their projected age at expiry reaches that cutoff. Age still advances only at annual development. Longer contracts continue to their existing expiry; neither an announcement nor reaching the cutoff shortens a wage term.

People lists each owned player's announced and effective dates. An announcement is fixed: the director excludes the player from transfer recommendations, outstanding transfer negotiations recheck it, and renewal offers cannot reverse it. There is no random retirement decision to reroll by loading or reopening a proposal.

## Expiry and shortages

At an ordinary annual review, the final season's matches and development complete first. Time remains frozen with the player on the squad until renewal confirmation applies the fixed retirement departure. The contract list has a retirement row without a renewal/release toggle, and final terms disclose departing names, changed wage totals and expected cover after any proposed academy intake. At a midseason expiry or the final playable checkpoint, retirement completes after that week's matches and development. Previously overdue announced departures complete before another permitted weekly advance.

Mandatory departures may leave a club short, including without a goalkeeper. They cannot block renewal. Voluntary releases still obey minimum cover after retirees are excluded; an uncommitted academy choice cannot be used to bypass that release guard. Current shortages appear in People. The manager uses the existing nearest-role/short-team selection fallback. Directors may seek a budgeted free agent, but replacements, ability and affordability are never guaranteed. No emergency player or money is created.

Every wage due through the signed expiry remains due. Retirement creates no new payment and does not forgive arrears. The historical obligation remains saved with its original end date. Retired people leave active squads, never enter the employable pool, and retain named departure and final age/ability records. Existing match names, development reports and academy provenance remain available.

## Persistence and scope

Announcements retain person, club, contract, announcement date and retirement date. Schema-14 migration adds only currently warranted notices, in memory. It preserves current employment, obligations, journal and random states, without inventing past departures or changing the original snapshot. Retirement executes in the application's private transaction candidate; publication follows durable save success. Duplicate commands reuse their receipt.

The optional `--retirement-smoke-test` with `--smoke-test` uses an explicitly controlled age fixture: two goalkeepers retire at the first review, the owner declines intake, and a forward retires at the final checkpoint. It exercises the real desktop confirmation flow, two text scales, saved departures and shortage advance. The ordinary seed's opening ages do not reach the cutoff within three seasons. This fixture is correctness evidence, not a representative population or balance sample.

The three-season boundary remains. General transfer trading, probabilistic early retirement and the 100-world/50-season population gates remain open; see [the lifecycle plan](PLAYER_LIFECYCLE_PLAN.md). Executed validation and packaged build evidence are recorded in [delivery progress](DELIVERY_PROGRESS.md).

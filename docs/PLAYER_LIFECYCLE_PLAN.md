# Player lifecycle and longer-career validation

Status: lifecycle implemented in schema-18 source; longer-career validation remains open. The academy, departure archive, prospective free-agent pool, owner-approved approaches and uncontracted retirement are implemented; see [FREE_AGENTS.md](FREE_AGENTS.md). Rivals now make [budgeted free-agent replacement offers](RIVAL_RECRUITMENT.md). [Contracted-player retirement](CONTRACT_RETIREMENT.md) now preserves signed terms and allows unavoidable shortages. General transfer trading and the population gates below remain planned. The three-season boundary stays in place until the lifecycle and longer-run validation are implemented.

## Current constraint

Voluntary release recommendations preserve minimum cover, while announced retirement remains fixed even when a club is short. Academy roles weighted to cover requirements and zero-to-two annual graduates do not guarantee cover. The [coverage diagnostics](LIFECYCLE_COVERAGE.md) separate available supply from vacancies and retained offer outcomes. Budgeted free-agent approaches offer a replacement route; rival directors now [consider pending competition](RECRUITMENT_CONTENTION.md), but availability and affordability can leave vacancies. The next work must measure whether these policies sustain a credible population and economy.

The free-agent lifecycle, budgeted replacement decisions and retirement are implemented. Population validation is next. It must preserve the ownership role: the sporting director finds candidates and the manager selects the team. The owner reviews material commitments, rather than searching an unrestricted player database.

The isolated runner now [compares no-owner-recruitment with affordable shortage replacement decisions](OWNER_REPLACEMENT_DIAGNOSTICS.md), reporting owner and rival shortage weeks separately. This measures a disclosed simulated owner policy through ordinary approvals; it does not give the production director authority to spend without the player.

## Free-agent lifecycle

- On an actual contract release, retain the existing immutable departure event and move the named person into a separately modeled available-player pool. An expired contract's wage is historical evidence, not a continuing obligation or an agreed future wage.
- Active squads and the available pool must have disjoint person IDs. Joining a club keeps the same identity, previous departure events, graduate provenance and report names. A saved offer must never become a different person after reload.
- Apply aging to each living person once per season, whether contracted or available, using isolated saved streams. Free agents receive no invented training exposure or match appearances. Leaving professional football is an explicit retained event, not silent deletion to meet a population cap.
- Old departure records cannot reconstruct a current employable person: they lack intervening development and career evidence. Migration must not reactivate everybody who ever left a club or invent past contracts. New releases enter the pool prospectively.

## Replacement commitments

At the annual review, assess next-season role cover after existing contracts, proposed renewals, declared retirements and separately accepted academy offers. A prospect's role may count toward numeric coverage once a real contract is committed, but the director must still explain weak ability or lack of experience. Do not promise that every shortage has an affordable solution.

Use at most three owner-facing options, including keeping the existing commitments when viable, consistent with the transfer-market design. Each proposed replacement needs a named candidate, wage, duration, complete commitment, start date and explanation. Fees, if any, and signed wages must enter the same preview and journal rules as current recruitment. Free transfers have no sale receipt for the former club. Speculative resale cannot fund the deal.

Rivals follow the same cash, wage, availability and refusal rules. Process competing offers in a documented stable order, or a persisted competition mechanism, so one free agent cannot sign for multiple clubs. Recheck authority and affordability when a deal resolves. Rejecting or losing a candidate must not block advancing time; retain the unresolved shortage in the review and use the manager's existing fallback selection.

## Retirement

Define and disclose a fictional, role-sensitive retirement policy separately from ability decline. The first implementation should retire players at contract expiry and preserve every payment due through that date. Do not truncate a live contract merely to improve a club's cash or population count.

Uncontracted people need a separate annual exit check because they have no future contract expiry. Initial fictional tuning: an available outfield player aged 40 or above, or goalkeeper aged 42 or above, retires at the season-close review before new offers. Persist the effective date and retained event once; loading or reopening a quote cannot re-evaluate a completed decision. These age cutoffs are provisional balance rules to test against the population gates, not real-world claims. Any later probabilistic early-exit rule must also use isolated persisted randomness, never a quote-time reroll.

Persist an announced retirement once chosen, including its effective date. Reopening a quote, loading or switching owner choices must not reroll it. Contract recommendations must not offer a renewal beyond a declared retirement. Where the announcement provides time to act, surface the coming vacancy in People and the director's review. If no replacement can be signed, explain the remaining shortage; never silently create an emergency player or cancel the retirement to satisfy a minimum.

Distinguish unavoidable retirement from a voluntary release. The existing contract-minimum guard must not block a declared retirement or prevent advancing a short-handed club. Owner-chosen releases still respect the minimum, using the remaining squad after mandatory departures and any already committed replacements. A shortage is visible sporting risk; it is not authority for an unaffordable signing.

Retirement must retain the person's name, club history, academy origin and final age/ability without retaining a playable squad member or live wage obligation. The final season's match and development evidence remains available after retirement.

## Transaction and validation gates

The [isolated endurance runner](ENDURANCE.md) now provides per-club population and finance evidence beyond three seasons using the actual core. The playable limit is unchanged. A completed diagnostic sample does not satisfy the 100-world acceptance gate below.

Quote generation stays pure. Confirmation, expiry, entry to or exit from the available pool, signed obligations and historical events must be applied atomically to the private transaction candidate before durable publication. Duplicate receipts, failed saves and reloads must never duplicate a signing or retirement.

Required cases include a retiring goalkeeper with no affordable cover; an owned club declining an academy cohort; rival clubs competing for one candidate; a released graduate joining a rival; contracts spanning a tier change; a poor club already over the wage limit; a failure during persistence; and old-save migration without fabricated people. Check exact first and last wage dates, disjoint current identities, retained historical names and loaded-versus-uninterrupted replay.

Before extending careers, complete at least 100 varied world runs of 50 seasons and test the PRD's population target of 1,100–1,500 active people. Count contracted and employable free-agent people explicitly and track births, graduations, releases, transfers, retirement and departures. The [1,200-person opening world](OPENING_POPULATION.md) and academy policy are not evidence that the target is sustainable. Tune intake and market policy from measured runs while preserving the cap of two named graduates per club per year; do not hide depletion or accumulation by silently adding or deleting people.

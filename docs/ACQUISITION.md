# Acquisition negotiation

The existing Stonebridge opportunity now allows either an asking-price purchase or one lower counteroffer. This implements the short negotiation in PRD G2; it does not supply the other five scenarios, difficulty settings, debt or a complete valuation system.

## Owner flow

The opening brief shows the asking equity price, club cash, annual squad wages, inherited recurring obligations and personal reserve after purchase. The owner can enter a lower price in pounds and review it before choosing **Send counteroffer**. Sending uses the one attempt and saves an immediate accepted or rejected response. It moves no cash, creates no payment obligation and leaves the career in acquisition setup at week zero.

An accepted response makes that lower price available for a separate purchase confirmation. A rejected response leaves the original asking price available. The purchase preview shows the exact payment to the seller, remaining personal reserve and unchanged club cash. Only **Confirm and commit** transfers personal money to the seller and opens the first capital-plan decision. Club funding remains a separate owner injection after purchase.

The response remains visible after loading. The same command ID returns its original receipt without repeating the offer or purchase. An earlier purchase preview becomes stale when a counteroffer is submitted, so the owner must review the current price. A failed durable write leaves the live world and its attempt unchanged; retrying receives the same seeded response.

## Rules and persistence

- An offer must be positive, below the asking price and within personal funds. The desktop entry uses whole pounds; the core money model uses integer pence. Bounds are checked before mutation, including extreme integer inputs.
- The seller has a seeded minimum of 85–98% of asking, in whole percentage points. An offer at least that minimum is accepted. This is an initial bargaining rule, not a calibrated acquisition valuation model. The threshold is not disclosed in the interface. Higher offers have a better chance; no extra fee or artificial penalty is charged for rejection.
- A dedicated `acquisition/seller` random stream is consumed only by submission. Preview does not draw; football and other market streams are unchanged. Reloading the same state and amount produces the same outcome. Loading an earlier save creates the usual career branch, not a different seller threshold for that seed.
- The saved response, its one history entry, random state and any completed purchase payments are validated together. A confirmed purchase posts exactly two equal and opposite owner/seller journal entries at the agreed price. Neither entry touches club cash.
- Schema 22 (`acquisition-counteroffer-22`) validates schema 21 then changes only version markers. Earlier schemas continue through the existing migration chain. No past offer is invented and existing purchases retain their original payments. Old quotes must refresh after migration.

The asking price remains the single opening-content price. Multiple opportunities will need persisted scenario-specific terms and authored financial baselines; selecting six names alone would not fulfill the six-scenario requirement.

## Validation

Fifteen acquisition tests cover accepted/rejected paths, pure preview, separate confirmation, exact cash destinations, stale purchase quotes, duplicate commands, invalid and unaffordable amounts, independent random streams, corrupt responses/payments, opening and completed-career migration, failed-save retry/reload through the application worker, and advancing/funding/resuming accepted and rejected negotiated careers.

The opt-in desktop flag `--acquisition-smoke-test` exercises the real review, send and purchase callbacks for both outcomes. It checks editable price text, advance-key isolation, no payment on submission, persisted response and correct final price, with screens at 100% and 150%. Package and final validation evidence are recorded in [DELIVERY_PROGRESS.md](DELIVERY_PROGRESS.md).

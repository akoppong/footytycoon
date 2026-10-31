# E09 — Owner mandate player study protocol

Status: **Awaiting tested build and player evidence.** No participants have been recruited, no sessions have run, and no product gate has passed. Protocol preparation is independently reviewed; execution remains incomplete. On 28 October 2026 the owner confirmed that participants are still needed and requested study materials. Before any participant session, E08 must validate the playable revision and checkpoint with no unresolved invariant failure or scenario infeasibility. Either failure blocks product testing until a separately reviewed repair or scenario solution passes the affected checks and reruns. A tested build or a recorded E08 decision alone is insufficient. This protocol uses amendment A1 of the experiment contract; changing it requires a dated, reviewed amendment before recruitment, retaining prior versions.

## Participants, consent and setup

Use five adult volunteers interested in football management or business strategy, including at least two who have not used this prototype. Recruitment or contacting anyone requires the owner's explicit authorization; this protocol grants none. Use participant labels P01–P05 in shared notes. Record prior prototype use and relevant experience without collecting unnecessary personal details.

Before the session explain: participation is voluntary; stopping, skipping questions or declining a replay is acceptable; this tests the prototype, not the participant. Ask permission for written observation notes. Screen/audio recording is optional and requires separate explicit consent, including who can access it and the agreed deletion date. Do not record personal desktop content. If recording is declined, use written notes; distinguish exact quotes from paraphrases and interpretations. Resolve storage/access and retention with the facilitator before recruitment, and communicate them in consent. Do not commit identifying notes or raw recordings to the public repository.

Use the E08-tested build and an unchanged fresh copy of the registered primary E01B checkpoint (seed 2026, season two, week 52) for every participant. Record build commit, checkpoint SHA-256, migrated save identity, text scale and controls. The primary checkpoint was chosen by fixed seed order before treatment outcomes; do not substitute a favorable seed. Retain original saves. A technical pilot by the facilitator checks loading, controls, season pacing and replay timing only; it does not count among the five participants or produce product evidence.

## Fixed session window

Reserve 90 minutes: 10 minutes for consent/introduction/initial goals, 55 minutes for the season, 10 minutes for reflection, and 15 minutes reserved equally for the optional replay. The main task is the full season-two calendar and its closing review through week 104. Do not accelerate through choices for the participant, force approvals or extend one participant's replay opportunity to improve the count. Participants may stop at any time. If the season cannot finish within its window, preserve the save and record the reached week, consequences observed and incomplete season; do not pretend that a partial session is a full-season result. Keep the same replay offer and reserved window when the participant is willing and able to continue.

Before recruitment, the technical pilot must verify that the replay window allows a fresh checkpoint load, mandate choice and first ordinary selection or recruitment consequence without rushing. If it cannot, amend the duration for all five and obtain independent re-review before recruitment; keep the prior protocol and reason for amendment. During actual sessions, technical interruption or external time restrictions make an unavailable opportunity missing evidence. Fatigue and lack of interest are separate product observations, not exclusions that improve the denominator.

## Neutral script and tasks

Introduction: “You own a football club entering its second season. Its first season was prepared through ordinary decisions, including contract renewals and departures. Staff select the team and recommend recruitment; you make owner decisions. This prototype covers three seasons. Please use the information in the game to pursue a club aim that matters to you. There is no preferred strategy, and you can stop whenever you wish.”

Before discussing any results, ask and record: “What would you like this club to achieve this season? What do you expect your instructions to the manager and director to change, and what might they cost?” Do not supply the answer or describe the thresholds that will be scored.

Ask the participant to inspect the club, choose their mandates, and explain one opportunity cost they expect. Let them keep the default mandates if that is their choice. Let them play the remaining season and make ordinary owner decisions, including declining recruitment or changing mandates at permitted times. Do not prescribe the automated E08 comparison policy. Record actual commands, approvals, mandate changes and their timing. Do not introduce manual lineups, new scenarios or forced events.

Observe self-initiated inspections and reconsideration separately from prompted actions. After an unexpected match or recruitment result, ask neutrally: “What do you make of that?” If none is self-identified, ask the participant to choose a result to inspect; record that the inspection was prompted. If navigation assistance is necessary, provide it and record exactly what was explained. Never recode an explanation supplied by the facilitator as uncoached understanding.

At the closing review (or recorded stopping point), ask:

1. What did you ask each staff member to do? Which decisions still required you?
2. Show one selection example and one recruitment example. What did the staff do, what changed, what did it cost, and which part was uncertain?
3. What did a failed approach or unexpected match result tell you? What does it not establish?
4. Which choice mattered most to your original aim? Was any choice obvious or irrelevant? Did keeping money influence your decision, and why?
5. What would you keep or change next time, and why?

Offer the replay exactly once: “We have time reserved to try a different pair of mandates from the same starting point, or we can stop here. Either is equally useful to us. What would you prefer?” Do not attach a reward or suggest that replaying is the desired outcome. If chosen, load a fresh common checkpoint and let them select a different pair and advance to its first ordinary selection or recruitment consequence. Agreement, opening the save or viewing settings alone does not count as a replay start. Record actual changed mandates, first consequence and timestamp, or why that consequence was not reached.

## Observation sheet (one blank copy per participant)

| Field | Record |
|---|---|
| Participant label; prior prototype use; consent | |
| Build/contract revision; source checkpoint hash; migrated save; controls | |
| Session start/end; reached week and season review reached | |
| Pre-outcome club aim and expected tradeoffs (quote or labeled paraphrase) | |
| Initial mandates and rationale; later changes with week/reason | |
| Actual decisions, previews, approvals/declines and result links | |
| Uncoached explanation of each mandate and retained owner approval | |
| Selection example: originating mandate → actual player choice → observed consequence/cost/uncertainty | |
| Recruitment example: recommendation → approval/decline → signed/closed result, terms/cost/uncertainty | |
| Connection of traced tradeoff to stated aim; concrete reason to retain/reconsider | |
| Self-initiated vs prompted inspections/reconsiderations | |
| Confusion; navigation help; facilitator-supplied explanations | |
| Replay offered once; fair opportunity available; chosen pair; first consequence | |
| Technical interruptions; external time limit; fatigue; lack of interest (separate) | |
| Participant words vs facilitator interpretation (separate columns/paragraphs) | |
| Missing evidence and cause; follow-up question | |

A feasible recruitment opportunity deliberately declined is a valid choice and remains in the five-person denominator. Record its reasons; declining alone cannot satisfy delegated recruitment-outcome tracing. An approved approach that signs or closes, including refusal or a failed execution check, supplies a real outcome. A successful signing is not required. A genuinely unavailable feasible opportunity is missing evidence. Do not force approval or label a deliberate decline as indefinitely missing evidence.

## Evaluation and handoff

Use the unchanged A1 exploratory gates, with a fixed denominator of five: at least four explain both mandates and owner approval without coaching; at least three trace both real examples and their uncertainty; at least three connect their traced tradeoff to their pre-outcome aim and a concrete reason to retain/reconsider; at least three voluntarily start the different-policy replay defined above. Report counts and missing evidence separately. These are exploratory judgments, not statistical targets, retention proof or a release gate.

After E08 and sessions, append the evidence-backed decision to `OWNER_MANDATE_EXPERIMENT_RESULTS.md`, linking de-identified observation summaries and E08 technical results. An independent non-author reviewer must inspect the actual notes and scoring, not just the reported counts. No fabricated quotes, synthetic participants or agent playthrough may substitute for these sessions.

- Await evidence when sessions or genuine recruitment/replay opportunities are missing. Retain deliberate declines as observed choices and do not let them mask a revise/stop result.
- Repair technical failures or scenario infeasibility before drawing a product conclusion.
- Revise a named behavior/scenario when E08 finds insufficient choice or a confirmed dominance concern; revise evidence/UI for unclear explanations; revise the experience if clear tradeoffs are irrelevant to players' aims.
- With a technically valid, actionable five-person study and a fair replay opportunity for all five, zero voluntary replay starts recommends stopping this mandate approach. One or two recommends bounded revision. Failed comprehension, tracing or relevance criteria also require revision.
- Recommend another bounded experiment only when both technical and product gates pass. Name the smallest follow-up and why; do not automatically implement the conditional backlog.

Staff attachment, hiring/relationships, autonomous spending, long-term retention, balance outside these seeds and release readiness remain unproven. No populated observations currently exist; the current decision is **Awaiting tested build and player evidence**.

## Preparation review and prerequisite status

Independent non-author reviewer `e03_review` inspected the neutral script, consent/privacy, timing/replay opportunity, unchanged five-person criteria and decision rules. The review found and rechecked one material clarification: unresolved technical or scenario failures explicitly block sessions until reviewed repairs and affected reruns pass. The reviewer rechecked the final publication wording and prerequisite status; no actionable concerns remain.

This documentation-only preparation is permitted before E08. It uses the reviewed E00 A1 contract integrated at `df54a03fd4cd47786cb771f200bff8904dc98ac1` and E01B checkpoint gate `ca9614c65b3bc5d445d07c91be1a0946c9608e99`. E08's tested playable revision, a successful duration/replay pilot, consent/storage arrangements and five actual volunteers remain required before sessions. No participant evidence or product decision is supplied by this preparation; E09 execution is **Awaiting player evidence**.
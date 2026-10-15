# Checkpoint capacity diagnostics

The endurance runner now reports which top-level saved fields occupy the uncompressed checkpoint. This provides evidence for storage work after the [fifty-season pilot](FIFTY_SEASON_PILOT.md) reached roughly 417 MB. It does not change gameplay, save formats, retained history, production size limits or the three-season playable boundary.

## Measurement

The final `RunResult.CheckpointCapacity` contains total encoded bytes, envelope bytes and sections sorted by descending size, then ordinal name. Each section reports its property name, exact UTF-8 value bytes and the immediate item count when its value is an array. Nested arrays and objects belong to their containing value; an array containing a nested array counts that nested array as one item. Non-array counts are null, not zero. Value bytes include whitespace inside a container. Envelope bytes contain property names, root braces, separators and whitespace outside values.

Section bytes plus envelope bytes equal the existing `CheckpointBytes` exactly. These are uncompressed serialized bytes, not memory usage, compressed file size or durable-save acceptance. Field names and semantics belong to the measured schema; compare sizes alongside the recorded build and policy.

The scanner traverses the already encoded final checkpoint using `Utf8JsonReader`. It does not allocate another world or a JSON document, re-encode gameplay, consume randomness or prune records. It retains only section metadata. Malformed roots, incomplete JSON and duplicate decoded top-level property names are rejected rather than reported. Initialization failure leaves the report null because no checkpoint exists. Final reports for an incomplete run describe that partial checkpoint, not the requested horizon.

The existing `ElapsedSeconds` value is captured before this final scan, so it excludes the scan's cost. Weekly timing also excludes annual encoding and this final work. No performance acceptance claim follows from these values.

## Matched replay

A four-season / seed-2026 PreserveReserve/CoverShortages pair compares the exact source at `5ab7a0e` with that source plus the capacity diagnostic. Both use acquisition schema 22, ordinary renewal/intake and affordable shortage approvals. Neither changes the production academy rule.

The original runner was built from the retained source archive under `artifacts/checkpoint-capacity-baseline`, with assembly SHA-256 `058C18321FF2C7657ED3F40795619D1B51675FED4B8925303456355A82AD3480`. The measured runner is preserved under `artifacts/checkpoint-capacity-runner`, SHA-256 `77E723468169C65FA7EA98ABB8E68A291E860B353E4B9DAE86F2B28AF85CD79D`; its build identifies base `5ab7a0e193e75a937659bf7375c75a20758aa748` plus the then-uncommitted diagnostic changes.

Both runs complete week 208 with the same 34,550,723-byte checkpoint and gameplay SHA-256 `9E9C5DD4D267A47A82382C236F410BCB1B25F6F2D707FF4C1A5E73B292E5376C`. All 126 metric events match after excluding build/assembly provenance from the configuration. Every existing non-timing summary field also matches. The new report contains 42 sections and 647 envelope bytes.

| Field | Value bytes | Immediate array items |
|---|---:|---:|
| Journal | 26,440,222 | 244,546 |
| Results | 6,451,865 | 3,068 |
| RandomStates | 426,969 | Not an array |
| Obligations | 398,281 | 2,742 |
| Fixtures | 348,911 | 3,068 |
| Clubs | 169,269 | 48 |
| FreeAgents | 58,683 | 279 |
| Departures | 57,007 | 378 |

The journal accounts for about 76.5% of this particular checkpoint, with match results contributing about 18.7%. Journal storage is therefore the first candidate for further measurement and design. This four-season breakdown does not establish the same proportions in the existing fifty-season evidence, which predates this diagnostic. History, cash reconciliation, exact replay and saved identities must survive any later storage redesign.

Evidence lives in `artifacts/checkpoint-capacity-before` and `artifacts/checkpoint-capacity-after`, with matching progress logs. The read-only local comparison `artifacts/checkpoint-capacity-compare.py` produces `artifacts/checkpoint-capacity-comparison.json`. Those ignored artifacts are not part of a game save or the repository report.

## Validation scope

Eight focused cases cover exact UTF-8 measurement using a separate JSON-document oracle, nested collections, empty arrays, unchanged input, deterministic real-world output, and invalid or duplicate roots. Existing endurance tests also require the final byte reconciliation and a null report when world initialization fails. Production core and desktop files are unchanged, so the existing Windows package remains the playable artifact; this diagnostic does not require a new desktop export.

All 258 Release tests, the solution formatting check and the warnings-as-errors solution build passed, with zero build warnings/errors. The first targeted restore could not reach the package vulnerability feed from the sandbox; the rerun with network access completed successfully. Logs are `artifacts/checkpoint-capacity-targeted.log`, `checkpoint-capacity-full-tests.log`, `checkpoint-capacity-format.log` and `checkpoint-capacity-build.log`. Source/test review and independent comparison of the raw replay evidence found no actionable issues.

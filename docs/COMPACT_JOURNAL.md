# Compact journal storage

Schema 23 (`compact-journal-23`) stores each journal entry as six ordered JSON values instead of repeating six property names. Every transaction, exact amount, sequence, date, account, category and reference remains present. No entries are combined, pruned or reordered. Financial calculations, recruitment, academy rules and the three-season playable boundary are unchanged.

## Format and compatibility

Each journal row has this fixed order:

```text
[Sequence, Week, Account, Amount, Kind, Reference]
```

Sequence, week and cash category use 32-bit integers; amount uses the existing signed 64-bit integer pence. Account and reference are strings. The category is the existing numeric `CashKind` value. Rows must contain exactly six correctly typed values. Null rows, missing text, fractional or out-of-range integers, and extra values fail with controlled JSON errors. The writer rejects null rows/text too. Historical object rows remain readable, with null/missing account or reference rejected rather than allowing an unreadable subsequent compact save.

The converter belongs to `WorldCodec` options, not the in-memory ledger type. Default object serialization remains available for logical-state comparisons. `WorldCodec.Encode` retains object rows when explicitly encoding schemas 1–22 and writes compact rows for the current schema. Decode validates schema 22 before changing only version markers to schema 23; all older sequential migrations remain in place. It does not rewrite journal references, forecast IDs, command receipts, contracts, payments or recorded decisions.

Loading a legacy save leaves its source file unchanged. A subsequent successful write uses the current format and existing branch ancestry. Uncommitted old proposals are stale after migration; previously committed command IDs still return their saved receipts without applying the action twice. Malformed checksummed snapshots are rejected individually, leaving valid saves discoverable and source files intact.

Encoding and version changes necessarily alter new checkpoint hashes. New forecasts and proposals also derive identifiers from encoded state, so separate fresh runs across schemas cannot be claimed byte-identical. The losslessness comparison below encodes the **same schema-23 world** both ways, then requires every logical field and row to match after decoding. Historical identifiers already stored in a migrated world are preserved exactly.

## Measured size

A retained local measurement program advances seed 2026 using the diagnostic runner's ordinary PreserveReserve/CoverShortages policy, accepting recommended renewals/intake unless blocked and making no injections. It compares default object JSON with compact JSON at four observations. Both representations include the same schema-23 markers and gameplay values. The diagnostic runner has a different terminal boundary from the playable game; week-156 saves below are diagnostic snapshots accepted by the production vault, not evidence of a separately played production career.

| Week | Journal entries | Object JSON bytes | Compact JSON bytes | Object gzip bytes | Compact gzip bytes |
|---|---:|---:|---:|---:|---:|
| 0 | 0 | 460,461 | 460,461 | 77,461 | 77,461 |
| 52 | 66,479 | 9,293,608 | 5,570,784 | 1,479,139 | 1,387,670 |
| 156 | 187,813 | 26,309,282 | 15,791,754 | 4,112,484 | 3,854,671 |
| 208 | 244,546 | 34,550,678 | 20,856,102 | 5,390,275 | 5,056,126 |

At week 156 the uncompressed checkpoint is about 40.0% smaller. Its journal value falls from 20,191,193 to 9,673,665 bytes. The production `LocalSaveVault` wrote both representations into separate checksummed containers and read each back byte-for-byte: 4,112,820 object-format bytes versus 3,855,007 compact-format bytes, about 6.3% smaller. Gzip uses the same Fastest setting as the vault; repeated field names already compress well, so the compressed saving is much smaller than the raw saving.

Every measurement requires exact default-object serialization after compact decode and exact compact re-encoding. Independent comparison of the retained week-156 files verifies all world fields, all 187,813 journal rows in order, both container checksums and their exact decompressed payloads. Week-156 object SHA-256 is `C28ECEFE566EB24901E8414822E7DD141AF2A20B2B56B4F4D51684FE0F482A71`; compact SHA-256 is `00BE9CCB5B714184CEC73A6591D5621F6DE705FC193C2DE273688147DE594A5B`.

Source/method: `artifacts/compact-journal-measure/Program.cs` and `Measure.csproj`, using base commit `46f36f3` plus this schema/converter change. Results, raw week-156 JSON and both vault files are under `artifacts/compact-journal-measure-results`, with progress in `artifacts/compact-journal-measure.log`. These are ignored local validation artifacts. Existing evidence must not be overwritten; the measurement tool refuses an existing result directory.

## Validation and limits

All 281 Release tests pass. The 23 focused cases cover numeric bounds, Unicode/escaping, category and row preservation, empty/nonempty legacy migration, all-field equality, stale proposals, duplicate receipts, exact loaded-versus-uninterrupted progression, durable old/new saves, candidate-write failure and retry, and malformed-save discovery. Independent review corrected a fault probe that initially interrupted the recovery write; the final probe reaches and asserts the second write, where the candidate would be published. All 23 focused cases passed again after that correction. Existing migration fixtures now explicitly use historical object rows and compare full logical state, including unchanged history.

The warnings-as-errors solution build and both solution/desktop formatting checks pass. Logs are `artifacts/compact-journal-full-tests.log`, `compact-journal-targeted-final.log`, `compact-journal-build.log`, `compact-journal-format.log` and `compact-journal-desktop-format.log`.

The fresh Windows export `windows-e718398a` passed packaged headless execution and the rendered acquisition, three-season, development, rival recruitment, owner-depth and retirement probes with `DOTNET_ROOT` cleared. The graphical process exited zero with empty stderr. Three-season history at 100% and retirement terms at 150% were inspected under `artifacts/compact-journal-screenshots`. Export and graphical logs are `artifacts/compact-journal-export.log`, `compact-journal-graphical.log` and `compact-journal-graphical-errors.log`. The portable `FootballTycoon-compact-journal-windows.zip` contains 199 entries, including runtime files and three font licenses, and is 79,056,653 bytes. This is development-PC validation, not clean-machine acceptance.

The vault retains its 64 MiB uncompressed-world and 32 MiB container bounds. This change does not prove a fifty-season world fits either limit, reduce the count of in-memory journal objects, provide incremental storage or stop retained checkpoint generations accumulating. It is not a fifty-season durability, memory, controlled-performance, clean-machine or human-playtest acceptance result. Further journal storage and history-capacity work remains required before longer playable careers.

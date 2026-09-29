# Player departure records

Contract releases now retain a snapshot of the player's name, role, age, ability, club, departure week and reason. All clubs record releases; the People workspace shows the owned club's departure records. The player leaves the active squad and their expired wages stay ended. A history record is neither an active player nor a new financial obligation.

Reports resolve names from current squads, departure records and previously saved development, recruitment and renewal evidence. Current identities take precedence. Names with no surviving evidence remain unknown; the game does not guess who an old unnamed appearance belonged to.

Schema 11 (`people-history-11`) migrates schemas 1–10. For older saves, the owner's confirmed renewal decisions can reconstruct releases because they preserve the named player and actual choice. A recommendation to release that the owner reversed is not a departure. Rival releases from older saves cannot be reconstructed because they were not recorded. Migration preserves active players, cash, journals and random states, and leaves source files unchanged.

Departure entries are immutable snapshots. A later return to a club can coexist with an earlier departure record. Replayed command receipts and repeated save/load do not duplicate events. Invalid identities, clubs, dates, attributes and duplicate player/club/week entries are rejected when loading.

This is a continuity foundation for academy intake, retirement and longer careers. Those systems are still pending. There is no free-agent market or player re-signing interface in this unit, and transfer departures are not added to this contract-release archive yet. The three-season boundary remains.

Validation covers every club's actual releases, current squad removal, command idempotence, save/load identity, application read-model filtering and name retention, evidence-based schema-10 migration, current-name precedence, unknown-name handling and malformed records. The full desktop career smoke also verifies archive names and captures the People-screen departure list when releases occur. See [DELIVERY_PROGRESS.md](DELIVERY_PROGRESS.md) for recorded runs.

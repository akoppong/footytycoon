# Published assessment evidence

These files are byte-for-byte copies of the local evidence inspected for the 18 October 2026 architecture/product assessment. They were copied and SHA-256 compared on 20 October solely to make the published report self-contained. Copying does not constitute a new test run.

| Published file | Original local artifact | Scope |
|---|---|---|
| `build.log` | `artifacts/assessment-build.log` | Fresh-at-assessment Release build; zero warnings/errors |
| `tests.log` | `artifacts/assessment-tests-final.log` | Fresh-at-assessment tests: 281 passed, none failed/skipped |
| `compact-journal-measurements.json` | `artifacts/compact-journal-measure-results/measurements.json` | Historical schema-23 size measurements |
| `fifty-season-summary.json` | `artifacts/renewal-basis-50-2028-training/summary.json` | Historical schema-21 direct-core pilot; not production save durability |
| `fifty-season-analysis.json` | `artifacts/fifty-season-analysis.json` | Retained extraction of that historical pilot; not a new independent run |
| `owner-desk.png` | `artifacts/compact-journal-screenshots/Owner-Desk-100.png` | Historical schema-23 packaged UI capture |
| `renewal-terms.png` | `artifacts/compact-journal-screenshots/Season-2-contracts-4-100.png` | Historical schema-23 packaged UI capture |

The full raw pilot stream, executable, save containers and other local artifacts are not bundled here. See the tracked [pilot methodology](../FIFTY_SEASON_PILOT.md) and [compact-journal methodology](../COMPACT_JOURNAL.md) for provenance and limitations. Agents must generate their own task evidence; these files do not validate later code or establish player enjoyment, long-career balance, Steam behavior or clean-machine compatibility.

# Agent task pack: prove the ownership loop

Prepared 19 October 2026 against source `57e640d`. This pack decomposes the [assessment](../ARCHITECTURE_PRODUCT_FIT_ASSESSMENT.md) into assignable work. It contains task briefs; none of the implementation tasks has been started or completed by creating this pack.

## Start here

**First objective:** prove that changing an owner's manager/director mandates changes sensible delegated decisions, produces understandable sporting/financial/development tradeoffs, and makes another approach worth trying.

The first experiment uses the existing named manager and director, one owned club, existing league/cup, current financial safeguards and the existing UI. It measures one playable season from a validated starting checkpoint, which may be season two of the existing career. It does not yet prove the full people/relationship fantasy. Hiring, dismissals, relationships, autonomous spending and supporter demand are conditional follow-up work. This narrower sequencing refines the assessment's broader vertical slice; it does not change the assessment's implementation findings.

Start with **E00 and E01**. Finish E00 before any feature code. Do not assign the whole backlog to one agent. A task should normally produce one coherent change and its tests/evidence. If its actual scope grows beyond one reviewable unit, split it in the task pack before expanding implementation. Task sizes below are relative, not elapsed-time promises.

## Task board

All statuses are **Not started**. “Ready” means its brief can be assigned once execution is requested; it does not mean an agent is already working or that later gates have passed.

| ID | Task | Depends on | Initial readiness | Size |
|---|---|---|---|---|
| [E00](E00-experiment-contract.md) | Define the experiment and shared behavior contract | — | Ready; documentation only | Small |
| [E01](E01-baseline.md) | Capture the existing reference season | — | Ready; diagnostics only | Small–medium |
| [E01B](E01B-playable-start.md) | Prepare and validate a playable experimental start | E00, E01 | Waiting for prerequisites; required feasibility gate | Small–medium |
| [E02](E02-policy-state.md) | Persist minimal mandate state safely | E00, E01B | Waiting for dependencies | Medium |
| [E03](E03-manager-behavior.md) | Make manager selection respond to the mandate | E02 | Waiting for dependency | Medium |
| [E04](E04-director-behavior.md) | Make free-agent recommendations respond to the mandate | E02 | Waiting for dependency | Medium |
| [E05](E05-mandate-commands.md) | Add transactional mandate changes | E02 | Waiting for dependency | Medium |
| [E06](E06-outcome-evidence.md) | Connect mandates to execution and explanations | E03, E04, E05 | Waiting for dependencies | Medium |
| [E07](E07-playable-ui.md) | Expose the experiment in People, Desk and History | E06 | Waiting for dependency | Medium |
| [E08](E08-integration-validation.md) | Run matched policies and integrated regression checks | E01, E07 | Waiting for dependencies | Medium |
| [E09](E09-player-validation.md) | Conduct a small player study and make the gate decision | E08 | Waiting for build and actual participants | Small preparation; execution varies |

## Dispatch instructions

Copy this prompt to the implementation agent, substituting the task ID and its repository-relative path. Resolve that path from the agent's own checkout root:

> Complete task E03 from docs/agent-tasks/E03-manager-behavior.md in this checkout. Read docs/agent-tasks/README.md and repository AGENTS.md first. Verify prerequisite deliverables exist and have passed their stated checks. Implement only this task, preserve unrelated work, and follow its scope and acceptance criteria. Use a separate subagent to review the actual changes as required by AGENTS.md, resolve findings, and obtain re-review of material fixes. Return changed files, validation results, reviewer findings and remaining limitations. Do not start the next task or interpret a backlog entry as an assignment.

For E00/E01/E09, honor their documentation/diagnostic/participant boundaries instead of assuming every task requires gameplay code. Dispatch a single task per agent. The coordinator records the assignee, working directory/branch, prerequisite revisions and review result before moving the board status to Done. Suggested status progression: Not started → In progress → In review → Done, or Blocked with a specific unmet dependency. An authored brief is not a completed feature.

## Independent issue execution

Each experiment task is published as a separate GitHub issue with its full brief, prerequisite issue links and shared instructions. One agent owns one issue and delivers one reviewable result; source changes normally use a separate task branch and PR. The tracking issue records the dependency order. Independent ownership does not remove prerequisites: E00 and E01 can start together; E03/E04/E05 can run together only after E02 is integrated.

The planning branch is `codex/owner-mandate-task-plan`, based on the schema-23 source in `codex/compact-journal` (code PR #31). The documentation PR is stacked on that branch to avoid including unrelated code changes. Before starting, verify the checkout contains the task pack and the assessed baseline or a reviewed successor. Until the code baseline is merged, use a branch containing it and declare the PR base explicitly; do not silently start from an older `main`. For dependent tasks, also include all integrated prerequisite changes. Retarget the documentation PR after its base lands if necessary.

Mark an issue complete only when its acceptance checks and independent review pass and its deliverable is accessible to the next agent. A dependency checkbox means its reviewed output is available on the selected integration base; merely closing an issue is not sufficient. E09 still needs actual participants. The conditional backlog remains deferred rather than being published as executable feature assignments.

## Shared context and constraints

- Read [AGENTS.md](../../AGENTS.md). Each meaningful unit requires a reviewer who did not author it and who inspects the actual changes. Review must cover correctness, request alignment, regressions, edge cases, maintainability and appropriate validation. Documentation also needs clarity/consistency review.
- Relevant baseline facts: pure C# Core, a single-writer `GameSession`, immutable views, Godot desktop, schema 23, and three playable seasons. Recheck these against the actual checkout before coding. Never reserve schema numbers in parallel; choose the next version at integration.
- Preserve separate owner/club money, exact obligations, preview purity, saved random streams, stable processing order, command idempotency, stale-quote rejection, publication only after durable writes, and source-file-preserving migrations.
- Tests must cover the new behavior and meaningful failure cases. Keep legacy policies equivalent to current behavior when the experiment's mandate is inactive. Byte equality across a new schema is not expected: encoded world bytes also feed new forecast/proposal IDs. Compare gameplay fields and RNG/financial behavior with explicit version/policy normalization and a consistent semantic mapping of newly generated derived IDs and their references. Do not ignore arbitrary IDs or broken links. Existing migrated history and its stored identifiers must remain unchanged.
- Existing owner approvals remain required for signings in E00–E09. No new autonomous spending, salaries, staff bonuses, supporter demand effects, changed player generation, paid transfer rules or capital-plan freedoms. The manager continues to select every lineup automatically.
- New policy controls affect only the owned club for this experiment. Rivals keep their current policies; shared player-market consequences still happen normally.
- No general AI framework, engine rewrite, career-limit change, Steam integration or storage redesign. No unexplained cash drains, forced human-playtest events or guaranteed development/promotion outcomes.
- When a command or new persistent record changes the schema, migrate all previously supported saves through the existing chain. Preserve pending deals, signed contracts and historical evidence; add new state prospectively. Historical forecasts must not be regenerated.
- Source pointers in tickets are starting points and intended ownership, not permission to overlook callers or edit unrelated systems. Report necessary boundary expansion to the coordinator; do not quietly absorb another ticket.

## Parallel work and integration

1. E00 and E01 can run together: one writes the experiment contract, the other reads/runs the current baseline and writes separate evidence. E01 may report missing opportunities to E00 without changing the baseline.
2. E01B must demonstrate a valid playable starting state before E02. Opening squads contain 22 players and free-agent reviews require fewer than 22, so ordinary opening-season play cannot exercise the proposed director policy. E01B owns the documented checkpoint preparation and control run. E02 then runs serially to establish shared state, validation, migration and the read contract used by the behavior/command tasks.
3. E03, E04 and E05 can run concurrently **only after E02 is integrated** and ownership is agreed. E03 owns match selection; E04 owns free-agent recommendation ranking; E05 owns command/application transactions. Each owns its tests. Shared `Model.cs`, `WorldFactory.cs`, codec/version markers and read-model contracts are coordinator-owned during this wave.
4. A shared-checkout team must not edit the same file concurrently. A required shared-model change pauses the affected task while the coordinator integrates the contract change. Isolated branches/worktrees may be used when appropriate; they do not remove semantic integration dependencies.
5. Integrate and review the three behavior/command changes before E06. Run the integrated relevant tests once; individual green branches do not establish a green combined build.
6. E06, E07, E08 and E09 follow in order. Preparing the E09 discussion guide can happen earlier, but no outcome or gate decision is valid before the tested build and real sessions exist.

With four concurrent agents, use a coordinator and up to three implementers for step 3; free an implementation slot for a non-author reviewer as units finish. Do not skip review to maintain parallel throughput.

## Validation and handoff

The assessment recorded 281 passing tests on 18 October; that is a historical baseline, not evidence for later changes. Each task reports its own commands, commit/working-tree identity, outcomes and log locations.

Use the repository build/run scripts and configured CI checks where applicable. Local cached tooling exists under `.tools`. A previously successful offline-capable build command was:

```powershell
& ./.tools/dotnet/dotnet.exe build FootballTycoon.slnx --no-restore --configuration Release --nologo -m:1 -nodeReuse:false -p:UseSharedCompilation=false
& ./.tools/dotnet/dotnet.exe test tests/FootballTycoon.Tests/FootballTycoon.Tests.csproj --no-build --no-restore --configuration Release --nologo
```

`--no-restore` requires current dependency assets; restore through the configured repository source when necessary. Never claim tests passed when the runner failed to connect. Desktop changes require the Desktop build and applicable engine/graphical smoke checks, including keyboard operation and supported text scales. Core-only or documentation-only work does not require inventing UI tests. E08 owns the final integrated check; do not repeat the full expensive suite after every prose edit.

Use new task-specific artifact directories; preserve old saves and evidence. Do not overwrite the assessment's validation logs. Durable knowledge and concise results belong under `docs/`; raw generated diagnostics can remain in ignored `artifacts/` with provenance.

Every handoff includes:

1. Task ID, prerequisite revisions and actual scope delivered.
2. Changed files and public/state contracts introduced.
3. Checks run with results, plus unrun checks and why.
4. Compatibility/default behavior and demonstrated user-visible consequence.
5. Independent review result, resolved findings and remaining concerns.
6. Exactly what the next task may rely on; do not claim dependent work is implemented.

## Gate after E09

Advance to deeper staff/delegation work only when E08 shows meaningful, explainable policy effects and E09 provides actual evidence that players understand a tradeoff and want to try another approach. The experiment can pass technically and fail as a product. The E00 contract predefines exploratory criteria; do not tune the pass criteria after seeing the result.

If behavior differs but players cannot explain it, revise E06/E07 in a bounded follow-up. If the policies are equivalent or one dominates, revise E03/E04 and the disclosed scenario conditions before adding new systems. If the player sample is missing, record **Awaiting player evidence**; the agent cannot substitute its own simulated opinions. Passing this gate supports the next experiment, not Steam release or a claim of retention.

See the [conditional backlog](BACKLOG.md) for the rest of the assessment. Those entries are not ready implementation assignments.

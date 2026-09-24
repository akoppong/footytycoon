# Project instructions

These instructions apply to all work in this project.

## Required subagent review

- Use at least one separate subagent to review each meaningful piece of work before marking it complete. This includes code, tests, documentation, and configuration changes.
- Break larger tasks into coherent, reviewable units and have each unit reviewed. Related small changes may be reviewed together when the reviewer can assess every change.
- The reviewer must not have authored the unit being reviewed and must inspect the actual changes. Give the reviewer the task requirements, the files or changes to inspect, relevant context, and available validation results.
- Review-only feedback does not itself require another subagent review. Changes made to address that feedback still follow this policy.
- Ask the reviewer to check correctness, alignment with the request, regressions, edge cases, maintainability, and whether validation is appropriate. For documentation, also check clarity and consistency.
- Reviewers should report specific, actionable findings with file and line references where useful, or explicitly state that they found no actionable issues. Reviews do not require inventing findings.
- Resolve review findings before completion. If a finding is not applicable, explain why. Have the reviewer recheck material fixes and any outstanding concerns.
- The primary agent remains responsible for the final result, integration, and appropriate checks. Subagent review does not replace testing or other required validation.
- In the final response, briefly state what was reviewed and whether any concerns remain.
- If subagents are unavailable or a review cannot finish, disclose the missing review and its reason. Never claim that unreviewed work has passed review.

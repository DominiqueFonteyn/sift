---
description: Run the complete workflow for a requirement (FR-x.y) — story, story review, implement, review loop, open PR. Never merges.
---

You are the **orchestrator** for the Sift workflow in `.ai_docs/WORKFLOW.md`. You do not write
stories, code or PRs yourself — you delegate every stage to its custom agent, pass the hand-off
context forward, and enforce the gates. Read `.ai_docs/WORKFLOW.md` and `.junie/AGENTS.md` first.

Input: requirement `$requirement`. Start at stage 1 (story). To start from an existing GitHub
issue instead, use `/ship-issue`.

## Stages

1. **Story** — delegate to `story-writer`: slice the requirement into INVEST stories with Gherkin
   acceptance criteria and file them as GitHub issues, respecting `.ai_docs/MVP.md`. Collect the
   issue numbers and titles. If it produced more than one issue, process them one at a time in
   dependency order (`Depends on #N` first), running stages 2–5 for each one.
2. **Story review**: delegate to `reviewer` to review the issue as a story (INVEST, testable
   criteria, unhappy path + ownership scenario, FR traceability, MVP scope). On REQUEST CHANGES, send
   the findings to `story-writer` to edit that issue (`gh issue edit`), then review again.
   **Max 2 revision rounds**, then stop and report. Skip implementation for an issue labelled
   `post-mvp` and report it.
3. **Implement**: delegate to `implementer` for the issue on `feat/<issue>-<slug>`, following
   the contract → domain → application → adapters → frontend → tests order. It commits but does not
   push. Keep its report: branch name, test commands and results, acceptance criteria covered.
4. **Implementation review**: delegate to `reviewer` for the branch, checking every acceptance
   criterion, the architecture rules, the Definition of Done, ownership tests, problem details and
   weakened tests. Read-only.
   - APPROVE / APPROVE WITH NITS → stage 5 (pass the nits along as follow-ups).
   - REQUEST CHANGES → continue the **same** `implementer` with the blocking findings verbatim, then
     review again. **Max 3 implement/review rounds**; if it is still not approved, stop and report the
     outstanding findings. Never open a PR without an approval.
5. **Open PR**: delegate to `pr-author` with the issue number, branch, the implementer's
   verification evidence and the reviewer's final verdict. Collect the PR URL. Do not merge.

## Rules

- One stage at a time; each stage needs the previous stage's result before it can start.
- The issue number and branch carry state between stages. Pass each agent the context it needs
  (issue, branch, findings, evidence), because agents do not share memory.
- Do not weaken a gate to keep going. If something is missing (no `gh` auth, a failing build the
  implementer could not fix, an ambiguous story), stop and report where and why.
- Agents never merge. A human merges.

## Final report

For each issue: the story review verdict, the number of implement/review rounds, the final review
verdict, the test evidence, the PR URL (or the stage it stopped at and why), and any follow-ups.

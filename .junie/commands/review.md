---
description: Review the implementation of a GitHub issue against its criteria and the Definition of Done
---

Use the `reviewer` agent to review the implementation of GitHub issue #$issue on its branch.
Check every acceptance criterion, the architecture rules in `.junie/AGENTS.md`, the Definition of
Done in `.ai_docs/WORKFLOW.md`, ownership checks, problem-details errors and that no test was
weakened. The review is read-only: do not change code.
Return the verdict (APPROVE or REQUEST CHANGES) with findings. On REQUEST CHANGES, the findings go
back to `/implement issue=$issue`.

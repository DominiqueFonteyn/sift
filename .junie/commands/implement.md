---
description: Implement a GitHub issue as a vertical slice on its own branch (does not open the PR)
---

Use the `implementer` agent to implement GitHub issue #$issue on branch `feat/$issue-<slug>`.
Follow the required order: contract (`api/*.tsp`) → domain → application → adapters → frontend → tests.
Run build, unit, integration and affected E2E tests and report the commands and results.
Commit with conventional commits referencing the issue. Do not push and do not open the PR;
that is the `pr-author` agent's job (`/open-pr`).

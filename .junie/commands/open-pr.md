---
description: Push the branch for an approved issue and open a described, linked pull request
---

Use the `pr-author` agent to push the branch for GitHub issue #$issue and open a pull request that
links the issue. Only proceed if the implementation was approved by the reviewer; if you cannot tell,
stop and ask. Do not merge the PR; a human merges.
Return the PR URL.

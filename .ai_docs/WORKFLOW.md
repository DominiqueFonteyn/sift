# Agentic workflow

SPEC.md says planning and implementation are agentic. These are the roles, the hand-offs and the
rules that keep them honest.

## Roles

| Agent          | File                            | Responsibility                                                                                           |
| -------------- | ------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `story-writer` | `.junie/agents/story-writer.md` | Slices requirements into INVEST stories with Gherkin acceptance criteria and files them as GitHub issues |
| `reviewer`     | `.junie/agents/reviewer.md`     | Reviews stories _and_ implementations; read-only, produces a verdict                                     |
| `implementer`  | `.junie/agents/implementer.md`  | Builds one story as a vertical slice, contract first, with tests                                         |
| `pr-author`    | `.junie/agents/pr-author.md`    | Pushes the branch and opens a described, linked pull request                                             |

Shared know-how lives in skills: `.junie/skills/api-first-typespec` and `.junie/skills/hexagonal-slice`.
Project-wide rules live in `.junie/AGENTS.md`.

## The loop

```
requirement (FR-x.y)
      │
      ▼
 story-writer ──► reviewer (story) ──► GitHub issue, labelled + milestoned
                                           │
                                           ▼
                                      implementer (branch, TypeSpec → domain → adapters → UI → tests)
                                           │
                                           ▼
                                      reviewer (implementation)
                                           │
                       REQUEST CHANGES ◄───┴───► APPROVE
                              │                      │
                              └──► implementer       ▼
                                                 pr-author ──► PR ──► human merge
```

A human merges. Agents never merge their own pull requests.

## Conventions

- **Labels:** `story`, `bug`, `spike`, `post-mvp`, plus a milestone label `M0`–`M4`.
- **Branches:** `feat/<issue>-<slug>` / `fix/<issue>-<slug>`.
- **Commits:** conventional commits referencing the issue.
- **One issue → one branch → one PR.** If a story grows past a reviewable PR, split it and file the remainder.

## Definition of Done (every story)

- [ ] TypeSpec updated first; OpenAPI and client regenerated and committed
- [ ] Vertical slice: domain, application, adapters, UI
- [ ] Unit tests for domain/application logic
- [ ] Integration tests for adapters against real SQLite
- [ ] E2E updated when a user journey changed
- [ ] Ownership enforced and proven by a test
- [ ] Errors returned as problem details
- [ ] Build and all tests green
- [ ] Docs updated when behaviour changed

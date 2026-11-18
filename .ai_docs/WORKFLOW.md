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

## Slash commands

Each hand-off in the loop has a project slash command in `.junie/commands/`. Run one per prompt and
check the result before the next; the issue number carries state between stages.

| Command                  | Delegates to   | Stage                                             |
| ------------------------ | -------------- | ------------------------------------------------- |
| `/story requirement=FR-2.1` | `story-writer` | Slice a requirement into issues                   |
| `/review-story issue=12` | `reviewer`     | Review the issue as a story                       |
| `/implement issue=12`    | `implementer`  | Build the slice on `feat/12-<slug>`, no PR        |
| `/review issue=12`       | `reviewer`     | Review the implementation; APPROVE / REQUEST CHANGES |
| `/open-pr issue=12`      | `pr-author`    | Push and open the PR; a human merges              |

On REQUEST CHANGES, run `/implement issue=12` again with the findings, then `/review issue=12`.

### Running the whole loop

`/ship-requirement` and `/ship-issue` orchestrate every stage above in one prompt, delegating each to
its agent and enforcing the gates. They run in the main session (not as a custom agent) because they
have to hand work to the other agents. They are two commands because Junie only runs a command when
every `$argument` in its template is supplied, so one command cannot take either argument.

| Command                                 | Runs                                                               |
| --------------------------------------- | ------------------------------------------------------------------ |
| `/ship-requirement requirement=FR-2.1`  | story → story review → implement ⇄ review → PR, for each new issue |
| `/ship-issue issue=12`                  | story review → implement ⇄ review → PR, for an existing issue      |

Gates: at most 2 story revision rounds and 3 implement/review rounds, then they stop and report. They
do not open a PR without APPROVE, and they never merge. Use the single-stage commands when you want
to check each hand-off yourself.

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

# Sift

Sift helps a job seeker manage recruiters, recruitment bureaus and vacancies, and scores every
vacancy against their resume and their own must-have / nice-to-have requirements — so the evening
goes to the three roles that fit instead of the thirty that do not.

Status: **planning complete, implementation not started.** The first story is
[#1 — walking skeleton](https://github.com/DominiqueFonteyn/sift/issues/1).

## Documentation

| Document                                                                   | What it covers                                     |
| -------------------------------------------------------------------------- | -------------------------------------------------- |
| [.ai_docs/SPEC.md](.ai_docs/SPEC.md)                                       | Product and stack brief                            |
| [.ai_docs/FUNCTIONAL_REQUIREMENTS.md](.ai_docs/FUNCTIONAL_REQUIREMENTS.md) | Numbered requirements (`FR-x.y`)                   |
| [.ai_docs/MVP.md](.ai_docs/MVP.md)                                         | MVP scope, deliberate trade-offs, milestones M0–M4 |
| [.ai_docs/WORKFLOW.md](.ai_docs/WORKFLOW.md)                               | The agentic story → review → implement → PR loop   |
| [.junie/AGENTS.md](.junie/AGENTS.md)                                       | Architecture rules and coding conventions          |

## Stack

TypeSpec (API-first) · ASP.NET Core with hexagonal architecture · EF Core for writes, Dapper for
reads · SQLite for the MVP · React + TypeScript + Vite + Tailwind/SCSS · JWT auth · E2E tests.

## Getting started

Nothing to run yet. The toolchain you will need: .NET SDK 10, Node 22+, Yarn (`corepack enable`)
and the GitHub CLI.

# Sift — project guidelines

Sift helps a job seeker manage recruiters, bureaus and vacancies, and scores each vacancy against
their resume and requirements.

Read before working on anything substantial:

- `.ai_docs/SPEC.md` — the product and stack brief (source of truth for the stack).
- `.ai_docs/FUNCTIONAL_REQUIREMENTS.md` — numbered requirements (`FR-x.y`). Reference these ids in stories, commits and PRs.
- `.ai_docs/MVP.md` — what is in and out of the first release, and why.
- `.ai_docs/WORKFLOW.md` — how stories, implementation, review and PRs flow between agents.

## Tech stack (do not substitute without asking)

| Layer        | Choice                                                               |
| ------------ | -------------------------------------------------------------------- |
| API contract | TypeSpec, API-first — the spec changes **before** the implementation |
| Backend      | ASP.NET Core (.NET 10), hexagonal architecture                       |
| Writes       | EF Core                                                              |
| Reads        | Dapper                                                               |
| Database     | SQLite for the MVP; provider-specific SQL must stay inside adapters  |
| Frontend     | React + TypeScript + Vite, Yarn, SCSS + Tailwind                     |
| Auth         | JWT bearer tokens                                                    |
| Tests        | Unit + integration + end-to-end                                      |
| Hosting      | GitHub (issues, PRs, Actions)                                        |

## Architecture rules

- **Hexagonal / ports & adapters.** Dependencies point inwards only: `Domain` ← `Application` ← `Adapters`. The domain references no framework, no EF, no ASP.NET, no Dapper.
- `Domain` — entities, value objects, domain services, invariants. Pure C#.
- `Application` — use cases, port interfaces (`IVacancyRepository`, `IMatchEngine`, `IClock`, ...). Orchestration only, no SQL, no HTTP.
- `Adapters` — inbound (ASP.NET controllers/endpoints generated from TypeSpec) and outbound (EF Core write repositories, Dapper read queries, file/clock/token adapters).
- **CQRS-lite:** commands go through EF Core aggregates; queries go through Dapper returning read models shaped for the UI. Never query with EF for a read path, never write with Dapper.
- Composition root is the API host project; it is the only place that wires implementations to ports.

## Working agreements

- **API-first:** change `api/*.tsp` first, regenerate, then implement. A PR that changes HTTP behaviour without a TypeSpec change is wrong.
- **Vertical slices:** a story delivers domain + application + adapters + UI + tests for one thin behaviour, not a horizontal layer.
- **Tests:** domain and application logic get unit tests; adapters get integration tests against a real SQLite database; each MVP journey gets an E2E test. Scoring logic is table-driven.
- **Security:** every query is scoped by the authenticated user id. Never trust an id from the request body or route without an ownership check.
- **Errors:** RFC 7807 problem details, consistently, from a single error-mapping place.
- **No secrets in the repo.** Use user-secrets / environment variables.

## Conventions

- C#: file-scoped namespaces, `sealed` by default, nullable enabled, `record` for DTOs and value objects, async suffix `Async`, one public type per file.
- TypeScript: functional components, hooks, no default exports, API clients generated from TypeSpec — never hand-write request/response types.
- Styling: Tailwind utilities first; SCSS modules only for things Tailwind cannot express.
- Branches: `feat/<issue-number>-<slug>`, `fix/<issue-number>-<slug>`.
- Commits: conventional commits, and reference the issue (`feat(vacancy): paste a vacancy (#12)`).
- Comments explain _why_, not _what_. The codebase is comment-light by default.

## Repository layout (target)

```
api/            TypeSpec definitions + generated OpenAPI
src/
  Sift.Domain/
  Sift.Application/
  Sift.Infrastructure/     EF Core (write) + Dapper (read) adapters
  Sift.Api/                ASP.NET Core host, composition root
  Sift.Web/                React + Vite frontend
tests/
  Sift.Domain.Tests/
  Sift.Application.Tests/
  Sift.Api.IntegrationTests/
  Sift.E2E/
.ai_docs/       Product, requirement, MVP and workflow docs
.junie/         Agent guidelines, custom agents and skills
```

## Local toolchain

Verified on this machine: .NET SDK 10.0.401, Node 24.x, npm 11.x, git, GitHub CLI, Docker.
Yarn is not installed globally — enable it with `corepack enable` before working on the frontend.
The TypeSpec compiler is a project dev dependency, run it via `yarn tsp`.

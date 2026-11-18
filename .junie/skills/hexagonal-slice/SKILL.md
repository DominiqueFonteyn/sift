---
name: hexagonal-slice
description: How to build one vertical slice in the Sift backend using hexagonal architecture with EF Core for writes and Dapper for reads. Use when adding a use case, endpoint, repository, query or domain rule.
---

#### Hexagonal vertical slice

A slice is one behaviour delivered through every layer. Never a layer delivered across behaviours.

#### Layers and what may live there

| Project               | Contains                                                                           | Must not reference                               |
| --------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------ |
| `Sift.Domain`         | Entities, value objects, domain services, invariants, domain events                | EF Core, ASP.NET, Dapper, anything I/O           |
| `Sift.Application`    | Use-case handlers, port interfaces, DTOs                                           | Concrete adapters, SQL, HTTP types               |
| `Sift.Infrastructure` | EF Core `DbContext`, write repositories, Dapper read queries, clock, token service | —                                                |
| `Sift.Api`            | Endpoints, auth, problem-details mapping, composition root                         | Domain internals bypassing the application layer |

Dependencies point inwards only. If the domain seems to need infrastructure, introduce a port.

#### Build order for a command (write)

1. Express the rule in the domain; unit test it.
2. Define the use case in `Application` plus the port it needs (`IVacancyRepository`, `IClock`, ...).
3. Implement the port in `Infrastructure` with EF Core; load and save whole aggregates, no partial updates.
4. Expose the endpoint in `Sift.Api` matching the generated contract; map failures to problem details.
5. Register the implementation in the composition root.
6. Integration-test the adapter against a real SQLite database (file-based or shared in-memory connection), not a mock.

#### Build order for a query (read)

1. Define the read model as a flat `record` shaped for the screen that uses it.
2. Define the query port in `Application`.
3. Implement it in `Infrastructure` with Dapper and explicit SQL. Keep SQL inside the adapter; parameterise everything.
4. Always filter by the authenticated user id in the SQL itself — ownership is not an afterthought in C#.

#### Hard rules

- Never read a projection through EF Core; never write through Dapper.
- No `DbContext` or `IDbConnection` outside `Sift.Infrastructure`.
- Use cases return results/outcomes, not HTTP status codes.
- Entities protect their invariants: private setters, behaviour-named methods, no anaemic models.
- Migrations are generated and committed with the slice that needs them.

#### Checklist before handing the slice over

- [ ] Domain logic unit tested, including the invariant it protects
- [ ] Port defined in `Application`, implementation only in `Infrastructure`
- [ ] Write path EF, read path Dapper, no crossover
- [ ] Query filtered by owner, with a test proving another user gets nothing
- [ ] Adapter integration tested against real SQLite
- [ ] Migration committed and applied cleanly on an empty database

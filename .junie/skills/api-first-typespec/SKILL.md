---
name: api-first-typespec
description: How to change the Sift HTTP API the API-first way - edit the TypeSpec definition in api/ first, regenerate OpenAPI and the typed clients, then implement. Use whenever an endpoint, request, response or error shape is added or changed.
---

#### API-first with TypeSpec

In Sift the TypeSpec definition is the contract. Hand-written request/response types, hand-edited
OpenAPI, or an endpoint that exists only in C# are all defects.

#### Order of operations

1. Edit the relevant file under `api/` (`main.tsp` plus one file per bounded area, e.g. `vacancies.tsp`).
2. Compile: `yarn tsp compile api` — fix every warning, not just errors.
3. Regenerate the OpenAPI document and the frontend client; commit the generated output.
4. Implement the server side against the generated contract.
5. Consume the generated client from React. Never hand-write the TS types.

#### Modelling conventions

- One namespace per bounded area; operations grouped in an interface named after the resource.
- Models are nouns (`Vacancy`, `CreateVacancyRequest`, `VacancyListItem`); operations are verbs (`create`, `list`, `getById`).
- Separate the write model from the read model — the list item is not the full aggregate.
- Annotate validation in the spec (`@minLength`, `@maxLength`, `@format`, `@pattern`) so it is generated, not duplicated by hand.
- Every operation declares its error responses; errors use the shared problem-details model.
- Everything except register/login carries the bearer security scheme.
- Collections are paged from the start: `page`, `pageSize`, and a response wrapping `items` + `total`.
- Ids are UUIDs; timestamps are UTC `utcDateTime`.

#### Breaking changes

Removing or renaming a field, tightening validation, or changing a status code is breaking.
Call it out explicitly in the PR description and update the frontend in the same PR.

#### Review checklist

- [ ] TypeSpec changed before the implementation, in the same PR
- [ ] `yarn tsp compile api` clean
- [ ] Generated OpenAPI + client committed and in sync
- [ ] Error responses and auth declared on every new operation
- [ ] Read and write models kept separate
- [ ] No hand-written TypeScript request/response types

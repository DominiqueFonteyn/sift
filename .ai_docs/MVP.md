# MVP definition

## Goal

> A job seeker can log in, store one resume and a short list of must-have/nice-to-have requirements,
> paste in a vacancy, and immediately see a score telling them whether it is worth their time.

Everything that does not serve that sentence is deferred.

## MVP scope

| Area                 | In the MVP                                                                                                  | Deferred                                                                    |
| -------------------- | ----------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| Auth (FR-1)          | Register, login, JWT-protected API, strict ownership checks                                                 | Refresh tokens, password reset, OAuth, email verification                   |
| Bureaus (FR-2)       | Create + list + view, name required                                                                         | Edit/delete rules, uniqueness enforcement, address                          |
| Recruiters (FR-3)    | Create + list, optional link to one bureau                                                                  | Filtering, vacancies-per-recruiter view                                     |
| Vacancies (FR-4)     | Create by **pasting text**, title + company + raw text, list, view, delete, optional recruiter link         | File upload & parsing, status pipeline, search/filter, archiving            |
| Resumes (FR-5)       | **One** resume per user, authored as plain text in the app                                                  | Multiple resumes, document upload/parsing, structured sections, duplication |
| Requirements (FR-6)  | Flat list of requirements, each must-have or nice-to-have                                                   | Weights, named profiles, salary/industry typing                             |
| Matching (FR-7)      | Synchronous, deterministic keyword/rule-based scoring; score + per-requirement verdict; ranked vacancy list | LLM-based analysis, async jobs, stored history, gap/strength extraction     |
| Cross-cutting (FR-8) | TypeSpec-first contract, problem-details errors, one E2E happy path                                         | Accessibility audit, auditing beyond timestamps                             |

## Deliberate MVP decisions

1. **No file upload in v1.** Paste-text covers the same user need and removes PDF/DOCX parsing, storage and virus-scanning from the critical path. FR-4.1 and FR-5.1 are only half-delivered on purpose.
2. **No AI in the matcher.** The first matcher is a transparent rule engine (requirement term found in vacancy text → met; unmet must-have → capped score). It is testable, free and fast, and it establishes the port that an LLM adapter can implement later.
3. **SQLite.** Single file, zero infra. EF Core for writes and Dapper for reads from day one so the hexagonal boundaries are exercised; the provider swap to Postgres/SQL Server stays a config change.
4. **One resume per user.** Removes "default resume" selection from every screen and API.
5. **Token in localStorage.** The web app keeps the JWT in `localStorage` (simple, survives reloads). Acceptable for the MVP; revisit with refresh tokens and safer storage post-MVP.
   - **Risk accepted:** any XSS can read the token from `localStorage` and use it until it expires.
   - **What limits it:** a 1h token lifetime, zero clock skew (a token dies exactly at expiry) and no refresh token, so a stolen token cannot be renewed.
6. **Hexagonal from the first commit.** Even thin slices go domain → port → adapter, because retrofitting the architecture later is what actually costs time.

## Walking skeleton (definition of "MVP done")

A single E2E test passes:

1. Register and log in.
2. Save a resume.
3. Add two requirements (one must-have, one nice-to-have).
4. Paste in two vacancies.
5. Open the vacancy list sorted by score, and see the vacancy that satisfies the must-have ranked first with a visible explanation.

## Release increments

- **M0 — Walking skeleton**: TypeSpec contract, solution skeleton, health endpoint, React shell, CI, E2E harness.
- **M1 — Identity**: register, login, JWT, ownership guard.
- **M2 — My profile**: resume + requirements CRUD.
- **M3 — Vacancies**: paste, list, view, delete, optional recruiter/bureau link.
- **M4 — Match**: scoring engine, score on vacancy detail, ranked list. _MVP complete._
- **M5+ — Post-MVP**: document upload & parsing, LLM matcher, statuses, multiple resumes, requirement profiles.

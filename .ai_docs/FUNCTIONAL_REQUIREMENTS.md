# Functional requirements

Derived from [SPEC.md](SPEC.md). Each requirement has a stable id (`FR-x.y`) so stories, tickets and
pull requests can reference it.

## Actors

| Actor      | Description                                                                                                                          |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| **User**   | A job seeker. Owns recruiters, vacancies, resumes and requirements. Single-tenant per account: a user only ever sees their own data. |
| **System** | The matching engine that scores a vacancy against a resume + requirements.                                                           |

> There is no admin/recruiter-facing role. Recruiters and bureaus are _data owned by the user_, not accounts.

## FR-1 Accounts & authentication

- **FR-1.1** A user can register with an email address and a password.
- **FR-1.2** A user can log in and receives a JWT access token.
- **FR-1.3** Every API endpoint except register/login requires a valid JWT.
- **FR-1.4** A user can only read or modify resources they own; another user's data must never be returned (404/403, never a leak).
- **FR-1.5** A user can log out (client-side token discard; server-side refresh/revocation is post-MVP).

## FR-2 Recruitment bureaus

- **FR-2.1** A user can add a bureau: name (required), website, address, notes.
- **FR-2.2** A user can list, view, edit and delete their bureaus.
- **FR-2.3** Deleting a bureau is blocked while recruiters are still linked to it, or cascades after explicit confirmation.
- **FR-2.4** Bureau names are unique per user.

## FR-3 Recruiters

- **FR-3.1** A user can add a recruiter: name (required), email, phone, LinkedIn URL, notes.
- **FR-3.2** A recruiter may be linked to exactly one bureau, or to none (freelance / in-house).
- **FR-3.3** A user can list, view, edit and delete their recruiters, and filter them by bureau.
- **FR-3.4** A recruiter shows the vacancies they introduced.

## FR-4 Vacancies

- **FR-4.1** A user can create a vacancy by uploading a document (PDF, DOCX, TXT, MD) **or** by pasting/typing its text.
- **FR-4.2** A vacancy has: title, company, location, employment type, source URL, raw text, and the original file when uploaded.
- **FR-4.3** A vacancy can be linked to a recruiter and/or a bureau.
- **FR-4.4** Uploaded documents are parsed to plain text; a parse failure is surfaced to the user and the vacancy stays editable.
- **FR-4.5** A user can list, view, edit, archive and delete vacancies, and search/filter them by title, company, recruiter, bureau and status.
- **FR-4.6** A vacancy has a status reflecting the application pipeline (e.g. `New`, `Interested`, `Applied`, `Rejected`, `Archived`).
- **FR-4.7** File uploads are limited in size and restricted to the allowed content types.

## FR-5 Resumes

- **FR-5.1** A user can upload a resume document (PDF, DOCX, TXT, MD) **or** author one in the app.
- **FR-5.2** A resume has a name, structured content (summary, skills, experience, education) and/or raw text.
- **FR-5.3** A user can hold multiple resumes and mark one as the default used for matching.
- **FR-5.4** A user can list, view, edit, duplicate and delete resumes.
- **FR-5.5** An uploaded resume is parsed into text, and where possible into skills/experience.

## FR-6 Job requirements (user preferences)

- **FR-6.1** A user can define requirements such as desired role, technologies, seniority, location/remote policy, minimum salary, contract type, industries to avoid.
- **FR-6.2** Each requirement is either **must-have** or **nice-to-have**, and nice-to-haves carry a weight.
- **FR-6.3** Requirements can be grouped into named profiles (e.g. "Backend contract roles") so different searches can be scored differently.
- **FR-6.4** A user can list, view, edit and delete requirements and profiles.

## FR-7 Matching & analysis

- **FR-7.1** The system analyses a vacancy against a selected resume and requirement profile and produces a match result.
- **FR-7.2** A match result contains an overall score (0-100), a per-requirement verdict (met / not met / unknown) and a short explanation per verdict.
- **FR-7.3** An unmet **must-have** caps or heavily penalises the overall score and is flagged prominently.
- **FR-7.4** The result lists skills present in the vacancy but missing from the resume ("gaps") and skills matched ("strengths").
- **FR-7.5** Matching can be triggered manually per vacancy and is re-runnable; results are stored with the inputs used (resume version, profile, timestamp).
- **FR-7.6** A user can see all vacancies ranked by match score.
- **FR-7.7** Matching runs asynchronously when slow, and its status (`Pending`, `Running`, `Completed`, `Failed`) is visible.

## FR-8 Cross-cutting

- **FR-8.1** The API is defined API-first in TypeSpec; server and client contracts are generated from it.
- **FR-8.2** All validation errors are returned in a consistent problem-details shape.
- **FR-8.3** The UI is responsive and keyboard-accessible.
- **FR-8.4** Key user journeys are covered by end-to-end tests.
- **FR-8.5** Actions that change data are auditable (created/updated timestamps, owner).

## Out of scope (explicitly, for now)

- Multi-user collaboration, sharing or teams.
- Automatic vacancy scraping from job boards.
- Sending email to recruiters from within Sift.
- Interview scheduling / calendar integration.
- Cover letter generation.

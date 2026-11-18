# SPECS

We are going to build an application to manager job offers and recruiter info. The name is `Sift`.

In Sift, users will be able to:
- add recruitment bureaus and recruiter(s) info
- upload vacancies
- upload or create their own resume
- add specific requirements for a job

Sift will analyse the uploaded vacancies and match it with the resume and requirements of the user.


## Technical details
### Tech stack
- API first, using TypeSpec
- ASP.NET Core with EF for writing, Dapper for reading
- Hexagonal architecture
- React, Typescript, Vite, Yarn, SCSS, TailWind
- End-to-end tests
- Authentication via JWT token

### Workflow
Planning & Implementation will be done in an agentic way.
- An agent will create the user stories and tickets following best practices (e.g. INVEST)
- An agent will write the implementation.
- An agent will review user stories, tickers and implementations
- An agent will create pull requests

### Version control
This repository is hosted on Github.
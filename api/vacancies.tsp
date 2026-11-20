import "@typespec/http";

using Http;

namespace Sift;

@doc("The vacancy to paste. The owner is always the signed-in user; any owner, id or creation time supplied is ignored.")
model CreateVacancyRequest {
  @doc("Job title. Must contain at least one non-whitespace character. At most 200 characters, counted as sent; surrounding whitespace is trimmed when stored.")
  @minLength(1)
  @maxLength(200)
  @pattern("\\S")
  title: string;

  @doc("Hiring company. Must contain at least one non-whitespace character. At most 200 characters, counted as sent; surrounding whitespace is trimmed when stored.")
  @minLength(1)
  @maxLength(200)
  @pattern("\\S")
  company: string;

  @doc("The pasted vacancy text. Must contain at least one non-whitespace character. At most 20,000 characters, counted as sent in UTF-16 code units (JavaScript string length), with no trimming before counting; stored exactly as sent.")
  @minLength(1)
  @maxLength(20000)
  @pattern("\\S")
  rawText: string;
}

@doc("A vacancy owned by the signed-in user.")
model Vacancy {
  @doc("Identifier of the vacancy.")
  @format("uuid")
  id: string;

  @doc("Job title, trimmed.")
  title: string;

  @doc("Hiring company, trimmed.")
  company: string;

  @doc("The vacancy text, exactly as pasted.")
  rawText: string;

  @doc("When the vacancy was created, in UTC, taken from the application clock.")
  createdAt: utcDateTime;
}

@doc("An input the signed-in user must provide before a vacancy can be scored.")
enum MatchInput {
  @doc("The user has not saved a resume.")
  Resume: "resume",

  @doc("The user has no requirements.")
  Requirements: "requirements",
}

@doc("Whether one requirement was found in the vacancy text.")
enum VerdictOutcome {
  @doc("The requirement term appears in the vacancy text.")
  Met: "met",

  @doc("The requirement term does not appear in the vacancy text.")
  NotMet: "not met",
}

@doc("The verdict for one of the signed-in user's requirements.")
model RequirementVerdict {
  @doc("The requirement term.")
  requirement: string;

  @doc("Whether the requirement is a must-have or a nice-to-have.")
  kind: RequirementKind;

  @doc("Whether the term was found in the vacancy text.")
  verdict: VerdictOutcome;

  @doc("Names the requirement term and says whether it was found in the vacancy text.")
  explanation: string;
}

@doc("How a vacancy scores against the signed-in user's resume and requirements. It is worked out each time the vacancy is opened and is never stored. Without a resume or without requirements there is no score and no verdicts, and the missing inputs are listed instead.")
model VacancyMatch {
  @doc("Overall score, an integer from 0 to 100. A vacancy meeting every must-have scores at least 50; one with an unmet must-have scores at most 49. Null when an input is missing.")
  @minValue(0)
  @maxValue(100)
  score: int32 | null;

  @doc("One verdict per requirement, unmet must-haves first. Empty when an input is missing.")
  verdicts: RequirementVerdict[];

  @doc("The inputs the user still has to provide, resume first. Empty when the vacancy was scored.")
  missingInputs: MatchInput[];
}

@doc("A vacancy owned by the signed-in user, together with how it scores.")
model VacancyDetail {
  @doc("Identifier of the vacancy.")
  @format("uuid")
  id: string;

  @doc("Job title, trimmed.")
  title: string;

  @doc("Hiring company, trimmed.")
  company: string;

  @doc("The vacancy text, exactly as pasted.")
  rawText: string;

  @doc("When the vacancy was created, in UTC, taken from the application clock.")
  createdAt: utcDateTime;

  @doc("How the vacancy scores against the signed-in user's resume and requirements.")
  match: VacancyMatch;
}

@doc("A vacancy as shown in the list; the raw text is not included.")
model VacancyListItem {
  @doc("Identifier of the vacancy.")
  @format("uuid")
  id: string;

  @doc("Job title.")
  title: string;

  @doc("Hiring company.")
  company: string;

  @doc("When the vacancy was created, in UTC.")
  createdAt: utcDateTime;
}

@doc("One page of the signed-in user's vacancies, newest first.")
model VacancyPage {
  @doc("The vacancies on this page.")
  items: VacancyListItem[];

  @doc("Total number of the signed-in user's vacancies across all pages.")
  @minValue(0)
  total: int32;

  @doc("The 1-based page that was returned.")
  @minValue(1)
  page: int32;

  @doc("The maximum number of items per page that was applied.")
  @minValue(1)
  @maxValue(100)
  pageSize: int32;
}

@route("/vacancies")
@tag("Vacancies")
interface Vacancies {
  @doc("Paste a vacancy owned by the signed-in user. Missing, blank or oversized fields are rejected with 400 problem details whose errors are keyed by field; nothing is stored.")
  @post
  create(@body request: CreateVacancyRequest): {
    @statusCode _: 201;
    @body body: Vacancy;
  } | BadRequestProblem | UnauthorizedProblem;

  @doc("List only the signed-in user's vacancies, newest first (ties broken deterministically), one page at a time. A page beyond the end returns empty items. An out-of-range page or page size is rejected with 400.")
  @get
  list(
    @doc("1-based page number. Defaults to 1.")
    @query
    @minValue(1)
    page?: int32 = 1,

    @doc("Number of items per page, between 1 and 100. Defaults to 20.")
    @query
    @minValue(1)
    @maxValue(100)
    pageSize?: int32 = 20,
  ): VacancyPage | BadRequestProblem | UnauthorizedProblem;

  @doc("Get one of the signed-in user's vacancies in full, scored against the user's own resume and requirements. The score is worked out on every request and never stored. A malformed id, an unknown id and another user's vacancy all return the same 404 and nothing is scored.")
  @route("/{id}")
  @get
  get(@path @format("uuid") id: string): VacancyDetail | UnauthorizedProblem | NotFoundProblem;

  @doc("Permanently delete one of the signed-in user's vacancies. A malformed id, an unknown id, an already-deleted vacancy and another user's vacancy all return the same 404 and nothing is changed.")
  @route("/{id}")
  @delete
  delete(@path @format("uuid") id: string): {
    @statusCode _: 204;
  } | UnauthorizedProblem | NotFoundProblem;
}

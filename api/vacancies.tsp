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
}

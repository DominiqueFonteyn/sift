import "@typespec/http";

using Http;

namespace Sift;

@doc("The details needed to add a recruiter. The owner is always the signed-in user; any owner supplied is ignored.")
model CreateRecruiterRequest {
  @doc("Name of the recruiter. Must contain at least one non-whitespace character; a name made only of whitespace is rejected with 400. At most 200 characters, counted as sent; surrounding whitespace is trimmed when stored.")
  @minLength(1)
  @maxLength(200)
  @pattern("\\S")
  name: string;

  @doc("Email address. Optional; when given it must be a well-formed email address of at most 254 characters. An empty or whitespace-only value is treated as not given.")
  @maxLength(254)
  email?: string;

  @doc("Phone number as free text; the format is not checked. Optional; at most 50 characters, counted as sent. An empty or whitespace-only value is treated as not given.")
  @maxLength(50)
  phone?: string;

  @doc("LinkedIn profile URL. Optional; when given it must be an absolute http or https URL of at most 2048 characters. An empty or whitespace-only value is treated as not given.")
  @maxLength(2048)
  linkedInUrl?: string;
}

@doc("A recruiter owned by the signed-in user.")
model Recruiter {
  @doc("Identifier of the recruiter.")
  @format("uuid")
  id: string;

  @doc("Name of the recruiter.")
  name: string;

  @doc("Email address, when one was given.")
  email?: string;

  @doc("Phone number, when one was given.")
  phone?: string;

  @doc("LinkedIn profile URL, when one was given.")
  linkedInUrl?: string;

  @doc("When the recruiter was created, in UTC.")
  createdAt: utcDateTime;
}

@doc("One page of the signed-in user's recruiters, oldest first.")
model RecruiterPage {
  @doc("The recruiters on this page.")
  items: Recruiter[];

  @doc("Total number of the signed-in user's recruiters across all pages.")
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

@route("/recruiters")
@tag("Recruiters")
interface Recruiters {
  @doc("Add a recruiter owned by the signed-in user.")
  @post
  create(@body request: CreateRecruiterRequest): {
    @statusCode _: 201;
    @body body: Recruiter;
  } | BadRequestProblem | UnauthorizedProblem;

  @doc("List only the signed-in user's recruiters, one page at a time. An out-of-range page or page size is rejected with 400.")
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
  ): RecruiterPage | BadRequestProblem | UnauthorizedProblem;
}

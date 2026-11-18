import "@typespec/http";

using Http;

namespace Sift;

@doc("The details needed to add a bureau. The owner is always the signed-in user; any owner supplied is ignored.")
model CreateBureauRequest {
  @doc("Name of the bureau. Must contain at least one non-whitespace character; a name made only of whitespace is rejected with 400. At most 200 characters, counted as sent; surrounding whitespace is trimmed when stored.")
  @minLength(1)
  @maxLength(200)
  @pattern("\\S")
  name: string;
}

@doc("A bureau owned by the signed-in user.")
model Bureau {
  @doc("Identifier of the bureau.")
  @format("uuid")
  id: string;

  @doc("Name of the bureau.")
  name: string;

  @doc("When the bureau was created, in UTC.")
  createdAt: utcDateTime;
}

@doc("One page of the signed-in user's bureaus, oldest first.")
model BureauPage {
  @doc("The bureaus on this page.")
  items: Bureau[];

  @doc("Total number of the signed-in user's bureaus across all pages.")
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

@doc("The resource does not exist or is not owned by the signed-in user. The two cases are indistinguishable.")
@error
model NotFoundProblem {
  @statusCode _: 404;
  ...ProblemDetails;
}

@route("/bureaus")
@tag("Bureaus")
interface Bureaus {
  @doc("Add a bureau owned by the signed-in user.")
  @post
  create(@body request: CreateBureauRequest): {
    @statusCode _: 201;
    @body body: Bureau;
  } | BadRequestProblem | UnauthorizedProblem;

  @doc("List only the signed-in user's bureaus, one page at a time. An out-of-range page or page size is rejected with 400.")
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
  ): BureauPage | BadRequestProblem | UnauthorizedProblem;

  @doc("Get one of the signed-in user's bureaus. Another user's bureau returns 404, same as a missing one.")
  @route("/{id}")
  @get
  get(@path @format("uuid") id: string): Bureau | UnauthorizedProblem | NotFoundProblem;
}

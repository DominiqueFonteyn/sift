import "@typespec/http";

using Http;

namespace Sift;

@doc("How strongly a requirement counts: a must-have is a deal-breaker, a nice-to-have is a bonus.")
enum RequirementKind {
  @doc("A deal-breaker.")
  MustHave: "must-have",

  @doc("A bonus.")
  NiceToHave: "nice-to-have",
}

@doc("The details needed to add a job requirement. The owner is always the signed-in user; any owner supplied is ignored.")
model CreateRequirementRequest {
  @doc("Free text of the requirement. Must contain at least one non-whitespace character; whitespace-only text is rejected with 400. At most 200 characters, counted as sent; surrounding whitespace is trimmed when stored. Unique per user, compared case-insensitively after trimming; a duplicate is rejected with 409.")
  @minLength(1)
  @maxLength(200)
  @pattern("\\S")
  text: string;

  @doc("Whether the requirement is a must-have or a nice-to-have. Required; any other value is rejected with 400.")
  kind: RequirementKind;
}

@doc("A job requirement owned by the signed-in user.")
model Requirement {
  @doc("Identifier of the requirement.")
  @format("uuid")
  id: string;

  @doc("Free text of the requirement.")
  text: string;

  @doc("Whether the requirement is a must-have or a nice-to-have.")
  kind: RequirementKind;

  @doc("When the requirement was created, in UTC.")
  createdAt: utcDateTime;
}

@doc("One page of the signed-in user's requirements, oldest first.")
model RequirementPage {
  @doc("The requirements on this page.")
  items: Requirement[];

  @doc("Total number of the signed-in user's requirements across all pages.")
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

@route("/requirements")
@tag("Requirements")
interface Requirements {
  @doc("Add a requirement owned by the signed-in user. Blank text or an invalid or missing kind returns 400; text the user already has (case-insensitively, after trimming) returns 409.")
  @post
  create(@body request: CreateRequirementRequest): {
    @statusCode _: 201;
    @body body: Requirement;
  } | BadRequestProblem | UnauthorizedProblem | ConflictProblem;

  @doc("List only the signed-in user's requirements, one page at a time. An out-of-range page or page size is rejected with 400.")
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
  ): RequirementPage | BadRequestProblem | UnauthorizedProblem;

  @doc("Delete one of the signed-in user's requirements. Another user's requirement returns 404, same as a missing one.")
  @route("/{id}")
  @delete
  delete(@path @format("uuid") id: string): {
    @statusCode _: 204;
  } | UnauthorizedProblem | NotFoundProblem;
}

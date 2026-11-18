import "@typespec/http";

using Http;

namespace Sift;

@doc("The details needed to add a bureau. The owner is always the signed-in user; any owner supplied is ignored.")
model CreateBureauRequest {
  @doc("Name of the bureau.")
  @minLength(1)
  @maxLength(200)
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

  @doc("List only the signed-in user's bureaus.")
  @get
  list(): Bureau[] | UnauthorizedProblem;

  @doc("Get one of the signed-in user's bureaus. Another user's bureau returns 404, same as a missing one.")
  @route("/{id}")
  @get
  get(@path @format("uuid") id: string): Bureau | UnauthorizedProblem | NotFoundProblem;
}

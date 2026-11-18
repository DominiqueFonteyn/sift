import "@typespec/http";

using Http;

namespace Sift;

@doc("The details needed to create an account.")
model RegisterRequest {
  @doc("Email address used to identify the account. Unique across all accounts.")
  @minLength(3)
  @maxLength(254)
  @format("email")
  email: string;

  @doc("Plain-text password, only ever sent over the wire and never stored. At least 8 characters.")
  @minLength(8)
  @maxLength(128)
  @secret
  password: string;
}

@doc("The account that was created. Never contains the password or its hash.")
model RegisteredUser {
  @doc("Identifier of the new account.")
  @format("uuid")
  id: string;

  @doc("Normalised email address of the account.")
  email: string;

  @doc("When the account was created, in UTC.")
  createdAt: utcDateTime;
}

@doc("Problem details for a rejected request, listing the offending fields.")
@error
model ValidationProblemDetails {
  ...ProblemDetails;

  @doc("Validation messages keyed by the request field they apply to.")
  errors: Record<string[]>;
}

@doc("The request was rejected because one or more fields are invalid.")
@error
model BadRequestProblem {
  @statusCode _: 400;
  ...ValidationProblemDetails;
}

@doc("An account already exists for the given email address.")
@error
model ConflictProblem {
  @statusCode _: 409;
  ...ProblemDetails;
}

@route("/auth")
@tag("Identity")
interface Identity {
  @doc("Register a new account. Does not require authentication.")
  @route("/register")
  @post
  register(@body request: RegisterRequest): {
    @statusCode _: 201;
    @body body: RegisteredUser;
  } | BadRequestProblem | ConflictProblem;
}

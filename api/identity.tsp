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

@doc("The credentials of an existing account.")
model LoginRequest {
  @doc("Email address of the account.")
  @minLength(1)
  @maxLength(254)
  email: string;

  @doc("Plain-text password of the account.")
  @minLength(1)
  @maxLength(128)
  @secret
  password: string;
}

@doc("A signed-in session. Send the token as a bearer token on protected requests.")
model LoginResponse {
  @doc("Signed JWT containing the user id and an expiry.")
  accessToken: string;

  @doc("Always Bearer.")
  tokenType: "Bearer";

  @doc("When the token expires, in UTC.")
  expiresAt: utcDateTime;
}

@doc("The credentials are wrong, or the bearer token is missing, invalid or expired. Never reveals whether an email exists.")
@error
model UnauthorizedProblem {
  @statusCode _: 401;
  ...ProblemDetails;
}

@doc("The account of the signed-in user.")
model CurrentUser {
  @doc("Identifier of the account.")
  @format("uuid")
  id: string;

  @doc("Normalised email address of the account.")
  email: string;
}

@route("/auth")
@tag("Identity")
interface Identity {
  @doc("Return the account of the signed-in user. Requires a valid bearer token.")
  @route("/me")
  @get
  me(): CurrentUser | UnauthorizedProblem;

  @doc("Log in with email and password and receive a JWT. Does not require authentication.")
  @route("/login")
  @post
  @useAuth(NoAuth)
  login(@body request: LoginRequest): LoginResponse | BadRequestProblem | UnauthorizedProblem;

  @doc("Register a new account. Does not require authentication.")
  @route("/register")
  @post
  @useAuth(NoAuth)
  register(@body request: RegisterRequest): {
    @statusCode _: 201;
    @body body: RegisteredUser;
  } | BadRequestProblem | ConflictProblem;
}

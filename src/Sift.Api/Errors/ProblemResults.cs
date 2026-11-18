namespace Sift.Api.Errors;

public static class ProblemResults
{
    public static IResult Validation(IReadOnlyDictionary<string, string[]> errors) =>
        Results.ValidationProblem(
            errors.ToDictionary(pair => pair.Key, pair => pair.Value),
            title: "One or more validation errors occurred.",
            statusCode: StatusCodes.Status400BadRequest);

    public static IResult Unauthorized(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication failed.",
            detail: detail);

    public static IResult InvalidToken() => Unauthorized("The access token is invalid.");

    // One fixed body so a foreign resource looks exactly like a missing one.
    public static IResult NotFound() =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "The resource was not found.");

    public static IResult Conflict(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "The request conflicts with the current state of the resource.",
            detail: detail);
}

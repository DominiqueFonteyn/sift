namespace Sift.Api.Errors;

public static class ProblemResults
{
    public static IResult Validation(IReadOnlyDictionary<string, string[]> errors) =>
        Results.ValidationProblem(
            errors.ToDictionary(pair => pair.Key, pair => pair.Value),
            title: "One or more validation errors occurred.",
            statusCode: StatusCodes.Status400BadRequest);

    public static IResult Conflict(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "The request conflicts with the current state of the resource.",
            detail: detail);
}

using Microsoft.AspNetCore.Diagnostics;

namespace Sift.Api.Errors;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddSiftProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Type ??= $"https://httpstatuses.io/{context.HttpContext.Response.StatusCode}";
        });

        return services;
    }

    public static WebApplication UseSiftProblemDetails(this WebApplication app)
    {
        app.UseExceptionHandler(builder => builder.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Sift.Api.Errors");

            if (feature is not null)
            {
                logger.LogError(feature.Error, "Unhandled exception for {Path}", context.Request.Path);
            }

            await Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred.")
                .ExecuteAsync(context);
        }));

        app.UseStatusCodePages(async context => await Results.Problem(statusCode: context.HttpContext.Response.StatusCode)
            .ExecuteAsync(context.HttpContext));

        return app;
    }
}

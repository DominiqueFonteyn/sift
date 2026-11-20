using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sift.Api.Bureaus;
using Sift.Api.Errors;
using Sift.Api.Health;
using Sift.Api.Identity;
using Sift.Api.Requirements;
using Sift.Api.Resumes;
using Sift.Api.Vacancies;
using Sift.Application.Bureaus;
using Sift.Application.Health;
using Sift.Application.Identity;
using Sift.Application.Requirements;
using Sift.Application.Resumes;
using Sift.Application.Vacancies;
using Sift.Infrastructure;
using Sift.Infrastructure.Identity;
using Sift.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Sift") ?? "Data Source=sift.db";

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddSiftProblemDetails();
builder.Services.AddSiftInfrastructure(connectionString, jwtOptions);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = JwtValidation.CreateParameters(jwtOptions);
    });
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build());
builder.Services.AddScoped<CheckHealthQueryHandler>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginUserHandler>();
builder.Services.AddScoped<CreateBureauHandler>();
builder.Services.AddScoped<CreateRequirementHandler>();
builder.Services.AddScoped<DeleteRequirementHandler>();
builder.Services.AddScoped<SaveMyResumeHandler>();
builder.Services.AddScoped<CreateVacancyHandler>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseSiftProblemDetails();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<SiftDbContext>().Database.MigrateAsync();
}

app.MapHealthEndpoints();
app.MapIdentityEndpoints();
app.MapBureauEndpoints();
app.MapRequirementEndpoints();
app.MapResumeEndpoints();
app.MapVacancyEndpoints();

await app.RunAsync();

public partial class Program;

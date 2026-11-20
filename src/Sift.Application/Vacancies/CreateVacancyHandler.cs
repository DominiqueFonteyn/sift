using Sift.Application.Common;
using Sift.Domain.Vacancies;

namespace Sift.Application.Vacancies;

public sealed class CreateVacancyHandler(IVacancyRepository vacancies, IClock clock)
{
    public const string TitleField = "title";
    public const string CompanyField = "company";
    public const string RawTextField = "rawText";

    public async Task<CreateVacancyResult> HandleAsync(CreateVacancyCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Vacancy.TryValidateTitle(command.Title, out var titleError))
        {
            errors[TitleField] = [titleError!];
        }

        if (!Vacancy.TryValidateCompany(command.Company, out var companyError))
        {
            errors[CompanyField] = [companyError!];
        }

        if (!Vacancy.TryValidateRawText(command.RawText, out var rawTextError))
        {
            errors[RawTextField] = [rawTextError!];
        }

        if (errors.Count > 0)
        {
            return new CreateVacancyResult.Invalid(errors);
        }

        var vacancy = Vacancy.Create(Guid.CreateVersion7(), command.OwnerId, command.Title!, command.Company!, command.RawText!, clock.UtcNow);
        await vacancies.AddAsync(vacancy, cancellationToken);

        return new CreateVacancyResult.Created(vacancy);
    }
}

using Sift.Application.Bureaus;
using Sift.Application.Common;
using Sift.Domain.Recruiters;

namespace Sift.Application.Recruiters;

public sealed class CreateRecruiterHandler(IRecruiterRepository recruiters, IBureauQueries bureaus, IClock clock)
{
    public async Task<CreateRecruiterResult> HandleAsync(CreateRecruiterCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(Recruiter.Validate(command.Name, command.Email, command.Phone, command.LinkedInUrl));

        Guid? bureauId = null;
        if (!string.IsNullOrWhiteSpace(command.BureauId))
        {
            if (Guid.TryParse(command.BureauId.Trim(), out var parsed))
            {
                bureauId = parsed;
            }
            else
            {
                errors[Recruiter.BureauIdField] = ["Bureau must be a valid identifier."];
            }
        }

        // Validation first, so a bad request never reveals whether a bureau exists.
        if (errors.Count > 0)
        {
            return new CreateRecruiterResult.Invalid(errors);
        }

        BureauReadModel? bureau = null;
        if (bureauId is { } id)
        {
            bureau = await bureaus.GetAsync(command.OwnerId, id, cancellationToken);
            if (bureau is null)
            {
                return new CreateRecruiterResult.BureauNotFound();
            }
        }

        var recruiter = Recruiter.Create(
            Guid.CreateVersion7(),
            command.OwnerId,
            command.Name!,
            command.Email,
            command.Phone,
            command.LinkedInUrl,
            clock.UtcNow,
            bureau?.Id);
        await recruiters.AddAsync(recruiter, cancellationToken);

        return new CreateRecruiterResult.Created(recruiter, bureau);
    }
}

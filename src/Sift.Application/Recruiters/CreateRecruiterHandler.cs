using Sift.Application.Common;
using Sift.Domain.Recruiters;

namespace Sift.Application.Recruiters;

public sealed class CreateRecruiterHandler(IRecruiterRepository recruiters, IClock clock)
{
    public async Task<CreateRecruiterResult> HandleAsync(CreateRecruiterCommand command, CancellationToken cancellationToken)
    {
        var errors = Recruiter.Validate(command.Name, command.Email, command.Phone, command.LinkedInUrl);
        if (errors.Count > 0)
        {
            return new CreateRecruiterResult.Invalid(errors);
        }

        var recruiter = Recruiter.Create(
            Guid.CreateVersion7(),
            command.OwnerId,
            command.Name!,
            command.Email,
            command.Phone,
            command.LinkedInUrl,
            clock.UtcNow);
        await recruiters.AddAsync(recruiter, cancellationToken);

        return new CreateRecruiterResult.Created(recruiter);
    }
}

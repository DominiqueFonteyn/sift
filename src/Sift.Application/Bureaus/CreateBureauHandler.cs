using Sift.Application.Common;
using Sift.Domain.Bureaus;

namespace Sift.Application.Bureaus;

public sealed class CreateBureauHandler(IBureauRepository bureaus, IClock clock)
{
    public const string NameField = "name";
    public const string WebsiteField = "website";

    public async Task<CreateBureauResult> HandleAsync(CreateBureauCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Bureau.TryValidateName(command.Name, out var nameError))
        {
            errors[NameField] = [nameError!];
        }

        if (!Bureau.TryValidateWebsite(command.Website, out var websiteError))
        {
            errors[WebsiteField] = [websiteError!];
        }

        if (errors.Count > 0)
        {
            return new CreateBureauResult.Invalid(errors);
        }

        var bureau = Bureau.Create(Guid.CreateVersion7(), command.OwnerId, command.Name!, command.Website, clock.UtcNow);
        await bureaus.AddAsync(bureau, cancellationToken);

        return new CreateBureauResult.Created(bureau);
    }
}

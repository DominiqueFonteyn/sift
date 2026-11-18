using Sift.Application.Common;
using Sift.Domain.Bureaus;

namespace Sift.Application.Bureaus;

public sealed class CreateBureauHandler(IBureauRepository bureaus, IClock clock)
{
    public const string NameField = "name";

    public async Task<CreateBureauResult> HandleAsync(CreateBureauCommand command, CancellationToken cancellationToken)
    {
        if (!Bureau.TryValidateName(command.Name, out var error))
        {
            return new CreateBureauResult.Invalid(new Dictionary<string, string[]> { [NameField] = [error!] });
        }

        var bureau = Bureau.Create(Guid.CreateVersion7(), command.OwnerId, command.Name!, clock.UtcNow);
        await bureaus.AddAsync(bureau, cancellationToken);

        return new CreateBureauResult.Created(bureau);
    }
}

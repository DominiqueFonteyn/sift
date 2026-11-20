using Sift.Application.Common;
using Sift.Domain.Requirements;

namespace Sift.Application.Requirements;

public sealed class CreateRequirementHandler(IRequirementRepository requirements, IClock clock)
{
    public const string TextField = "text";

    public const string KindField = "kind";

    public async Task<CreateRequirementResult> HandleAsync(CreateRequirementCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Requirement.TryValidateText(command.Text, out var textError))
        {
            errors[TextField] = [textError!];
        }

        if (!RequirementKinds.TryParse(command.Kind, out var kind))
        {
            errors[KindField] = [$"The kind must be \"{RequirementKinds.MustHave}\" or \"{RequirementKinds.NiceToHave}\"."];
        }

        if (errors.Count > 0)
        {
            return new CreateRequirementResult.Invalid(errors);
        }

        var requirement = Requirement.Create(Guid.CreateVersion7(), command.OwnerId, command.Text!, kind, clock.UtcNow);

        if (await requirements.TextExistsAsync(command.OwnerId, requirement.NormalizedText, cancellationToken))
        {
            return new CreateRequirementResult.Duplicate();
        }

        return await requirements.TryAddAsync(requirement, cancellationToken)
            ? new CreateRequirementResult.Created(requirement)
            : new CreateRequirementResult.Duplicate();
    }
}

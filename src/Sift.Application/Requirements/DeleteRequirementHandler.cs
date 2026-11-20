namespace Sift.Application.Requirements;

public sealed class DeleteRequirementHandler(IRequirementRepository requirements)
{
    public async Task<DeleteRequirementResult> HandleAsync(DeleteRequirementCommand command, CancellationToken cancellationToken) =>
        await requirements.DeleteAsync(command.OwnerId, command.RequirementId, cancellationToken)
            ? new DeleteRequirementResult.Deleted()
            : new DeleteRequirementResult.NotFound();
}

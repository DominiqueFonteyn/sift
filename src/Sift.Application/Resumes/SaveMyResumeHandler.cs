using Sift.Application.Common;
using Sift.Domain.Resumes;

namespace Sift.Application.Resumes;

public sealed class SaveMyResumeHandler(IResumeRepository resumes, IClock clock)
{
    public const string TextField = "text";

    public async Task<SaveMyResumeResult> HandleAsync(SaveMyResumeCommand command, CancellationToken cancellationToken)
    {
        if (!Resume.TryValidateText(command.Text, out var error))
        {
            return new SaveMyResumeResult.Invalid(new Dictionary<string, string[]> { [TextField] = [error!] });
        }

        var now = clock.UtcNow;
        var existing = await resumes.FindByOwnerAsync(command.OwnerId, cancellationToken);

        if (existing is null)
        {
            var created = Resume.Create(command.OwnerId, command.Text!, now);
            await resumes.AddAsync(created, cancellationToken);

            return new SaveMyResumeResult.Saved(created);
        }

        existing.Replace(command.Text!, now);
        await resumes.SaveAsync(existing, cancellationToken);

        return new SaveMyResumeResult.Saved(existing);
    }
}

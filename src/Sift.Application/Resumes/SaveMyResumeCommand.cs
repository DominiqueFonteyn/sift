namespace Sift.Application.Resumes;

public sealed record SaveMyResumeCommand(Guid OwnerId, string? Text);

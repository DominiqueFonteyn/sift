namespace Sift.Application.Bureaus;

// The owner comes from the authenticated identity, never from the request body.
public sealed record CreateBureauCommand(Guid OwnerId, string? Name);

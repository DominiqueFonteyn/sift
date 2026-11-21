namespace Sift.Api.Bureaus;

// Unknown members such as an owner id are not bound, so they cannot spoof ownership.
public sealed record CreateBureauRequest(string? Name, string? Website);

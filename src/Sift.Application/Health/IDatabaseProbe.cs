namespace Sift.Application.Health;

public interface IDatabaseProbe
{
    Task<bool> CanReadAsync(CancellationToken cancellationToken);
}

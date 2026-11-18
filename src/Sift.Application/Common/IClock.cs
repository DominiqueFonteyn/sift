namespace Sift.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

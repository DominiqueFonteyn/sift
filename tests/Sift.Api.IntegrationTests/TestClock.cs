using Sift.Application.Common;

namespace Sift.Api.IntegrationTests;

// Follows the real clock until a test pins it.
public sealed class TestClock : IClock
{
    private DateTimeOffset? fixedTime;

    public DateTimeOffset UtcNow => fixedTime ?? DateTimeOffset.UtcNow;

    public void Set(DateTimeOffset time) => fixedTime = time;

    public void Reset() => fixedTime = null;
}

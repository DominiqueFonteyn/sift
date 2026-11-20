using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sift.Application.Requirements;
using Sift.Domain.Requirements;

namespace Sift.Api.IntegrationTests.Requirements;

// Bypasses TextExistsAsync to exercise the unique-index path a lost race would take.
public sealed class RequirementRepositoryTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_second_insert_of_the_same_text_is_reported_as_a_duplicate()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var ownerId = await AuthenticatedClient.GetUserIdAsync(client);

        Assert.True(await TryAddAsync(Requirement.Create(Guid.NewGuid(), ownerId, "Remote-first", RequirementKind.MustHave, Now)));
        Assert.False(await TryAddAsync(Requirement.Create(Guid.NewGuid(), ownerId, " REMOTE-FIRST ", RequirementKind.NiceToHave, Now)));
    }

    [Fact]
    public async Task An_insert_for_an_unknown_owner_is_not_mistaken_for_a_duplicate() =>
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            TryAddAsync(Requirement.Create(Guid.NewGuid(), Guid.NewGuid(), "Remote-first", RequirementKind.MustHave, Now)));

    private async Task<bool> TryAddAsync(Requirement requirement)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IRequirementRepository>()
            .TryAddAsync(requirement, CancellationToken.None);
    }
}

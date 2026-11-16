using System.Net;
using Claims.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Claims.Tests.Integration;

public class AuditingApiTests(ApiFixture fixture) : ApiTestBase(fixture)
{
    private readonly ApiFixture _fixture = fixture;

    [Fact]
    public async Task Create_and_delete_are_eventually_saved_to_sql_server()
    {
        var cover = await CreateCoverAsync(Today.AddDays(1), Today.AddDays(30));
        Assert.Equal(HttpStatusCode.OK, (await Client.DeleteAsync($"/Covers/{cover.Id}", Ct)).StatusCode);

        var types = await WaitForCoverAuditsAsync(cover.Id, expectedCount: 2);

        Assert.Equal(["DELETE", "POST"], types.Order());
    }

    [Fact]
    public async Task Failed_delete_is_not_audited()
    {
        const string unknownId = "unknown-cover-for-audit-test";
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync($"/Covers/{unknownId}", Ct)).StatusCode);

        // Create and audit another cover, so we know the queue has processed the earlier request too.
        var marker = await CreateCoverAsync(Today.AddDays(1), Today.AddDays(30));
        await WaitForCoverAuditsAsync(marker.Id, expectedCount: 1);

        Assert.Empty(await WaitForCoverAuditsAsync(unknownId, expectedCount: 0));
    }

    // Audits are saved in the background, so poll SQL Server. The first write to a new container can be slow.
    private async Task<List<string>> WaitForCoverAuditsAsync(string coverId, int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
            var types = await context.CoverAudits
                .Where(audit => audit.CoverId == coverId)
                .Select(audit => audit.HttpRequestType)
                .ToListAsync(Ct);

            if (types.Count >= expectedCount || DateTime.UtcNow > deadline)
            {
                Assert.Equal(expectedCount, types.Count);
                return types;
            }

            await Task.Delay(100, Ct);
        }
    }
}

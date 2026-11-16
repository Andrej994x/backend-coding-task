using System.Net.Http.Json;
using Claims.Domain;
using Xunit;

namespace Claims.Tests.Integration;

[Trait("Category", "Integration")]
public abstract class ApiTestBase(ApiFixture fixture)
{
    protected static readonly DateTime Today = DateTime.UtcNow.Date;
    protected readonly HttpClient Client = fixture.CreateClient();
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected Task<HttpResponseMessage> PostAsync(string url, object body) =>
        Client.PostAsJsonAsync(url, body, ApiFixture.Json, Ct);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(ApiFixture.Json, Ct))!;

    protected async Task<Cover> CreateCoverAsync(DateTime start, DateTime end, CoverType type = CoverType.Yacht)
    {
        var response = await PostAsync("/Covers", new { startDate = start, endDate = end, type });
        response.EnsureSuccessStatusCode();
        return await ReadAsync<Cover>(response);
    }
}

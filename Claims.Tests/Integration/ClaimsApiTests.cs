using System.Net;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.Tests.Integration;

public class ClaimsApiTests(ApiFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Create_get_and_delete_claim()
    {
        var cover = await CreateCoverAsync(Today.AddDays(1), Today.AddDays(60));

        // The client does not send an id, the server generates it.
        var createResponse = await PostAsync("/Claims", new
        {
            coverId = cover.Id,
            created = Today.AddDays(5).ToString("yyyy-MM-dd"),
            name = "Engine fire",
            type = "Fire",
            damageCost = 100_000m
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = await ReadAsync<Claim>(createResponse);
        Assert.False(string.IsNullOrEmpty(created.Id));
        // A date without offset is treated as UTC and must not be shifted by the server time zone.
        Assert.Equal(Today.AddDays(5), created.Created.ToUniversalTime());

        var fetched = await ReadAsync<Claim>(await Client.GetAsync($"/Claims/{created.Id}", Ct));
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(Today.AddDays(5), fetched.Created.ToUniversalTime());

        Assert.Equal(HttpStatusCode.OK, (await Client.DeleteAsync($"/Claims/{created.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/Claims/{created.Id}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Invalid_claim_is_rejected_and_not_saved()
    {
        var cover = await CreateCoverAsync(Today.AddDays(1), Today.AddDays(60));
        const string name = "too-expensive-claim";

        var response = await PostAsync("/Claims", new
        {
            coverId = cover.Id,
            created = Today.AddDays(5),
            name,
            type = "Fire",
            damageCost = 100_001m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains(nameof(Claim.DamageCost), problem.Errors.Keys);

        var all = await ReadAsync<List<Claim>>(await Client.GetAsync("/Claims", Ct));
        Assert.DoesNotContain(all, claim => claim.Name == name);
    }

    [Fact]
    public async Task Created_with_offset_is_compared_in_utc()
    {
        var cover = await CreateCoverAsync(Today.AddDays(1), Today.AddDays(60));
        object ClaimCreatedAt(string created) => new { coverId = cover.Id, name = "n", type = "Fire", damageCost = 10m, created };

        // 01:00 at +02:00 on the day after the cover ends is still the last cover day in UTC.
        var lastDay = await PostAsync("/Claims", ClaimCreatedAt($"{Today.AddDays(61):yyyy-MM-dd}T01:00:00+02:00"));
        var dayAfter = await PostAsync("/Claims", ClaimCreatedAt($"{Today.AddDays(61):yyyy-MM-dd}T03:00:00+02:00"));

        Assert.Equal(HttpStatusCode.OK, lastDay.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, dayAfter.StatusCode);
    }

    [Fact]
    public async Task Claim_without_damage_cost_is_rejected()
    {
        var cover = await CreateCoverAsync(Today.AddDays(1), Today.AddDays(60));

        var response = await PostAsync("/Claims", new { coverId = cover.Id, created = Today.AddDays(5), name = "n", type = "Fire" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_claim_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/Claims/unknown", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/Claims/unknown", Ct)).StatusCode);
    }
}

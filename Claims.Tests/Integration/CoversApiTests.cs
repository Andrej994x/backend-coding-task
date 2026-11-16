using System.Net;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.Tests.Integration;

public class CoversApiTests(ApiFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Create_get_and_delete_cover()
    {
        var cover = await CreateCoverAsync(Today, Today.AddYears(1), CoverType.Tanker);
        Assert.False(string.IsNullOrEmpty(cover.Id));
        Assert.True(cover.Premium > 0);

        var fetched = await ReadAsync<Cover>(await Client.GetAsync($"/Covers/{cover.Id}", Ct));
        Assert.Equal(cover.Id, fetched.Id);

        Assert.Equal(HttpStatusCode.OK, (await Client.DeleteAsync($"/Covers/{cover.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/Covers/{cover.Id}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Cover_starting_in_the_past_is_rejected_and_not_saved()
    {
        var response = await PostAsync("/Covers", new { startDate = Today.AddDays(-1), endDate = Today.AddDays(30), type = "Yacht" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains(nameof(Cover.StartDate), problem.Errors.Keys);
        var all = await ReadAsync<List<Cover>>(await Client.GetAsync("/Covers", Ct));
        Assert.DoesNotContain(all, c => c.StartDate.ToUniversalTime().Date == Today.AddDays(-1));
    }

    [Fact]
    public async Task Compute_premium_validates_period()
    {
        var start = Today.AddDays(1).ToString("yyyy-MM-dd");
        var tooLate = Today.AddDays(1).AddYears(1).AddDays(1).ToString("yyyy-MM-dd");

        var ok = await Client.PostAsync($"/Covers/compute?startDate={start}&endDate={Today.AddDays(31):yyyy-MM-dd}&coverType=Yacht", null, Ct);
        var bad = await Client.PostAsync($"/Covers/compute?startDate={start}&endDate={tooLate}&coverType=Yacht", null, Ct);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Unknown_cover_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/Covers/unknown", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/Covers/unknown", Ct)).StatusCode);
    }
}

using System.Net;
using Claims.Domain;
using Claims.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        Assert.Equal(41_250m, await ReadAsync<decimal>(ok)); // 30 days of Yacht at full price
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Unknown_cover_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/Covers/unknown", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/Covers/unknown", Ct)).StatusCode);
    }

    [Fact]
    public async Task Mongo_provider_reports_a_concurrent_delete_as_concurrency_exception()
    {
        var cover = await CreateCoverAsync(Today.AddDays(1), Today.AddDays(30));
        using var first = Fixture.Factory.Services.CreateScope();
        using var second = Fixture.Factory.Services.CreateScope();
        var firstContext = first.ServiceProvider.GetRequiredService<ClaimsContext>();
        var secondRepository = second.ServiceProvider.GetRequiredService<ICoverRepository>();

        // The repositories rely on this: the first request has read the cover, then another request deletes it.
        var loaded = await firstContext.Covers.SingleAsync(c => c.Id == cover.Id, Ct);
        Assert.True(await secondRepository.DeleteAsync(cover.Id));
        firstContext.Covers.Remove(loaded);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => firstContext.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Validation_error_is_400_even_when_client_asks_for_xml()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/Covers/compute?startDate={Today:yyyy-MM-dd}&endDate={Today.AddDays(-1):yyyy-MM-dd}&coverType=Yacht");
        request.Headers.Accept.ParseAdd("application/xml");

        var response = await Client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

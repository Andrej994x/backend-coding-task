using System.Text.Json;
using System.Text.Json.Serialization;
using Claims.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: AssemblyFixture(typeof(ApiFixture))]

namespace Claims.Tests.Integration;

/// <summary>
/// Starts the real API once for all integration tests. Program.cs starts MongoDB and SQL Server
/// with Testcontainers, so Docker must be running.
/// </summary>
public sealed class ApiFixture : IAsyncDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // The API clock is fixed, so tests do not break when the run crosses midnight.
    public static readonly DateTime Today = DateTime.UtcNow.Date;

    public WebApplicationFactory<Program> Factory { get; } = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Today.AddHours(12)))));

    public HttpClient CreateClient() => Factory.CreateClient();

    public ValueTask DisposeAsync() => Factory.DisposeAsync();
}

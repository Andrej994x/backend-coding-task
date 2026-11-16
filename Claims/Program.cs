using Claims.Auditing;
using Claims.Persistence;
using Claims.Services;
using Claims.Validation;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server and MongoDB
var sqlContainer = new MsSqlBuilder(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? "mcr.microsoft.com/mssql/server:2022-latest"
        : "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
    .Build();

var mongoContainer = new MongoDbBuilder("mongo:latest")
    .Build();

await sqlContainer.StartAsync();
await mongoContainer.StartAsync();

builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(sqlContainer.GetConnectionString()));

// Create one MongoClient for the whole app. A new client per DbContext makes EF build a new internal service provider every time.
var mongoClient = new MongoClient(mongoContainer.GetConnectionString());
builder.Services.AddDbContext<ClaimsContext>(options =>
    options.UseMongoDB(mongoClient, builder.Configuration["MongoDb:DatabaseName"]!));

// Application services
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<ICoverRepository, CoverRepository>();
builder.Services.AddScoped<IAuditer, Auditer>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<CoverService>();
builder.Services.AddSingleton<PremiumCalculator>();
builder.Services.AddSingleton<CoverValidator>();
builder.Services.AddSingleton(TimeProvider.System);

// Business rule violations are returned as 400 Bad Request.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }

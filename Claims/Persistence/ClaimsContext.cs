using Claims.Domain;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Persistence;

/// <summary>MongoDB context that stores claims and covers.</summary>
public class ClaimsContext(DbContextOptions<ClaimsContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims { get; init; } = null!;
    public DbSet<Cover> Covers { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Claim>().ToCollection("claims");
        modelBuilder.Entity<Cover>().ToCollection("covers");
    }
}

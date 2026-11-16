using Microsoft.EntityFrameworkCore;

namespace Claims.Auditing;

/// <summary>SQL Server context that stores audit records.</summary>
public class AuditContext(DbContextOptions<AuditContext> options) : DbContext(options)
{
    public DbSet<ClaimAudit> ClaimAudits { get; set; } = null!;
    public DbSet<CoverAudit> CoverAudits { get; set; } = null!;
}

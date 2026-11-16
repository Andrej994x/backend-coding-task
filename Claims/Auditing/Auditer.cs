namespace Claims.Auditing;

public interface IAuditer
{
    void AuditClaim(string claimId, string httpRequestType);
    void AuditCover(string coverId, string httpRequestType);
}

public class Auditer(AuditContext auditContext) : IAuditer
{
    public void AuditClaim(string claimId, string httpRequestType)
    {
        auditContext.ClaimAudits.Add(new ClaimAudit
        {
            ClaimId = claimId,
            Created = DateTime.Now,
            HttpRequestType = httpRequestType
        });
        auditContext.SaveChanges();
    }

    public void AuditCover(string coverId, string httpRequestType)
    {
        auditContext.CoverAudits.Add(new CoverAudit
        {
            CoverId = coverId,
            Created = DateTime.Now,
            HttpRequestType = httpRequestType
        });
        auditContext.SaveChanges();
    }
}

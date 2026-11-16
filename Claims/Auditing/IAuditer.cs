namespace Claims.Auditing;

/// <summary>Records that a claim or cover was created or deleted.</summary>
public interface IAuditer
{
    ValueTask AuditClaimAsync(string claimId, string httpRequestType);
    ValueTask AuditCoverAsync(string coverId, string httpRequestType);
}

namespace Claims.Auditing;

public enum AuditEntityType
{
    Claim,
    Cover
}

/// <summary>An audit event waiting in the queue. <see cref="Created"/> is the UTC time when it was queued.</summary>
public record AuditEntry(AuditEntityType EntityType, string EntityId, string HttpRequestType, DateTime Created);

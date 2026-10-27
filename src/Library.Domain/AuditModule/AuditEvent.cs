namespace Library.Domain.AuditModule;

public sealed class AuditEvent : Entity
{
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Actor { get; private set; } = string.Empty;
    public DateTime TimestampUtc { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    public string Details { get; private set; } = string.Empty;

    private AuditEvent() { }

    public AuditEvent(
        string entityName,
        string entityId,
        string action,
        string actor,
        string correlationId,
        string details)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("O nome da entidade auditada é obrigatório.", nameof(entityName));
        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("O identificador da entidade é obrigatório.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("A ação de auditoria é obrigatória.", nameof(action));

        Id = Guid.NewGuid();
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        Actor = string.IsNullOrWhiteSpace(actor) ? "System" : actor;
        CorrelationId = correlationId;
        TimestampUtc = DateTime.UtcNow;
        Details = details;
    }
}

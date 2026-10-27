using Library.Domain.Repository;

namespace Library.Domain.Audit;

public interface IAuditEventRepository : ICreateRepository<AuditEvent>
{ }

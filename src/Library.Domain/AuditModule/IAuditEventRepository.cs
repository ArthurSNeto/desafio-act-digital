using Library.Domain.Repository;

namespace Library.Domain.AuditModule;

public interface IAuditEventRepository : ICreateRepository<AuditEvent>
{ 
    Task<List<AuditEvent>> GetAllAsync(CancellationToken cancellationToken);
}

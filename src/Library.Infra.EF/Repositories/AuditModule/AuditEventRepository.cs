using Library.Domain.AuditModule;
using Library.Infra.EF.Context;
using Microsoft.EntityFrameworkCore;

namespace Library.Infra.EF.Repositories.AuditModule;

public class AuditEventRepository : GenericRepositoryBase<AuditEvent>, IAuditEventRepository
{
    public AuditEventRepository(IApplicationDbContext dbContext)
        : base(dbContext)
    { }

    public Task<AuditEvent> CreateAsync(AuditEvent entity, CancellationToken cancellationToken)
    {
        return GenericRepository.CreateAsync(entity, cancellationToken);
    }

    public Task<List<AuditEvent>> GetAllAsync(CancellationToken cancellationToken)
    {
        return GenericRepository.Context.Set<AuditEvent>().ToListAsync(cancellationToken);
    }
}

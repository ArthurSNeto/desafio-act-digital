using Library.Domain.Audit;
using Library.Infra.EF.Context;

namespace Library.Infra.EF.Repositories.Audit;

public class AuditEventRepository : GenericRepositoryBase<AuditEvent>, IAuditEventRepository
{
    public AuditEventRepository(IApplicationDbContext dbContext)
        : base(dbContext)
    { }

    public Task<AuditEvent> CreateAsync(AuditEvent entity, CancellationToken cancellationToken)
    {
        return GenericRepository.CreateAsync(entity, cancellationToken);
    }
}

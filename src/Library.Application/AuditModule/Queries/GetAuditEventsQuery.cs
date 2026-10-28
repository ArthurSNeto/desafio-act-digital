using CSharpFunctionalExtensions;
using Library.Domain.AuditModule;
using MediatR;

namespace Library.Application.AuditModule.Queries;

public sealed class GetAuditEventsQuery : IRequest<Result<List<AuditEvent>>>
{ }

public sealed class GetAuditEventsQueryHandler(IAuditEventRepository auditEventRepository) : IRequestHandler<GetAuditEventsQuery, Result<List<AuditEvent>>>
{
    public async Task<Result<List<AuditEvent>>> Handle(GetAuditEventsQuery request, CancellationToken cancellationToken)
    {
        List<AuditEvent> auditEvents = await auditEventRepository.GetAllAsync(cancellationToken);

        return Result.Success(auditEvents);
    }
}

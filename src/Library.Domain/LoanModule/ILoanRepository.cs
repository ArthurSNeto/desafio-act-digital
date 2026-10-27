using Library.Domain.Repository;

namespace Library.Domain.LoanModule;

public interface ILoanRepository :
    IRetrieveByIDRepository<Loan>,
    ICreateRepository<Loan>,
    IUpdateRepository<Loan>
{
    Task<Loan?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<List<Loan>> ListByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<List<Loan>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLoanAsync(Guid bookId, Guid userId, CancellationToken cancellationToken = default);
}

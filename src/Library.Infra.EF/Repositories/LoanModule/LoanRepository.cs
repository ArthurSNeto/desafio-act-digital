using Library.Domain.LoanModule;
using Library.Infra.EF.Context;
using Microsoft.EntityFrameworkCore;

namespace Library.Infra.EF.Repositories.LoanModule;

public class LoanRepository : GenericRepositoryBase<Loan>, ILoanRepository
{
    public LoanRepository(IApplicationDbContext dbContext)
        : base(dbContext)
    { }

    public Task<Loan> CreateAsync(Loan entity, CancellationToken cancellationToken)
    {
        return GenericRepository.CreateAsync(entity, cancellationToken);
    }

    public Task<Loan?> RetrieveByIDAsync(Guid id, CancellationToken cancellationToken)
    {
        return GenericRepository.RetrieveByIDAsync(id, cancellationToken);
    }

    public void Update(Loan entity)
    {
        GenericRepository.Update(entity);
    }

    public Task<Loan?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return GenericRepository.Context.Set<Loan>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task<bool> HasActiveLoanAsync(Guid bookId, Guid userId, CancellationToken cancellationToken = default)
    {
        return GenericRepository.Context.Set<Loan>()
            .AnyAsync(l =>
                l.BookId == bookId &&
                l.UserId == userId &&
                l.Status == LoanStatus.Active,
            cancellationToken);
    }

    public Task<List<Loan>> ListByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return GenericRepository.Context.Set<Loan>()
            .AsNoTracking()
            .Where(l => l.BookId == bookId)
            .OrderByDescending(l => l.LoanDateUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Loan>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return GenericRepository.Context.Set<Loan>()
            .AsNoTracking()
            .Include(l => l.Book)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.LoanDateUtc)
            .ToListAsync(cancellationToken);
    }
}

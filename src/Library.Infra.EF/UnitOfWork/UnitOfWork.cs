using Library.Domain.Exceptions;
using Library.Domain.UnitOfWork;
using Library.Infra.EF.Context;
using Microsoft.EntityFrameworkCore;

namespace Library.Infra.EF.UnitOfWork;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "O registro foi alterado ou emprestado por outra requisição simultânea. Tente novamente.");
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

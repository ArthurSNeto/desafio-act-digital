using Library.Domain.BookModule;
using Library.Infra.EF.Context;
using Microsoft.EntityFrameworkCore;

namespace Library.Infra.EF.Repositories.BookModule;

public class BookRepository : GenericRepositoryBase<Book>, IBookRepository
{
    public BookRepository(IApplicationDbContext dbContext)
        : base(dbContext)
    { }

    public Task<Book> CreateAsync(Book entity, CancellationToken cancellationToken)
    {
        return GenericRepository.CreateAsync(entity, cancellationToken);
    }

    public Task<Book?> RetrieveByIDAsync(Guid id, CancellationToken cancellationToken)
    {
        return GenericRepository.RetrieveByIDAsync(id, cancellationToken);
    }

    public void Update(Book entity)
    {
        GenericRepository.Update(entity);
    }

    public void Delete(Book entity)
    {
        GenericRepository.Delete(entity);
    }

    public Task<bool> ExistsByIsbnAsync(Guid id, string isbn, CancellationToken cancellationToken = default)
    {
        return GenericRepository.Context.Set<Book>().AnyAsync(b => b.Isbn == isbn && b.Id != id, cancellationToken);
    }

    public Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        return GenericRepository.Context.Set<Book>().FirstOrDefaultAsync(b => b.Isbn == isbn, cancellationToken);
    }

    public Task<List<Book>> ListAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        return GenericRepository.Context.Set<Book>().Where(b => b.IsActive == onlyActive).ToListAsync(cancellationToken);
    }

    public Task<bool> HasLoanHistoryAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

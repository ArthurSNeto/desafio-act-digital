using Library.Domain.Repository;

namespace Library.Domain.BookModule;

public interface IBookRepository :
    ICreateRepository<Book>,
    IRetrieveByIDRepository<Book>,
    IUpdateRepository<Book>,
    IDeleteRepository<Book>
{
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
    Task<List<Book>> ListAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<bool> HasLoanHistoryAsync(Guid bookId, CancellationToken cancellationToken = default);
}

using CSharpFunctionalExtensions;
using Library.Application.BookModule.Models;
using Library.Domain.BookModule;
using MediatR;

namespace Library.Application.BookModule.Queries;

public sealed class ListBooksQuery : IRequest<Result<IReadOnlyList<BookResponse>>>
{
    public bool OnlyActive { get; set; }

    public ListBooksQuery(bool onlyActive)
    {
        OnlyActive = onlyActive;
    }
}

public sealed class ListBooksQueryHandler(IBookRepository bookRepository) : IRequestHandler<ListBooksQuery, Result<IReadOnlyList<BookResponse>>>
{
    public async Task<Result<IReadOnlyList<BookResponse>>> Handle(ListBooksQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Book> books = await bookRepository.ListAsync(request.OnlyActive, cancellationToken);
        List<BookResponse> bookResponses = books.Select(b => new BookResponse(
            b.Id, b.Title, b.Author, b.Isbn,
            b.TotalCopies, b.AvailableCopies, b.IsActive,
            b.CreatedAtUtc, b.UpdatedAtUtc))
        .ToList();

        return Result.Success((IReadOnlyList<BookResponse>)bookResponses);
    }
}
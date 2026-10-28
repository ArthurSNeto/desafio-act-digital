using CSharpFunctionalExtensions;
using Library.Application.BookModule.Models;
using Library.Domain.BookModule;
using Library.Infra.Redis;
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

public sealed class ListBooksQueryHandler(
    IBookRepository bookRepository,
    ICacheService cache) : IRequestHandler<ListBooksQuery, Result<IReadOnlyList<BookResponse>>>
{
    public async Task<Result<IReadOnlyList<BookResponse>>> Handle(ListBooksQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"books:catalog:active:{request.OnlyActive.ToString().ToLowerInvariant()}";

        List<BookResponse> response = await cache.GetOrCreateAsync(
            cacheKey,
            factory: async () =>
            {
                List<Book> books = await bookRepository.ListAsync(request.OnlyActive, cancellationToken);

                return books.Select(b => new BookResponse(
                    b.Id, b.Title, b.Author, b.Isbn,
                    b.TotalCopies, b.AvailableCopies, b.IsActive,
                    b.CreatedAtUtc, b.UpdatedAtUtc))
                .ToList();
            },
            expiration: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken
        );

        return Result.Success((IReadOnlyList<BookResponse>)response);
    }
}
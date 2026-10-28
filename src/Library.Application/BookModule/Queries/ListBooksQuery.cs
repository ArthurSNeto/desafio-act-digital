using CSharpFunctionalExtensions;
using Library.Application.BookModule.Models;
using Library.Application.Telemetry;
using Library.Domain.BookModule;
using Library.Infra.Redis;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
    ICacheService cache,
    ILogger<ListBooksQueryHandler> logger) : IRequestHandler<ListBooksQuery, Result<IReadOnlyList<BookResponse>>>
{
    public async Task<Result<IReadOnlyList<BookResponse>>> Handle(ListBooksQuery request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("ListBooks");
        activity?.SetTag("library.catalog.only_active", request.OnlyActive);

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

        activity?.SetTag("library.catalog.total_returned", response.Count);
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation(
            "Catálogo de livros listado (OnlyActive={OnlyActive}). Total de livros retornados: {Count}",
            request.OnlyActive,
            response.Count);

        return Result.Success((IReadOnlyList<BookResponse>)response);
    }
}

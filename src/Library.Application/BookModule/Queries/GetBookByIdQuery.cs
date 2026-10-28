using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Application.Telemetry;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Library.Application.BookModule.Queries;

public sealed class GetBookByIdQuery : IRequest<Result<BookResponse>>
{
    public Guid Id { get; set; }

    public GetBookByIdQuery(Guid id)
    {
        Id = id;
    }
}

public sealed class GetBookByIdQuerydValidator : AbstractValidator<GetBookByIdQuery>
{
    public GetBookByIdQuerydValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class GetBookByIdQueryHandler(
    IBookRepository bookRepository,
    ILogger<GetBookByIdQueryHandler> logger) : IRequestHandler<GetBookByIdQuery, Result<BookResponse>>
{
    public async Task<Result<BookResponse>> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("GetBookById");
        activity?.SetTag("library.book.id", request.Id.ToString());

        Book? book = await bookRepository.RetrieveByIDAsync(request.Id, cancellationToken);
        if (book is null)
        {
            logger.LogWarning("Livro com ID '{BookId}' não encontrado na consulta.", request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Livro não encontrado");
            throw new BusinessRuleException($"Livro com ID '{request.Id}' não encontrado.");
        }

        activity?.SetTag("library.book.title", book.Title);
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation("Livro {BookId} ('{Title}') consultado com sucesso.", book.Id, book.Title);

        return Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}

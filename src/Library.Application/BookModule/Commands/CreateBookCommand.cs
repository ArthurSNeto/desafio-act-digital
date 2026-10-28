using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Application.Interfaces;
using Library.Application.Telemetry;
using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.LoanModule;
using Library.Domain.UnitOfWork;
using Library.Infra.Redis;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Library.Application.BookModule.Commands;

public sealed class CreateBookCommand : IRequest<Result<BookResponse>>
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string Isbn { get; set; }
    public int TotalCopies { get; set; }

    public CreateBookCommand(string title, string author, string isbn, int totalCopies)
    {
        Title = title;
        Author = author;
        Isbn = isbn;
        TotalCopies = totalCopies;
    }
}

public sealed class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty();
        RuleFor(x => x.Author)
            .NotEmpty();
        RuleFor(x => x.Isbn)
            .NotEmpty();
        RuleFor(x => x.TotalCopies)
            .GreaterThan(0);
    }
}

public sealed class CreateBookCommandHandler(
    IBookRepository bookRepository,
    IAuditEventRepository auditRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext userContext,
    ICacheService cache,
    ILogger<CreateBookCommandHandler> logger) : IRequestHandler<CreateBookCommand, Result<BookResponse>>
{
    public async Task<Result<BookResponse>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("CreateBook");
        activity?.SetTag("library.book.title", request.Title);
        activity?.SetTag("library.book.isbn", request.Isbn);
        activity?.SetTag("library.book.total_copies", request.TotalCopies);

        if (await bookRepository.ExistsByIsbnAsync(Guid.Empty, request.Isbn, cancellationToken))
        {
            logger.LogWarning("Tentativa de cadastro com ISBN duplicado: '{Isbn}'", request.Isbn);
            activity?.SetStatus(ActivityStatusCode.Error, "ISBN duplicado");
            throw new BusinessRuleException($"Já existe um livro cadastrado com o ISBN '{request.Isbn}'.");
        }

        Book book = new(request.Title, request.Author, request.Isbn, request.TotalCopies);
        await bookRepository.CreateAsync(book, cancellationToken);

        AuditEvent audit = new(
            entityName: nameof(Book),
            entityId: book.Id.ToString(),
            action: "Created",
            actor: userContext.CurrentUser,
            correlationId: userContext.CorrelationId,
            details: JsonSerializer.Serialize(new
            {
                book.Title,
                book.Author,
                book.Isbn,
                book.TotalCopies
            })
        );
        await auditRepository.CreateAsync(audit, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        activity?.SetTag("library.book.id", book.Id.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation(
            "Livro '{Title}' (ISBN: {Isbn}) criado com sucesso. Id: {BookId}, Exemplares: {TotalCopies}",
            book.Title,
            book.Isbn,
            book.Id,
            book.TotalCopies);

        await BookCacheKeys.InvalidateCatalogAsync(cache, cancellationToken);

        return Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}

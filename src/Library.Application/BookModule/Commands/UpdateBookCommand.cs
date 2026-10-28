using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Application.Interfaces;
using Library.Application.Telemetry;
using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.UnitOfWork;
using Library.Infra.Redis;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Library.Application.BookModule.Commands;

public sealed class UpdateBookCommand : IRequest<Result<BookResponse>>
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public int TotalCopies { get; set; }

    public UpdateBookCommand(Guid id, string? title, string? author, string? isbn, int totalCopies)
    {
        Id = id;
        Title = title;
        Author = author;
        Isbn = isbn;
        TotalCopies = totalCopies;
    }
}

public sealed class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
        RuleFor(x => x.TotalCopies)
            .GreaterThan(0);
    }
}

public sealed class UpdateBookCommandHandler(
    IBookRepository bookRepository,
    IAuditEventRepository auditRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext userContext,
    ICacheService cache,
    ILogger<UpdateBookCommandHandler> logger) : IRequestHandler<UpdateBookCommand, Result<BookResponse>>
{
    public async Task<Result<BookResponse>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("UpdateBook");
        activity?.SetTag("library.book.id", request.Id.ToString());

        Book? book = await bookRepository.RetrieveByIDAsync(request.Id, cancellationToken);
        if (book is null)
        {
            logger.LogWarning("Livro com ID '{BookId}' não encontrado para atualização.", request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Livro não encontrado");
            throw new BusinessRuleException($"Livro com ID '{request.Id}' não encontrado.");
        }

        if (request.Isbn is not null && await bookRepository.ExistsByIsbnAsync(request.Id, request.Isbn, cancellationToken))
        {
            logger.LogWarning("Falha ao atualizar livro {BookId}: ISBN duplicado '{Isbn}'", request.Id, request.Isbn);
            activity?.SetStatus(ActivityStatusCode.Error, "ISBN duplicado");
            throw new BusinessRuleException($"Já existe um livro cadastrado com o ISBN '{request.Isbn}'.");
        }

        book.UpdateDetails(request.Title, request.Author, request.Isbn, request.TotalCopies);

        bookRepository.Update(book);

        AuditEvent audit = new(
            entityName: nameof(Book),
            entityId: book.Id.ToString(),
            action: "Update",
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

        activity?.SetTag("library.book.title", book.Title);
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation(
            "Livro {BookId} ('{Title}') atualizado com sucesso. Exemplares totais: {TotalCopies}",
            book.Id,
            book.Title,
            book.TotalCopies);

        await BookCacheKeys.InvalidateCatalogAsync(cache, cancellationToken);
        await cache.RemoveAsync(BookCacheKeys.Availability(book.Id), cancellationToken);

        return Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}

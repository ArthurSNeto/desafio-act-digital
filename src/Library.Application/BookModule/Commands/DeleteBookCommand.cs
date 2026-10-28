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

public sealed class DeleteBookCommand : IRequest<Result<BookResponse>>
{
    public Guid Id { get; set; }

    public DeleteBookCommand(Guid id)
    {
        Id = id;
    }
}

public sealed class DeleteBookCommandValidator : AbstractValidator<DeleteBookCommand>
{
    public DeleteBookCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class DeleteBookCommandHandler(
    IBookRepository bookRepository,
    IAuditEventRepository auditRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext userContext,
    ICacheService cache,
    ILogger<DeleteBookCommandHandler> logger) : IRequestHandler<DeleteBookCommand, Result<BookResponse>>
{
    public async Task<Result<BookResponse>> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("DeleteBook");
        activity?.SetTag("library.book.id", request.Id.ToString());

        Book? book = await bookRepository.RetrieveByIDAsync(request.Id, cancellationToken);
        if (book is null)
        {
            logger.LogWarning("Livro com ID '{BookId}' não encontrado para remoção/desativação.", request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Livro não encontrado");
            throw new BusinessRuleException($"Livro com ID '{request.Id}' não encontrado.");
        }

        bool hasHistory = await bookRepository.HasLoanHistoryAsync(request.Id, cancellationToken);
        activity?.SetTag("library.book.has_history", hasHistory);

        if (hasHistory)
        {
            book.Deactivate();
            bookRepository.Update(book);
            logger.LogInformation("Livro {BookId} ('{Title}') desativado (soft delete) devido a histórico existente de empréstimos.", book.Id, book.Title);
        }
        else
        {
            bookRepository.Delete(book);
            logger.LogInformation("Livro {BookId} ('{Title}') removido fisicamente por não possuir histórico de empréstimos.", book.Id, book.Title);
        }

        AuditEvent audit = new(
            entityName: nameof(Book),
            entityId: book.Id.ToString(),
            action: hasHistory ? "Deactivated" : "Deleted",
            actor: userContext.CurrentUser,
            correlationId: userContext.CorrelationId,
            details: JsonSerializer.Serialize(new
            {
                book.IsActive,
                HasLoanHistory = hasHistory
            })
        );
        await auditRepository.CreateAsync(audit, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        activity?.SetStatus(ActivityStatusCode.Ok);

        await BookCacheKeys.InvalidateCatalogAsync(cache, cancellationToken);
        await cache.RemoveAsync(BookCacheKeys.Availability(book.Id), cancellationToken);

        return Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}

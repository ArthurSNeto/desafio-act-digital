using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Application.Interfaces;
using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.UnitOfWork;
using Library.Infra.Redis;
using MediatR;
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
    ICacheService cache) : IRequestHandler<DeleteBookCommand, Result<BookResponse>>
{
    public async Task<Result<BookResponse>> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        Book? book = await bookRepository.RetrieveByIDAsync(request.Id, cancellationToken);
        if (book is null)
        {
            throw new BusinessRuleException($"Livro com ID '{request.Id}' não encontrado.");
        }

        if (await bookRepository.HasLoanHistoryAsync(request.Id, cancellationToken))
        {
            book.Deactivate();

            bookRepository.Update(book);
        }
        else
        {
            bookRepository.Delete(book);
        }

        AuditEvent audit = new(
            entityName: nameof(Book),
            entityId: book.Id.ToString(),
            action: "Delete",
            actor: userContext.CurrentUser,
            correlationId: userContext.CorrelationId,
            details: JsonSerializer.Serialize(new
            {
                book.IsActive,
            })
        );
        await auditRepository.CreateAsync(audit, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        await BookCacheKeys.InvalidateCatalogAsync(cache, cancellationToken);

        await cache.RemoveAsync(BookCacheKeys.Availability(book.Id), cancellationToken);

        return Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}
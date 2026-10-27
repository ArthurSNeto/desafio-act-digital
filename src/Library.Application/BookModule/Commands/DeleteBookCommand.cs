using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.UnitOfWork;
using MediatR;

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

public sealed class DeleteBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteBookCommand, Result<BookResponse>>
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

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}
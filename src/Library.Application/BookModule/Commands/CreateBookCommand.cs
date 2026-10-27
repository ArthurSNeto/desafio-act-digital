using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.UnitOfWork;
using MediatR;

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

public sealed class CreateBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateBookCommand, Result<BookResponse>>
{
    public async Task<Result<BookResponse>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        if (await bookRepository.ExistsByIsbnAsync(Guid.Empty, request.Isbn, cancellationToken))
        {
            throw new BusinessRuleException($"Já existe um livro cadastrado com o ISBN '{request.Isbn}'.");
        }

        Book book = new(request.Title, request.Author, request.Isbn, request.TotalCopies);
        await bookRepository.CreateAsync(book, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}
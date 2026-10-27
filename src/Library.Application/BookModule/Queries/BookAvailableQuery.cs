using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using MediatR;

namespace Library.Application.BookModule.Queries;

public sealed class BookAvailableQueryQuery : IRequest<Result<BookAvailableResponse>>
{
    public Guid Id { get; set; }

    public BookAvailableQueryQuery(Guid id)
    {
        Id = id;
    }
}

public sealed class BookAvailableQueryQueryValidator : AbstractValidator<BookAvailableQueryQuery>
{
    public BookAvailableQueryQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class BookAvailableQueryQueryHandler(IBookRepository bookRepository) : IRequestHandler<BookAvailableQueryQuery, Result<BookAvailableResponse>>
{
    public async Task<Result<BookAvailableResponse>> Handle(BookAvailableQueryQuery request, CancellationToken cancellationToken)
    {
        Book? book = await bookRepository.RetrieveByIDAsync(request.Id, cancellationToken);

        return book is null
            ? throw new BusinessRuleException($"Livro com ID '{request.Id}' não encontrado.")
            : Result.Success(new BookAvailableResponse(book.IsActive ? book.AvailableCopies > 0 : false));
    }
}

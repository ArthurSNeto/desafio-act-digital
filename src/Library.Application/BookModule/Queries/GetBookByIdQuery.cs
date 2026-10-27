using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using MediatR;

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

public sealed class GetBookByIdQueryHandler(IBookRepository bookRepository) : IRequestHandler<GetBookByIdQuery, Result<BookResponse>>
{
    public async Task<Result<BookResponse>> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        Book? book = await bookRepository.RetrieveByIDAsync(request.Id, cancellationToken);

        return book is null
            ? throw new BusinessRuleException($"Livro com ID '{request.Id}' não encontrado.")
            : Result.Success(new BookResponse(
            book.Id, book.Title, book.Author, book.Isbn,
            book.TotalCopies, book.AvailableCopies, book.IsActive,
            book.CreatedAtUtc, book.UpdatedAtUtc));
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.LoanModule.Models;
using Library.Domain.LoanModule;
using MediatR;

namespace Library.Application.UserModule.Queries;

public sealed class GetLoansByUserIdQuery : IRequest<Result<IReadOnlyList<DetailedLoanResponse>>>
{
    public Guid Id { get; set; }

    public GetLoansByUserIdQuery(Guid id)
    {
        Id = id;
    }
}

public sealed class GetLoansByUserIdQueryValidator : AbstractValidator<GetLoansByUserIdQuery>
{
    public GetLoansByUserIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class GetLoansByUserIdQueryHandler(ILoanRepository loanRepository) : IRequestHandler<GetLoansByUserIdQuery, Result<IReadOnlyList<DetailedLoanResponse>>>
{
    public async Task<Result<IReadOnlyList<DetailedLoanResponse>>> Handle(GetLoansByUserIdQuery request, CancellationToken cancellationToken)
    {
        List<Loan> loans = await loanRepository.ListByUserIdAsync(request.Id, cancellationToken);

        var response = loans.Select(l => new DetailedLoanResponse(
            Id: l.Id,
            UserId: l.UserId,
            BookId: l.BookId,
            BookTitle: l.Book.Title,
            BookAuthor: l.Book.Author,
            BookIsbn: l.Book.Isbn,
            Status: l.Status.ToString(),
            LoanDateUtc: l.LoanDateUtc,
            ExpectedReturnDateUtc: l.ExpectedReturnDateUtc,
            ReturnedAtUtc: l.ReturnedAtUtc
        )).ToList();

        return Result.Success<IReadOnlyList<DetailedLoanResponse>>(response);
    }
}

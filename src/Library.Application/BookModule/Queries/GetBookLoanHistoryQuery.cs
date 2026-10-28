using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Domain.LoanModule;
using MediatR;

namespace Library.Application.BookModule.Queries;

public class GetBookLoanHistoryQuery : IRequest<Result<IReadOnlyList<BookLoanHistoryResponse>>>
{
    public Guid Id { get; set; }

    public GetBookLoanHistoryQuery(Guid id)
    {
        Id = id;
    }
}

public sealed class GetBookLoanHistoryQueryValidator : AbstractValidator<GetBookLoanHistoryQuery>
{
    public GetBookLoanHistoryQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class GetBookLoanHistoryQueryHandler(ILoanRepository loanRepository) : IRequestHandler<GetBookLoanHistoryQuery, Result<IReadOnlyList<BookLoanHistoryResponse>>>
{
    public async Task<Result<IReadOnlyList<BookLoanHistoryResponse>>> Handle(GetBookLoanHistoryQuery request, CancellationToken cancellationToken)
    {
        List<Loan> loans = await loanRepository.ListByBookIdAsync(request.Id, cancellationToken);

        List<BookLoanHistoryResponse> response = loans.Select(l => new BookLoanHistoryResponse(
            LoanId: l.Id,
            UserId: l.UserId,
            Status: l.Status.ToString(),
            LoanDateUtc: l.LoanDateUtc,
            ExpectedReturnDateUtc: l.ExpectedReturnDateUtc,
            ReturnedAtUtc: l.ReturnedAtUtc,
            CancelledAtUtc: l.CancelledAtUtc,
            CancellationReason: l.CancellationReason
        )).ToList();

        return Result.Success<IReadOnlyList<BookLoanHistoryResponse>>(response);
    }
}

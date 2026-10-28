using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Application.Telemetry;
using Library.Domain.LoanModule;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

public sealed class GetBookLoanHistoryQueryHandler(
    ILoanRepository loanRepository,
    ILogger<GetBookLoanHistoryQueryHandler> logger) : IRequestHandler<GetBookLoanHistoryQuery, Result<IReadOnlyList<BookLoanHistoryResponse>>>
{
    public async Task<Result<IReadOnlyList<BookLoanHistoryResponse>>> Handle(GetBookLoanHistoryQuery request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("GetBookLoanHistory");
        activity?.SetTag("library.book.id", request.Id.ToString());

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

        activity?.SetTag("library.loan.history_count", response.Count);
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation(
            "Histórico de empréstimos consultado para o livro {BookId}. Total de registros: {Count}",
            request.Id,
            response.Count);

        return Result.Success<IReadOnlyList<BookLoanHistoryResponse>>(response);
    }
}

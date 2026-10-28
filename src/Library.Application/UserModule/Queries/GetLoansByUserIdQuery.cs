using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.LoanModule.Models;
using Library.Application.Telemetry;
using Library.Domain.LoanModule;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

public sealed class GetLoansByUserIdQueryHandler(
    ILoanRepository loanRepository,
    ILogger<GetLoansByUserIdQueryHandler> logger) : IRequestHandler<GetLoansByUserIdQuery, Result<IReadOnlyList<DetailedLoanResponse>>>
{
    public async Task<Result<IReadOnlyList<DetailedLoanResponse>>> Handle(GetLoansByUserIdQuery request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("GetLoansByUserId");
        activity?.SetTag("library.user.id", request.Id.ToString());

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

        activity?.SetTag("library.user.loans_count", response.Count);
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation(
            "Histórico de empréstimos consultado para o usuário {UserId}. Total de empréstimos: {Count}",
            request.Id,
            response.Count);

        return Result.Success<IReadOnlyList<DetailedLoanResponse>>(response);
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.Interfaces;
using Library.Application.LoanModule.Models;
using Library.Application.Telemetry;
using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.LoanModule;
using Library.Domain.UnitOfWork;
using Library.Infra.Redis;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Library.Application.LoanModule.Commands;

public class CancelLoanCommand : IRequest<Result<LoanResponse>>
{
    public Guid Id { get; }
    public string? Reason { get; set; } = string.Empty;

    public CancelLoanCommand(Guid id, string? reason)
    {
        Id = id;
        Reason = reason;
    }
}

public class CancelLoanCommandValidator : AbstractValidator<CancelLoanCommand>
{
    public CancelLoanCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class CancelLoanCommandHandler(
    ILoanRepository loanRepository,
    IBookRepository bookRepository,
    IAuditEventRepository auditRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext userContext,
    ICacheService cache,
    ILogger<CancelLoanCommandHandler> logger) : IRequestHandler<CancelLoanCommand, Result<LoanResponse>>
{
    public async Task<Result<LoanResponse>> Handle(CancelLoanCommand request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("CancelLoan");
        activity?.SetTag("library.loan_id", request.Id.ToString());
        activity?.SetTag("library.cancel_reason", request.Reason ?? string.Empty);

        Loan? loan = await loanRepository.RetrieveByIDAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Emprestimo", request.Id);

        Book? book = await bookRepository.RetrieveByIDAsync(loan.BookId, cancellationToken)
            ?? throw new EntityNotFoundException("Livro", loan.BookId);

        loan.Cancel(request.Reason);
        loanRepository.Update(loan);

        book.IncreaseAvailableCopies();
        bookRepository.Update(book);

        AuditEvent audit = new(
            entityName: nameof(Loan),
            entityId: loan.Id.ToString(),
            action: "Cancel",
            actor: userContext.CurrentUser,
            correlationId: userContext.CorrelationId,
            details: JsonSerializer.Serialize(new
            {
                loan.BookId,
                loan.UserId,
                loan.CancelledAtUtc,
                loan.CancellationReason,
                book.AvailableCopies
            })
        );
        await auditRepository.CreateAsync(audit, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        activity?.SetTag("library.book_id", loan.BookId.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation("Empréstimo {LoanId} cancelado com sucesso. Motivo: {Reason}", loan.Id, request.Reason);

        await BookCacheKeys.InvalidateCatalogAsync(cache, cancellationToken);

        await cache.RemoveAsync(BookCacheKeys.Availability(book.Id), cancellationToken);

        return Result.Success(new LoanResponse(
            loan.Id,
            loan.BookId,
            loan.UserId,
            loan.Status.ToString(),
            loan.LoanDateUtc,
            loan.ExpectedReturnDateUtc,
            loan.ReturnedAtUtc
        ));
    }
}

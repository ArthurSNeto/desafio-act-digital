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

public class ReturnLoanCommand : IRequest<Result<LoanResponse>>
{
    public Guid Id { get; }

    public ReturnLoanCommand(Guid id)
    {
        Id = id;
    }
}

public class ReturnLoanCommandValidator : AbstractValidator<ReturnLoanCommand>
{
    public ReturnLoanCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class ReturnLoanCommandHandler(
    ILoanRepository loanRepository,
    IBookRepository bookRepository,
    IAuditEventRepository auditRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext userContext,
    ICacheService cache,
    ILogger<ReturnLoanCommandHandler> logger) : IRequestHandler<ReturnLoanCommand, Result<LoanResponse>>
{
    public async Task<Result<LoanResponse>> Handle(ReturnLoanCommand request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("ReturnLoan");
        activity?.SetTag("library.loan_id", request.Id.ToString());

        Loan? loan = await loanRepository.RetrieveByIDAsync(request.Id, cancellationToken) 
            ?? throw new EntityNotFoundException("Emprestimo", request.Id);

        Book? book = await bookRepository.RetrieveByIDAsync(loan.BookId, cancellationToken) 
            ?? throw new EntityNotFoundException("Livro", loan.BookId);

        loan.Return();
        loanRepository.Update(loan);

        book.IncreaseAvailableCopies();
        bookRepository.Update(book);

        AuditEvent audit = new(
            entityName: nameof(Loan),
            entityId: loan.Id.ToString(),
            action: "Return",
            actor: userContext.CurrentUser,
            correlationId: userContext.CorrelationId,
            details: JsonSerializer.Serialize(new
            {
                loan.BookId,
                loan.UserId,
                loan.ReturnedAtUtc,
                book.AvailableCopies
            })
        );
        await auditRepository.CreateAsync(audit, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        activity?.SetTag("library.book_id", loan.BookId.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        logger.LogInformation("Empréstimo {LoanId} devolvido com sucesso para o livro {BookId}", loan.Id, loan.BookId);

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

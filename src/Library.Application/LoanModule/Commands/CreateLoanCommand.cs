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
using Library.Domain.UserModule;
using Library.Infra.Redis;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Library.Application.LoanModule.Commands;

public record LoanExecutionResult(LoanResponse Data, bool IsReplay);

public class CreateLoanCommand : IRequest<Result<LoanExecutionResult>>
{
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    public string IdempotencyKey { get; set; }
    public DateTime ExpectedReturnDateUtc { get; set; }

    public CreateLoanCommand(Guid bookId, Guid userId, string idempotencyKey, DateTime expectedReturnDateUtc)
    {
        BookId = bookId;
        UserId = userId;
        IdempotencyKey = idempotencyKey;
        ExpectedReturnDateUtc = expectedReturnDateUtc;
    }
}

public sealed class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("O identificador do livro é obrigatório.");
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("O identificador do usuário é obrigatório.");
        RuleFor(x => x.ExpectedReturnDateUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("A data prevista de devolução deve ser no futuro.");
    }
}

public sealed class CreateLoanCommandHandler(
    ILoanRepository loanRepository,
    IBookRepository bookRepository,
    IUserRepository userRepository,
    IAuditEventRepository auditRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext userContext,
    ICacheService cache,
    ILibraryMetrics metrics,
    ILogger<CreateLoanCommandHandler> logger) : IRequestHandler<CreateLoanCommand, Result<LoanExecutionResult>>
{
    public async Task<Result<LoanExecutionResult>> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
    {
        using Activity? activity = LibraryTelemetry.ActivitySource.StartActivity("CreateLoan");
        activity?.SetTag("library.book_id", request.BookId.ToString());
        activity?.SetTag("library.user_id", request.UserId.ToString());
        activity?.SetTag("library.idempotency_key", request.IdempotencyKey);

        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            Loan? existingLoan = await loanRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existingLoan is not null)
            {
                metrics.IncrementIdempotentHit();
                activity?.SetTag("library.is_replay", true);
                activity?.SetTag("library.loan_id", existingLoan.Id.ToString());
                activity?.SetStatus(ActivityStatusCode.Ok);

                logger.LogInformation(
                    "Requisição repetida atendida por idempotência para chave {IdempotencyKey}. Empréstimo: {LoanId}",
                    request.IdempotencyKey,
                    existingLoan.Id);

                return Result.Success(new LoanExecutionResult(MapToResponse(existingLoan), IsReplay: true));
            }

            Book book = await bookRepository.RetrieveByIDAsync(request.BookId, cancellationToken)
                    ?? throw new EntityNotFoundException("Livro", request.BookId);

            if (book.AvailableCopies <= 0 || !book.IsActive)
            {
                metrics.IncrementUnavailableRejection();
                activity?.SetTag("library.rejection_reason", "stock_unavailable");
                activity?.SetStatus(ActivityStatusCode.Error, "Não há exemplares disponíveis");

                logger.LogWarning(
                    "Tentativa de empréstimo rejeitada por indisponibilidade para o livro {BookId}. Exemplares disponíveis: {AvailableCopies}, Ativo: {IsActive}",
                    request.BookId,
                    book.AvailableCopies,
                    book.IsActive);

                return Result.Failure<LoanExecutionResult>("Não há exemplares disponíveis para realizar o empréstimo.");
            }

            book.DecreaseAvailableCopies();
            bookRepository.Update(book);

            User user = await userRepository.RetrieveByIDAsync(request.UserId, cancellationToken)
                ?? throw new EntityNotFoundException("Usuário", request.UserId);

            Loan loan = new(request.BookId, request.UserId, request.IdempotencyKey, request.ExpectedReturnDateUtc);
            await loanRepository.CreateAsync(loan, cancellationToken);

            AuditEvent audit = new(
                entityName: nameof(Loan),
                entityId: loan.Id.ToString(),
                action: "Created",
                actor: userContext.CurrentUser,
                correlationId: userContext.CorrelationId,
                details: JsonSerializer.Serialize(new
                {
                    loan.BookId,
                    loan.UserId,
                    loan.IdempotencyKey,
                    loan.ExpectedReturnDateUtc
                })
            );
            await auditRepository.CreateAsync(audit, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            metrics.IncrementLoansCreated();
            activity?.SetTag("library.loan_id", loan.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);

            logger.LogInformation(
                "Empréstimo {LoanId} criado com sucesso para o livro {BookId} e usuário {UserId}",
                loan.Id,
                request.BookId,
                request.UserId);

            await BookCacheKeys.InvalidateCatalogAsync(cache, cancellationToken);
            await cache.RemoveAsync(BookCacheKeys.Availability(book.Id), cancellationToken);

            return new LoanExecutionResult(MapToResponse(loan), IsReplay: false);
        }
        catch (ConcurrencyConflictException)
        {
            metrics.IncrementUnavailableRejection();
            activity?.SetTag("library.concurrency_conflict", true);
            activity?.SetTag("library.rejection_reason", "concurrency_conflict");
            activity?.SetStatus(ActivityStatusCode.Error, "Conflito de concorrência ao reservar exemplar");

            logger.LogWarning(
                "Conflito de concorrência ao tentar emprestar o livro {BookId} para o usuário {UserId}. Outra requisição simultânea finalizou primeiro.",
                request.BookId,
                request.UserId);

            throw;
        }
        finally
        {
            double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            metrics.RecordLoanCreationLatency(elapsedMs);
            activity?.SetTag("library.duration_ms", elapsedMs);
        }
    }

    private static LoanResponse MapToResponse(Loan loan)
    {
        return new(loan.Id, loan.BookId, loan.UserId, loan.Status.ToString(), loan.LoanDateUtc, loan.ExpectedReturnDateUtc, loan.ReturnedAtUtc);
    }
}

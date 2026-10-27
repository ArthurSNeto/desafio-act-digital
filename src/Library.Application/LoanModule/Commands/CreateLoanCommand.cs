using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.Interfaces;
using Library.Application.LoanModule.Models;
using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.LoanModule;
using Library.Domain.UnitOfWork;
using Library.Domain.UserModule;
using MediatR;
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
    ICurrentUserContext userContext) : IRequestHandler<CreateLoanCommand, Result<LoanExecutionResult>>
{
    public async Task<Result<LoanExecutionResult>> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
    {
        Loan? existingLoan = await loanRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingLoan is not null)
        {
            return Result.Success(new LoanExecutionResult(MapToResponse(existingLoan), IsReplay: true));
        }

        Book book = await bookRepository.RetrieveByIDAsync(request.BookId, cancellationToken)
                ?? throw new EntityNotFoundException("Livro", request.BookId);

        if (book.AvailableCopies <= 0 || !book.IsActive)
        {
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

        return new LoanExecutionResult(MapToResponse(loan), IsReplay: false);
    }

    private static LoanResponse MapToResponse(Loan loan)
    {
        return new(loan.Id, loan.BookId, loan.UserId, loan.Status.ToString(), loan.LoanDateUtc, loan.ExpectedReturnDateUtc);
    }
}

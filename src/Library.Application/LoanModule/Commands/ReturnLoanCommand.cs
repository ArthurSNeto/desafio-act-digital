using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.LoanModule.Models;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.LoanModule;
using Library.Domain.UnitOfWork;
using MediatR;

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
    IUnitOfWork unitOfWork) : IRequestHandler<ReturnLoanCommand, Result<LoanResponse>>
{
    public async Task<Result<LoanResponse>> Handle(ReturnLoanCommand request, CancellationToken cancellationToken)
    {
        Loan? loan = await loanRepository.RetrieveByIDAsync(request.Id, cancellationToken) 
            ?? throw new EntityNotFoundException("Emprestimo", request.Id);

        Book? book = await bookRepository.RetrieveByIDAsync(loan.BookId, cancellationToken) 
            ?? throw new EntityNotFoundException("Livro", loan.BookId);

        loan.Return();
        loanRepository.Update(loan);

        book.IncreaseAvailableCopies();
        bookRepository.Update(book);

        await unitOfWork.CommitAsync(cancellationToken);

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

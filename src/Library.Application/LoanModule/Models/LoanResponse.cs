namespace Library.Application.LoanModule.Models;

public record LoanResponse(
    Guid Id,
    Guid BookId,
    Guid UserId,
    string Status,
    DateTime LoanDateUtc,
    DateTime ExpectedReturnDateUtc
);

public record DetailedLoanResponse(
    Guid Id,
    Guid BookId,
    Guid UserId,
    string BookTitle,
    string BookAuthor,
    string BookIsbn,
    string Status,
    DateTime LoanDateUtc,
    DateTime ExpectedReturnDateUtc,
    DateTime? ReturnedAtUtc
);
namespace Library.Application.LoanModule.Models;

public record LoanResponse(
    Guid Id,
    Guid BookId,
    Guid UserId,
    string Status,
    DateTime LoanDateUtc,
    DateTime ExpectedReturnDateUtc
);
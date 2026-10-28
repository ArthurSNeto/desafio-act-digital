namespace Library.Application.BookModule.Models;

public record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    int TotalCopies,
    int AvailableCopies,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record BookAvailableResponse(
    Guid Id,
    string Title,
    int AvailableCopies,
    bool IsAvailable);

public record BookLoanHistoryResponse(
    Guid LoanId,
    Guid UserId,
    string Status,
    DateTime LoanDateUtc,
    DateTime ExpectedReturnDateUtc,
    DateTime? ReturnedAtUtc,
    DateTime? CancelledAtUtc,
    string? CancellationReason);

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

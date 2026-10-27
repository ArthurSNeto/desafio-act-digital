using Library.Domain.Exceptions;

namespace Library.Domain.BookModule;

public sealed class Book : Entity
{
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string Isbn { get; private set; } = string.Empty;
    public int TotalCopies { get; private set; }
    public int AvailableCopies { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid ConcurrencyToken { get; private set; } = Guid.NewGuid();

    private Book() { }

    public Book(string title, string author, string isbn, int totalCopies)
    {
        ValidateData(title, author, isbn, totalCopies);

        Id = Guid.NewGuid();
        Title = title;
        Author = author;
        Isbn = NormalizeIsbn(isbn);
        TotalCopies = totalCopies;
        AvailableCopies = totalCopies;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string? title, string? author, string? isbn, int totalCopies)
    {
        if (totalCopies < 0)
            throw new BusinessRuleException("A quantidade total de exemplares não pode ser negativa.");

        var borrowedCopies = TotalCopies - AvailableCopies;
        if (totalCopies < borrowedCopies)
        {
            throw new BusinessRuleException(
                $"Não é possível reduzir o total para {totalCopies}, pois existem {borrowedCopies} exemplares emprestados no momento.");
        }

        Title = title ?? Title;
        Author = author ?? Author;
        Isbn = isbn is not null ? NormalizeIsbn(isbn) : Isbn;

        AvailableCopies = totalCopies - borrowedCopies;
        TotalCopies = totalCopies;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void DecreaseAvailableCopies()
    {
        if (!IsActive)
            throw new BusinessRuleException("Não é possível emprestar um livro desativado.");

        if (AvailableCopies <= 0)
            throw new BusinessRuleException("Não há exemplares disponíveis para este livro.");

        AvailableCopies--;
        UpdatedAtUtc = DateTime.UtcNow;

        ConcurrencyToken = Guid.NewGuid();
    }

    public void IncreaseAvailableCopies()
    {
        if (AvailableCopies >= TotalCopies)
            throw new BusinessRuleException("A quantidade disponível não pode exceder o total de exemplares.");

        AvailableCopies++;
        UpdatedAtUtc = DateTime.UtcNow;

        ConcurrencyToken = Guid.NewGuid();
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleException("O livro já está desativado.");

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateData(string title, string author, string isbn, int totalCopies)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleException("O título é obrigatório.");
        if (string.IsNullOrWhiteSpace(author))
            throw new BusinessRuleException("O autor é obrigatório.");
        if (string.IsNullOrWhiteSpace(isbn))
            throw new BusinessRuleException("O ISBN é obrigatório.");
        if (totalCopies < 1)
            throw new BusinessRuleException("O livro deve ser cadastrado com pelo menos 1 exemplar.");
    }

    private static string NormalizeIsbn(string isbn) =>
        isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
}
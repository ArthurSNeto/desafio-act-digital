using Library.Domain.Exceptions;

namespace Library.Domain.LoanModule;

public sealed class Loan : Entity
{
    public Guid BookId { get; private set; }
    public Guid UserId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime LoanDateUtc { get; private set; }
    public DateTime ExpectedReturnDateUtc { get; private set; }
    public DateTime? ReturnedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public LoanStatus Status { get; private set; }

    private Loan() { }

    public Loan(
        Guid bookId,
        Guid userId,
        string idempotencyKey,
        DateTime expectedReturnDateUtc)
    {
        if (bookId == Guid.Empty)
            throw new BusinessRuleException("O identificador do livro é obrigatório.");
        if (userId == Guid.Empty)
            throw new BusinessRuleException("O identificador do usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new BusinessRuleException("A chave de idempotência é obrigatória.");
        if (expectedReturnDateUtc <= DateTime.UtcNow)
            throw new BusinessRuleException("A data prevista de devolução deve ser no futuro.");

        Id = Guid.NewGuid();
        BookId = bookId;
        UserId = userId;
        IdempotencyKey = idempotencyKey;
        LoanDateUtc = DateTime.UtcNow;
        ExpectedReturnDateUtc = expectedReturnDateUtc;
        Status = LoanStatus.Active;
    }

    public void Return()
    {
        if (Status == LoanStatus.Returned)
            throw new BusinessRuleException("Este empréstimo já foi devolvido anteriormente.");

        if (Status == LoanStatus.Cancelled)
            throw new BusinessRuleException("Não é possível devolver um empréstimo cancelado.");

        Status = LoanStatus.Returned;
        ReturnedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == LoanStatus.Cancelled)
            throw new BusinessRuleException("Este empréstimo já se encontra cancelado.");

        if (Status == LoanStatus.Returned)
            throw new BusinessRuleException("Não é possível cancelar um empréstimo que já foi devolvido.");

        Status = LoanStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancellationReason = reason ?? "Cancelado pelo usuário ou administração.";
    }
}

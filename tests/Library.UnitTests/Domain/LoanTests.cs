using FluentAssertions;
using Library.Domain.Exceptions;
using Library.Domain.LoanModule;

namespace Library.UnitTests.Domain;

public class LoanTests
{
    [Fact]
    public void Should_Create_Loan_Successfully_When_Valid_Data()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var key = "idemp-unit-123";
        var expectedReturn = DateTime.UtcNow.AddDays(7);

        // Act
        var loan = new Loan(bookId, userId, key, expectedReturn);

        // Assert
        loan.Id.Should().NotBeEmpty();
        loan.BookId.Should().Be(bookId);
        loan.UserId.Should().Be(userId);
        loan.IdempotencyKey.Should().Be(key);
        loan.ExpectedReturnDateUtc.Should().Be(expectedReturn);
        loan.Status.Should().Be(LoanStatus.Active);
        loan.LoanDateUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        loan.ReturnedAtUtc.Should().BeNull();
        loan.CancelledAtUtc.Should().BeNull();
        loan.CancellationReason.Should().BeNull();
    }

    [Fact]
    public void Should_Throw_When_BookId_Is_Empty()
    {
        // Act
        Action act = () => new Loan(Guid.Empty, Guid.NewGuid(), "key", DateTime.UtcNow.AddDays(1));

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("O identificador do livro é obrigatório.");
    }

    [Fact]
    public void Should_Throw_When_UserId_Is_Empty()
    {
        // Act
        Action act = () => new Loan(Guid.NewGuid(), Guid.Empty, "key", DateTime.UtcNow.AddDays(1));

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("O identificador do usuário é obrigatório.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Throw_When_IdempotencyKey_Is_Blank(string invalidKey)
    {
        // Act
        Action act = () => new Loan(Guid.NewGuid(), Guid.NewGuid(), invalidKey, DateTime.UtcNow.AddDays(1));

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("A chave de idempotência é obrigatória.");
    }

    [Fact]
    public void Should_Throw_When_ExpectedReturnDate_Is_In_The_Past()
    {
        // Act
        Action act = () => new Loan(Guid.NewGuid(), Guid.NewGuid(), "key", DateTime.UtcNow.AddMinutes(-5));

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("A data prevista de devolução deve ser no futuro.");
    }

    [Fact]
    public void Should_Return_Loan_Successfully()
    {
        // Arrange
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), "key", DateTime.UtcNow.AddDays(5));

        // Act
        loan.Return();

        // Assert
        loan.Status.Should().Be(LoanStatus.Returned);
        loan.ReturnedAtUtc.Should().NotBeNull();
        loan.ReturnedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Should_Throw_When_Returning_Already_Returned_Loan()
    {
        // Arrange
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), "key", DateTime.UtcNow.AddDays(5));
        loan.Return();

        // Act
        Action act = () => loan.Return();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Este empréstimo já foi devolvido anteriormente.");
    }

    [Fact]
    public void Should_Throw_When_Returning_Cancelled_Loan()
    {
        // Arrange
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), "key", DateTime.UtcNow.AddDays(5));
        loan.Cancel("Cancelado");

        // Act
        Action act = () => loan.Return();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Não é possível devolver um empréstimo cancelado.");
    }

    [Fact]
    public void Should_Cancel_Loan_Successfully_With_Reason()
    {
        // Arrange
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), "key", DateTime.UtcNow.AddDays(5));

        // Act
        loan.Cancel("Usuário desistiu do empréstimo.");

        // Assert
        loan.Status.Should().Be(LoanStatus.Cancelled);
        loan.CancelledAtUtc.Should().NotBeNull();
        loan.CancellationReason.Should().Be("Usuário desistiu do empréstimo.");
    }

    [Fact]
    public void Should_Throw_When_Cancelling_Already_Cancelled_Loan()
    {
        // Arrange
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), "key", DateTime.UtcNow.AddDays(5));
        loan.Cancel();

        // Act
        Action act = () => loan.Cancel();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Este empréstimo já se encontra cancelado.");
    }

    [Fact]
    public void Should_Throw_When_Cancelling_Already_Returned_Loan()
    {
        // Arrange
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), "key", DateTime.UtcNow.AddDays(5));
        loan.Return();

        // Act
        Action act = () => loan.Cancel();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Não é possível cancelar um empréstimo que já foi devolvido.");
    }
}

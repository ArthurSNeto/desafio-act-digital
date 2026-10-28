using FluentAssertions;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain;

public class BookTests
{
    [Fact]
    public void Should_Create_Book_Successfully_When_Valid_Data()
    {
        // Arrange & Act
        var book = new Book("Clean Code", "Robert C. Martin", "978-0132350884", 5);

        // Assert
        book.Id.Should().NotBeEmpty();
        book.Title.Should().Be("Clean Code");
        book.Author.Should().Be("Robert C. Martin");
        book.Isbn.Should().Be("9780132350884"); // Normalizado sem traços
        book.TotalCopies.Should().Be(5);
        book.AvailableCopies.Should().Be(5);
        book.IsActive.Should().BeTrue();
        book.ConcurrencyToken.Should().NotBeEmpty();
        book.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        book.UpdatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Robert Martin", "9780132350884", 3, "O título é obrigatório.")]
    [InlineData("   ", "Robert Martin", "9780132350884", 3, "O título é obrigatório.")]
    [InlineData("Clean Code", "", "9780132350884", 3, "O autor é obrigatório.")]
    [InlineData("Clean Code", "   ", "9780132350884", 3, "O autor é obrigatório.")]
    [InlineData("Clean Code", "Robert Martin", "", 3, "O ISBN é obrigatório.")]
    [InlineData("Clean Code", "Robert Martin", "   ", 3, "O ISBN é obrigatório.")]
    [InlineData("Clean Code", "Robert Martin", "9780132350884", 0, "O livro deve ser cadastrado com pelo menos 1 exemplar.")]
    [InlineData("Clean Code", "Robert Martin", "9780132350884", -2, "O livro deve ser cadastrado com pelo menos 1 exemplar.")]
    public void Should_Throw_Exception_When_Required_Data_Is_Invalid(
        string title, string author, string isbn, int totalCopies, string expectedMessage)
    {
        // Act
        Action act = () => new Book(title, author, isbn, totalCopies);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void Should_Decrease_Available_Copies_Successfully_And_Update_ConcurrencyToken()
    {
        // Arrange
        var book = new Book("Refactoring", "Martin Fowler", "978-0201485677", 2);
        var initialToken = book.ConcurrencyToken;

        // Act
        book.DecreaseAvailableCopies();

        // Assert
        book.AvailableCopies.Should().Be(1);
        book.ConcurrencyToken.Should().NotBe(initialToken);
        book.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Should_Throw_When_Decreasing_Copies_And_Available_Is_Zero()
    {
        // Arrange
        var book = new Book("Domain-Driven Design", "Eric Evans", "978-0321125217", 1);
        book.DecreaseAvailableCopies(); // restam 0

        // Act
        Action act = () => book.DecreaseAvailableCopies();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Não há exemplares disponíveis para este livro.");
    }

    [Fact]
    public void Should_Throw_When_Decreasing_Copies_And_Book_Is_Inactive()
    {
        // Arrange
        var book = new Book("Domain-Driven Design", "Eric Evans", "978-0321125217", 2);
        book.Deactivate();

        // Act
        Action act = () => book.DecreaseAvailableCopies();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Não é possível emprestar um livro desativado.");
    }

    [Fact]
    public void Should_Increase_Available_Copies_Successfully()
    {
        // Arrange
        var book = new Book("Clean Architecture", "Robert Martin", "978-0134494166", 2);
        book.DecreaseAvailableCopies(); // Available: 1
        var intermediateToken = book.ConcurrencyToken;

        // Act
        book.IncreaseAvailableCopies();

        // Assert
        book.AvailableCopies.Should().Be(2);
        book.ConcurrencyToken.Should().NotBe(intermediateToken);
    }

    [Fact]
    public void Should_Throw_When_Increasing_Copies_Beyond_TotalCopies()
    {
        // Arrange
        var book = new Book("Clean Architecture", "Robert Martin", "978-0134494166", 2);

        // Act
        Action act = () => book.IncreaseAvailableCopies();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("A quantidade disponível não pode exceder o total de exemplares.");
    }

    [Fact]
    public void Should_Update_Details_And_Recalculate_AvailableCopies()
    {
        // Arrange
        var book = new Book("Building Microservices", "Sam Newman", "978-1492034025", 5);
        book.DecreaseAvailableCopies(); // 1 emprestado (Available = 4)

        // Act
        book.UpdateDetails("Building Microservices 2nd Ed", "Sam Newman", "978-1492034025", 6);

        // Assert
        book.Title.Should().Be("Building Microservices 2nd Ed");
        book.TotalCopies.Should().Be(6);
        book.AvailableCopies.Should().Be(5); // 6 - 1 emprestado = 5
    }

    [Fact]
    public void Should_Throw_When_Updating_TotalCopies_Below_Currently_Borrowed()
    {
        // Arrange
        var book = new Book("Building Microservices", "Sam Newman", "978-1492034025", 4);
        book.DecreaseAvailableCopies();
        book.DecreaseAvailableCopies();
        book.DecreaseAvailableCopies(); // 3 emprestados, 1 disponível

        // Act - tenta reduzir total para 2 (sendo que 3 estão emprestados)
        Action act = () => book.UpdateDetails(null, null, null, 2);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Não é possível reduzir o total para 2, pois existem 3 exemplares emprestados no momento.*");
    }

    [Fact]
    public void Should_Deactivate_Book_Successfully()
    {
        // Arrange
        var book = new Book("Test Driven Development", "Kent Beck", "978-0321146533", 3);

        // Act
        book.Deactivate();

        // Assert
        book.IsActive.Should().BeFalse();
        book.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Should_Throw_When_Deactivating_Already_Inactive_Book()
    {
        // Arrange
        var book = new Book("Test Driven Development", "Kent Beck", "978-0321146533", 3);
        book.Deactivate();

        // Act
        Action act = () => book.Deactivate();

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("O livro já está desativado.");
    }
}

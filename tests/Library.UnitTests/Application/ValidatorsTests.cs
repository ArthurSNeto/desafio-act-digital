using FluentAssertions;
using Library.Application.BookModule.Commands;
using Library.Application.LoanModule.Commands;

namespace Library.UnitTests.Application;

public class ValidatorsTests
{
    [Fact]
    public void CreateLoanCommandValidator_Should_Pass_When_Valid()
    {
        // Arrange
        var validator = new CreateLoanCommandValidator();
        var command = new CreateLoanCommand(Guid.NewGuid(), Guid.NewGuid(), "key-123", DateTime.UtcNow.AddDays(3));

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateLoanCommandValidator_Should_Fail_When_Missing_Fields()
    {
        // Arrange
        var validator = new CreateLoanCommandValidator();
        var command = new CreateLoanCommand(Guid.Empty, Guid.Empty, "key-123", DateTime.UtcNow.AddDays(-1));

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoanCommand.BookId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoanCommand.UserId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoanCommand.ExpectedReturnDateUtc));
    }

    [Fact]
    public void CreateBookCommandValidator_Should_Pass_When_Valid()
    {
        // Arrange
        var validator = new CreateBookCommandValidator();
        var command = new CreateBookCommand("Pragmatic Programmer", "Andrew Hunt", "978-0201616224", 3);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateBookCommandValidator_Should_Fail_When_Invalid()
    {
        // Arrange
        var validator = new CreateBookCommandValidator();
        var command = new CreateBookCommand("", "", "", 0);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(4);
    }
}

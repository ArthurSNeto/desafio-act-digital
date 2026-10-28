using FluentAssertions;
using Library.Application.Interfaces;
using Library.Application.LoanModule.Commands;
using Library.Application.Telemetry;
using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Domain.LoanModule;
using Library.Domain.UnitOfWork;
using Library.Domain.UserModule;
using Library.Infra.Redis;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Library.UnitTests.Application;

public class CreateLoanCommandHandlerTests
{
    private readonly ILoanRepository _loanRepository = Substitute.For<ILoanRepository>();
    private readonly IBookRepository _bookRepository = Substitute.For<IBookRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAuditEventRepository _auditRepository = Substitute.For<IAuditEventRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserContext _userContext = Substitute.For<ICurrentUserContext>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly ILibraryMetrics _metrics = Substitute.For<ILibraryMetrics>();
    private readonly ILogger<CreateLoanCommandHandler> _logger = Substitute.For<ILogger<CreateLoanCommandHandler>>();

    private readonly CreateLoanCommandHandler _handler;

    public CreateLoanCommandHandlerTests()
    {
        _userContext.CurrentUser.Returns("test-user");
        _userContext.CorrelationId.Returns("corr-123");

        _handler = new CreateLoanCommandHandler(
            _loanRepository,
            _bookRepository,
            _userRepository,
            _auditRepository,
            _unitOfWork,
            _userContext,
            _cache,
            _metrics,
            _logger);
    }

    [Fact]
    public async Task Handle_Should_Return_Existing_Loan_When_IdempotencyKey_Already_Exists()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var idempotencyKey = "existing-key-123";
        var existingLoan = new Loan(bookId, userId, idempotencyKey, DateTime.UtcNow.AddDays(7));

        _loanRepository.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(existingLoan);

        var command = new CreateLoanCommand(bookId, userId, idempotencyKey, DateTime.UtcNow.AddDays(7));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsReplay.Should().BeTrue();
        result.Value.Data.Id.Should().Be(existingLoan.Id);
        _metrics.Received(1).IncrementIdempotentHit();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_EntityNotFoundException_When_Book_Does_Not_Exist()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var idempotencyKey = "key-404-book";

        _loanRepository.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Loan?)null);
        _bookRepository.RetrieveByIDAsync(bookId, Arg.Any<CancellationToken>())
            .Returns((Book?)null);

        var command = new CreateLoanCommand(bookId, userId, idempotencyKey, DateTime.UtcNow.AddDays(7));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_No_Available_Copies()
    {
        // Arrange
        var book = new Book("TDD by Example", "Kent Beck", "978-0321146533", 1);
        book.DecreaseAvailableCopies(); // restam 0 exemplares

        var userId = Guid.NewGuid();
        var idempotencyKey = "key-no-copies";

        _loanRepository.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Loan?)null);
        _bookRepository.RetrieveByIDAsync(book.Id, Arg.Any<CancellationToken>())
            .Returns(book);

        var command = new CreateLoanCommand(book.Id, userId, idempotencyKey, DateTime.UtcNow.AddDays(7));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Não há exemplares disponíveis");
        _metrics.Received(1).IncrementUnavailableRejection();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Create_Loan_Successfully_When_Valid()
    {
        // Arrange
        var book = new Book("Design Patterns", "GoF", "978-0201633610", 3);
        var user = new User();
        var idempotencyKey = "new-key-success";
        var expectedReturn = DateTime.UtcNow.AddDays(7);

        _loanRepository.GetByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns((Loan?)null);
        _bookRepository.RetrieveByIDAsync(book.Id, Arg.Any<CancellationToken>())
            .Returns(book);
        _userRepository.RetrieveByIDAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new CreateLoanCommand(book.Id, user.Id, idempotencyKey, expectedReturn);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsReplay.Should().BeFalse();
        book.AvailableCopies.Should().Be(2);

        await _loanRepository.Received(1).CreateAsync(Arg.Any<Loan>(), Arg.Any<CancellationToken>());
        await _auditRepository.Received(1).CreateAsync(Arg.Is<AuditEvent>(a => a.Action == "Created" && a.EntityName == "Loan"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).IncrementLoansCreated();
    }
}

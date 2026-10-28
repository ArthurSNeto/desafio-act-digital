using FluentAssertions;
using Library.Application.BookModule.Commands;
using Library.Application.BookModule.Models;
using Library.Application.UserModule.Commands;
using Library.IntegrationTests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Library.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class BooksControllerTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Should_Create_And_Get_Book_Successfully()
    {
        // Arrange
        var isbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var createCommand = new CreateBookCommand("Domain-Driven Design", "Eric Evans", isbn, 4);

        // Act - Create
        var postResponse = await _client.PostAsJsonAsync("/books", createCommand);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBook = await postResponse.Content.ReadFromJsonAsync<BookResponse>();

        createdBook.Should().NotBeNull();
        createdBook!.Title.Should().Be("Domain-Driven Design");
        createdBook.TotalCopies.Should().Be(4);
        createdBook.AvailableCopies.Should().Be(4);

        // Act - GetById
        var getResponse = await _client.GetAsync($"/books/{createdBook.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedBook = await getResponse.Content.ReadFromJsonAsync<BookResponse>();

        retrievedBook.Should().NotBeNull();
        retrievedBook!.Id.Should().Be(createdBook.Id);
    }

    [Fact]
    public async Task Should_Reject_Creation_When_Isbn_Already_Exists()
    {
        // Arrange
        var sharedIsbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var book1 = new CreateBookCommand("Clean Code 1st Edition", "Robert Martin", sharedIsbn, 2);
        var book2 = new CreateBookCommand("Clean Code 2nd Edition", "Robert Martin", sharedIsbn, 3);

        // Act
        var firstResponse = await _client.PostAsJsonAsync("/books", book1);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync("/books", book2);

        // Assert - Duplicated ISBN must be rejected
        secondResponse.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Deactivate_Instead_Of_Physical_Delete_When_Book_Has_Loan_History()
    {
        // 1. Cria livro
        var isbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var createBookResponse = await _client.PostAsJsonAsync("/books", new CreateBookCommand("Livro com Histórico", "Autor", isbn, 2));
        createBookResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = (await createBookResponse.Content.ReadFromJsonAsync<BookResponse>())!;

        // 2. Cria usuário
        var createUserResponse = await _client.PostAsJsonAsync("/users", new CreateUserCommand());
        createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = (await createUserResponse.Content.ReadFromJsonAsync<UserDto>())!;

        // 3. Cria empréstimo
        var request = new HttpRequestMessage(HttpMethod.Post, "/loans")
        {
            Content = JsonContent.Create(new { BookId = book.Id, UserId = user.Id, ExpectedReturnDateUtc = DateTime.UtcNow.AddDays(5) })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var loanResponse = await _client.SendAsync(request);
        loanResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Tenta deletar o livro
        var deleteResponse = await _client.DeleteAsync($"/books/{book.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedResult = await deleteResponse.Content.ReadFromJsonAsync<BookResponse>();

        // Assert: Livro não foi apagado silenciosamente; foi desativado (soft delete) mantendo integridade
        deletedResult!.IsActive.Should().BeFalse();

        // 5. Verifica se ainda existe na consulta por ID (desativado)
        var getAgain = await _client.GetAsync($"/books/{book.Id}");
        getAgain.StatusCode.Should().Be(HttpStatusCode.OK);
        var persistedBook = await getAgain.Content.ReadFromJsonAsync<BookResponse>();
        persistedBook!.IsActive.Should().BeFalse();
    }

    private record UserDto(Guid Id);
}

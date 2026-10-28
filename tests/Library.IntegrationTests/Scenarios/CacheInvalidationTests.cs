using FluentAssertions;
using Library.Application.BookModule.Commands;
using Library.Application.BookModule.Models;
using Library.Application.UserModule.Commands;
using Library.IntegrationTests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Library.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class CacheInvalidationTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Cache_Should_Be_Invalidated_When_Loan_Is_Created()
    {
        // 1. Cria livro com 3 exemplares
        var isbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var createBookResponse = await _client.PostAsJsonAsync("/books",
            new CreateBookCommand("Cache Test Book", "Autor Cache", isbn, 3));
        var book = (await createBookResponse.Content.ReadFromJsonAsync<BookResponse>())!;

        // 2. Consulta disponibilidade -> Popula o cache no Redis com 3 exemplares
        var initialAvailability = await _client.GetFromJsonAsync<BookAvailableResponse>($"/books/{book.Id}/availability");
        initialAvailability!.AvailableCopies.Should().Be(3);
        initialAvailability.IsAvailable.Should().BeTrue();

        // 3. Cria um usuário e realiza um empréstimo
        var createUserResponse = await _client.PostAsJsonAsync("/users", new CreateUserCommand());
        var user = (await createUserResponse.Content.ReadFromJsonAsync<UserDto>())!;

        using var loanRequest = new HttpRequestMessage(HttpMethod.Post, "/loans")
        {
            Content = JsonContent.Create(new
            {
                BookId = book.Id,
                UserId = user.Id,
                ExpectedReturnDateUtc = DateTime.UtcNow.AddDays(7)
            })
        };
        loanRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var loanResponse = await _client.SendAsync(loanRequest);
        loanResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Consulta a disponibilidade novamente
        // O cache DEVE ter sido invalidado pelo handler do empréstimo, refletindo 2 exemplares
        var updatedAvailability = await _client.GetFromJsonAsync<BookAvailableResponse>($"/books/{book.Id}/availability");
        updatedAvailability!.AvailableCopies.Should().Be(2);
        updatedAvailability.IsAvailable.Should().BeTrue();
    }

    private record UserDto(Guid Id);
}

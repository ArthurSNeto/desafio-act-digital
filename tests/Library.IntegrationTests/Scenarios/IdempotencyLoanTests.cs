using FluentAssertions;
using Library.Application.BookModule.Commands;
using Library.Application.BookModule.Models;
using Library.Application.LoanModule.Models;
using Library.Application.UserModule.Commands;
using Library.Domain.LoanModule;
using Library.Infra.EF.Context;
using Library.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Library.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class IdempotencyLoanTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Idempotency_Should_Return_Cached_Response_Without_Double_Decreasing_Copies()
    {
        // 1. Arrange: Cria livro com 5 cópias e um usuário
        var isbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var createBookResponse = await _client.PostAsJsonAsync("/books",
            new CreateBookCommand("Idempotência em Prática", "Autor Idemp", isbn, 5));
        createBookResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = (await createBookResponse.Content.ReadFromJsonAsync<BookResponse>())!;

        var createUserResponse = await _client.PostAsJsonAsync("/users", new CreateUserCommand());
        var user = (await createUserResponse.Content.ReadFromJsonAsync<UserDto>())!;

        var idempotencyKey = $"idemp-key-{Guid.NewGuid()}";
        var loanPayload = new
        {
            BookId = book.Id,
            UserId = user.Id,
            ExpectedReturnDateUtc = DateTime.UtcNow.AddDays(7)
        };

        // 2. Act - Primeira chamada: criação inicial
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/loans")
        {
            Content = JsonContent.Create(loanPayload)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        var firstResponse = await _client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstLoanResult = (await firstResponse.Content.ReadFromJsonAsync<LoanResponse>())!;

        // 3. Act - Segunda chamada: repetição idêntica com a MESMA chave de idempotência
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/loans")
        {
            Content = JsonContent.Create(loanPayload)
        };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        var secondResponse = await _client.SendAsync(secondRequest);
        
        // Assert: A segunda resposta é um replay (200 OK)
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondLoanResult = (await secondResponse.Content.ReadFromJsonAsync<LoanResponse>())!;

        // 4. Assert: O ID retornado nas duas chamadas deve ser idêntico
        secondLoanResult.Id.Should().Be(firstLoanResult.Id);
        secondLoanResult.BookId.Should().Be(book.Id);
        secondLoanResult.UserId.Should().Be(user.Id);

        // 5. Assert: O livro teve apenas 1 exemplar debitado (5 -> 4), NUNCA 2 vezes
        var bookAfterLoans = await _client.GetFromJsonAsync<BookResponse>($"/books/{book.Id}");
        bookAfterLoans!.AvailableCopies.Should().Be(4);

        // 6. Assert: No banco de dados, existe estritamente 1 linha com essa IdempotencyKey
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var matchingLoans = await db.Set<Loan>().Where(l => l.IdempotencyKey == idempotencyKey).ToListAsync();
        matchingLoans.Should().HaveCount(1);
    }

    private record UserDto(Guid Id);
}

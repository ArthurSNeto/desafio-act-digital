using FluentAssertions;
using Library.Application.BookModule.Commands;
using Library.Application.BookModule.Models;
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
public class ConcurrencyLoanTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Concurrency_Should_Allow_Only_One_Loan_For_Last_Available_Copy()
    {
        // 1. Arrange: Cadastra um livro com APENAS 1 exemplar disponível
        var uniqueIsbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var createBookResponse = await _client.PostAsJsonAsync("/books",
            new CreateBookCommand("Concorrência Extrema", "Autor Teste", uniqueIsbn, 1));
        createBookResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = (await createBookResponse.Content.ReadFromJsonAsync<BookResponse>())!;

        // 2. Cria 10 usuários para concorrerem pelo mesmo exemplar
        const int concurrentUsersCount = 10;
        var users = new List<UserDto>();
        for (int i = 0; i < concurrentUsersCount; i++)
        {
            var userRes = await _client.PostAsJsonAsync("/users", new CreateUserCommand());
            var user = await userRes.Content.ReadFromJsonAsync<UserDto>();
            users.Add(user!);
        }

        // 3. Act: Prepara 10 requisições HTTP paralelas disparadas ao mesmo tempo
        var loanTasks = users.Select(async user =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/loans")
            {
                Content = JsonContent.Create(new
                {
                    BookId = book.Id,
                    UserId = user.Id,
                    ExpectedReturnDateUtc = DateTime.UtcNow.AddDays(7)
                })
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

            return await _client.SendAsync(request);
        }).ToList();

        HttpResponseMessage[] responses = await Task.WhenAll(loanTasks);

        // 4. Assert: Exatamente 1 deve ser 201 Created
        var successfulResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
        var rejectedResponses = responses.Where(r =>
            r.StatusCode == HttpStatusCode.BadRequest || r.StatusCode == HttpStatusCode.Conflict).ToList();

        successfulResponses.Should().HaveCount(1,
            because: "apenas um empréstimo pode ser concedido quando há apenas um exemplar disponível");

        rejectedResponses.Should().HaveCount(concurrentUsersCount - 1,
            because: "todas as outras tentativas simultâneas devem ser rejeitadas por falta de estoque ou conflito de concorrência");

        // 5. Assert: O livro deve ter exatamente 0 cópias disponíveis (nunca negativo!)
        var bookAfterCheck = await _client.GetFromJsonAsync<BookResponse>($"/books/{book.Id}");
        bookAfterCheck!.AvailableCopies.Should().Be(0);

        // 6. Assert: No banco de dados, deve existir rigorosamente 1 registro de empréstimo para esse livro
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var totalLoansInDb = await db.Set<Loan>().CountAsync(l => l.BookId == book.Id);
        totalLoansInDb.Should().Be(1);
    }

    private record UserDto(Guid Id);
}

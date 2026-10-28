using FluentAssertions;
using Library.Application.BookModule.Commands;
using Library.Application.BookModule.Models;
using Library.Application.LoanModule.Models;
using Library.Application.UserModule.Commands;
using Library.Domain.AuditModule;
using Library.IntegrationTests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Library.IntegrationTests.Scenarios;

[Collection("IntegrationTests")]
public class LoanHistoryAndAuditTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Should_Preserve_History_And_Audit_Events_After_Loan_Return()
    {
        // 1. Arrange: Cria livro e usuário
        var isbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var createBookResponse = await _client.PostAsJsonAsync("/books",
            new CreateBookCommand("Arquitetura Limpa", "Uncle Bob", isbn, 2));
        var book = (await createBookResponse.Content.ReadFromJsonAsync<BookResponse>())!;

        var createUserResponse = await _client.PostAsJsonAsync("/users", new CreateUserCommand());
        var user = (await createUserResponse.Content.ReadFromJsonAsync<UserDto>())!;

        // 2. Realiza o empréstimo
        using var loanRequest = new HttpRequestMessage(HttpMethod.Post, "/loans")
        {
            Content = JsonContent.Create(new
            {
                BookId = book.Id,
                UserId = user.Id,
                ExpectedReturnDateUtc = DateTime.UtcNow.AddDays(10)
            })
        };
        loanRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var loanResponse = await _client.SendAsync(loanRequest);
        loanResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdLoan = (await loanResponse.Content.ReadFromJsonAsync<LoanResponse>())!;

        // 3. Executa a devolução
        var returnResponse = await _client.PostAsync($"/loans/{createdLoan.Id}/return", null);
        returnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedLoan = (await returnResponse.Content.ReadFromJsonAsync<LoanResponse>())!;

        // 4. Assert: Status atualizado para Returned e data de devolução preenchida
        returnedLoan.Status.Should().Be("Returned");
        returnedLoan.ReturnedAtUtc.Should().NotBeNull();

        // 5. Assert: Estoque do livro foi recomposto (+1)
        var bookAfterReturn = await _client.GetFromJsonAsync<BookResponse>($"/books/{book.Id}");
        bookAfterReturn!.AvailableCopies.Should().Be(2);

        // 6. Assert: Histórico do livro preserva o empréstimo (nunca apagado silenciosamente)
        var bookHistoryResponse = await _client.GetAsync($"/books/{book.Id}/history");
        bookHistoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var bookHistory = await bookHistoryResponse.Content.ReadFromJsonAsync<List<BookLoanHistoryResponse>>();
        bookHistory.Should().Contain(h => h.LoanId == createdLoan.Id && h.Status == "Returned");

        // 7. Assert: Histórico do usuário também preserva o registro
        var userLoans = await _client.GetFromJsonAsync<List<DetailedLoanResponse>>($"/users/{user.Id}/loans");
        userLoans.Should().Contain(l => l.Id == createdLoan.Id && l.Status == "Returned");

        // 8. Assert: Trilha de auditoria contém eventos de criação e devolução
        var auditEvents = await _client.GetFromJsonAsync<List<AuditEvent>>("/audit-events");
        auditEvents.Should().Contain(a => a.EntityName == "Loan" && a.EntityId == createdLoan.Id.ToString() && a.Action == "Created");
        auditEvents.Should().Contain(a => a.EntityName == "Loan" && a.EntityId == createdLoan.Id.ToString() && a.Action == "Return");
    }

    [Fact]
    public async Task Should_Preserve_History_And_Audit_Events_After_Loan_Cancellation()
    {
        // 1. Arrange: Cria livro e usuário
        var isbn = $"978{Random.Shared.Next(100000000, 999999999)}";
        var createBookResponse = await _client.PostAsJsonAsync("/books",
            new CreateBookCommand("Código Limpo", "Uncle Bob", isbn, 3));
        var book = (await createBookResponse.Content.ReadFromJsonAsync<BookResponse>())!;

        var createUserResponse = await _client.PostAsJsonAsync("/users", new CreateUserCommand());
        var user = (await createUserResponse.Content.ReadFromJsonAsync<UserDto>())!;

        // 2. Realiza o empréstimo
        using var loanRequest = new HttpRequestMessage(HttpMethod.Post, "/loans")
        {
            Content = JsonContent.Create(new
            {
                BookId = book.Id,
                UserId = user.Id,
                ExpectedReturnDateUtc = DateTime.UtcNow.AddDays(5)
            })
        };
        loanRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var loanResponse = await _client.SendAsync(loanRequest);
        var createdLoan = (await loanResponse.Content.ReadFromJsonAsync<LoanResponse>())!;

        // 3. Executa o cancelamento do empréstimo
        var cancellationReason = "Leitor solicitou cancelamento antes da retirada física.";
        var cancelResponse = await _client.PostAsJsonAsync($"/loans/{createdLoan.Id}/cancel", new { Reason = cancellationReason });
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelledLoan = (await cancelResponse.Content.ReadFromJsonAsync<LoanResponse>())!;

        // 4. Assert: Status atualizado para Cancelled
        cancelledLoan.Status.Should().Be("Cancelled");

        // 5. Assert: Estoque do livro foi recomposto
        var bookAfterCancel = await _client.GetFromJsonAsync<BookResponse>($"/books/{book.Id}");
        bookAfterCancel!.AvailableCopies.Should().Be(3);

        // 6. Assert: Histórico do livro preserva a existência do empréstimo cancelado
        var bookHistory = await _client.GetFromJsonAsync<List<BookLoanHistoryResponse>>($"/books/{book.Id}/history");
        bookHistory.Should().Contain(h => h.LoanId == createdLoan.Id && h.Status == "Cancelled");

        // 7. Assert: Trilha de auditoria gravou o cancelamento com o motivo
        var auditEvents = await _client.GetFromJsonAsync<List<AuditEvent>>("/audit-events");
        auditEvents.Should().Contain(a =>
            a.EntityName == "Loan" &&
            a.EntityId == createdLoan.Id.ToString() &&
            a.Action == "Cancel" &&
            a.Details.Contains(cancellationReason));
    }

    private record UserDto(Guid Id);
}

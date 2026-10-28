using CSharpFunctionalExtensions;
using FluentValidation;
using Library.Application.BookModule.Models;
using Library.Domain.BookModule;
using Library.Domain.Exceptions;
using Library.Infra.Redis;
using MediatR;

namespace Library.Application.BookModule.Queries;

public sealed class CheckBookAvailabilityQuery : IRequest<Result<BookAvailableResponse>>
{
    public Guid Id { get; set; }

    public CheckBookAvailabilityQuery(Guid id)
    {
        Id = id;
    }
}

public sealed class CheckBookAvailabilityQueryValidator : AbstractValidator<CheckBookAvailabilityQuery>
{
    public CheckBookAvailabilityQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

public sealed class CheckBookAvailabilityQueryHandler(
    IBookRepository bookRepository,
    ICacheService cache) : IRequestHandler<CheckBookAvailabilityQuery, Result<BookAvailableResponse>>
{
    public async Task<Result<BookAvailableResponse>> Handle(CheckBookAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"books:{request.Id}:availability";

        var response = await cache.GetOrCreateAsync(
            cacheKey,
            factory: async () =>
            {
                var book = await bookRepository.RetrieveByIDAsync(request.Id, cancellationToken)
                    ?? throw new EntityNotFoundException(nameof(Book), request.Id);

                return new BookAvailableResponse(
                    book.Id,
                    book.Title,
                    book.AvailableCopies,
                    book.AvailableCopies > 0 && book.IsActive
                );
            },
            expiration: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken
        );

        return Result.Success(response);
    }
}

namespace Library.Infra.Redis;

public static class BookCacheKeys
{
    public const string CatalogActive = "books:catalog:active:true";
    public const string CatalogAll = "books:catalog:active:false";

    public static string Availability(Guid bookId)
    {
        return $"books:{bookId}:availability";
    }

    public static async Task InvalidateCatalogAsync(ICacheService cache, CancellationToken ct = default)
    {
        await Task.WhenAll(
            cache.RemoveAsync(CatalogActive, ct),
            cache.RemoveAsync(CatalogAll, ct)
        );
    }
}

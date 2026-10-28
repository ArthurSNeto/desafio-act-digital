using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Library.Infra.Redis;

public sealed class DistributedCacheService(
    IDistributedCache cache,
    ILogger<DistributedCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            string? cachedString = await cache.GetStringAsync(key, cancellationToken);
            return string.IsNullOrWhiteSpace(cachedString) ? default : JsonSerializer.Deserialize<T>(cachedString, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao obter chave '{Key}' do Redis. Prosseguindo com fallback.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            string payload = JsonSerializer.Serialize(value, JsonOptions);
            await cache.SetStringAsync(key, payload, options, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao salvar chave '{Key}' no Redis.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao remover chave '{Key}' do Redis.", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Cache HIT para a chave '{Key}'", key);
            return cached;
        }

        logger.LogDebug("Cache MISS para a chave '{Key}'. Executando factory...", key);
        var result = await factory();

        if (result is not null)
            await SetAsync(key, result, expiration, cancellationToken);

        return result!;
    }
}

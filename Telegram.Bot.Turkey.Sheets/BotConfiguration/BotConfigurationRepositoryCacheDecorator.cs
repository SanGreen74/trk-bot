using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

namespace Telegram.Bot.Turkey.Sheets.BotConfiguration;

internal class BotConfigurationRepositoryCacheDecorator : IBotConfigurationRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private const string CacheKey = "BotConfiguration_Cache_Key";
    
    private readonly IBotConfigurationRepository _botConfigurationRepository;
    private readonly IMemoryCache _memoryCache;

    public BotConfigurationRepositoryCacheDecorator(
        IBotConfigurationRepository botConfigurationRepository,
        IMemoryCache memoryCache)
    {
        _botConfigurationRepository = botConfigurationRepository;
        _memoryCache = memoryCache;
    }

    public async Task<BotConfigurationDto?> GetAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CacheKey, out BotConfigurationDto? botConfiguration))
        {
            return botConfiguration;
        }

        botConfiguration = await _botConfigurationRepository.GetAsync(cancellationToken);
        if (botConfiguration != null)
        {
            _memoryCache.Set(CacheKey, botConfiguration, CacheDuration);
        }

        return botConfiguration;
    }

    public async Task SetAsync(BotConfigurationDto botConfiguration, CancellationToken cancellationToken)
    {
        _memoryCache.Remove(CacheKey);
        await _botConfigurationRepository.SetAsync(botConfiguration, cancellationToken);
        _memoryCache.Set(CacheKey, botConfiguration, CacheDuration);
    }
}

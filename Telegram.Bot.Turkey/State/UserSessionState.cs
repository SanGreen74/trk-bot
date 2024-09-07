using Microsoft.Extensions.Caching.Memory;

namespace Telegram.Bot.Turkey.State;

public class UserSessionState : IUserSessionState
{
    private readonly IMemoryCache _memoryCache;
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);
    
    public UserSessionState(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void SetState<T>(string key, T value)
    {
        var stateKey = FormatStateKey(key);
        _memoryCache.Set(stateKey, value, SessionTimeout);
    }

    public T? GetState<T>(string key)
    {
        var stateKey = FormatStateKey(key);
        var cacheValue = _memoryCache.TryGetValue(stateKey, out var value) ? value : default;
        if (cacheValue is T result)
            return result;
        return default;
    }

    public void SetCommandName(string key, string commandName)
    {
        var commandKey = FormatCommandName(key);
        _memoryCache.Set(commandKey, commandName, SessionTimeout);
    }

    public string? GetCommandName(string key)
    {
        var commandKey = FormatCommandName(key);
        return _memoryCache.TryGetValue(commandKey, out var value) ? value as string : null;
    }

    public void Invalidate(string key)
    {
        var commandKey = FormatCommandName(key);
        var stateKey = FormatStateKey(key);
        _memoryCache.Remove(stateKey);
        _memoryCache.Remove(commandKey);
    }
    
    private static string FormatStateKey(string key) => $"{key}SessionState";
    
    private static string FormatCommandName(string key) => $"{key}SessionCommand";
}

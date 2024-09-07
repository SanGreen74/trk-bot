namespace Telegram.Bot.Turkey.State;

public interface IUserSessionState
{
    void SetState<T>(string key, T value);
    T? GetState<T>(string key);
    void SetCommandName(string key, string commandName);
    string? GetCommandName(string key);
    void Invalidate(string key);
}
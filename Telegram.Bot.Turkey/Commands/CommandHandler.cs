using Telegram.Bot.Turkey.State;
using Telegram.Bot.Types;

namespace Telegram.Bot.Turkey.Commands;

public abstract class CommandHandler
{
    public abstract required string CommandName { get; init; }

    private readonly IUserSessionState _sessionState;

    public CommandHandler(IUserSessionState sessionState)
    {
        _sessionState = sessionState;
    }
    
    public abstract Task StartHandle(Update update, CancellationToken ct);
    
    public abstract Task HandleIntermediateMessage(Update update, CancellationToken ct);

    protected void OnComplete(string userName)
    {
        _sessionState.Invalidate(userName);
    }
}
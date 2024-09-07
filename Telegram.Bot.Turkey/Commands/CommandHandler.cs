using Telegram.Bot.Types;

namespace Telegram.Bot.Turkey.Commands;

public abstract class CommandHandler
{
    public abstract required string CommandName { get; init; }

    // public abstract required string[] SupportedStates { get; init; }

    public abstract Task StartHandle(Update update, CancellationToken cancellationToken);
    
    public abstract Task HandleIntermediateMessage(Update update, CancellationToken ct);

    // protected async Task EndHandlingAsync(Update update, CancellationToken cancellationToken);
}
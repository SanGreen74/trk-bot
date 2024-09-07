using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Turkey;
using Telegram.Bot.Turkey.Commands;
using Telegram.Bot.Types;

public class MessageReceiver
{
    private readonly ITelegramBotClient _botClient;
    private readonly MainCommandHandler _handler;

    public MessageReceiver(ITelegramBotClient botClient, MainCommandHandler handler)
    {
        _botClient = botClient;
        _handler = handler;
    }
    
    public async Task StartReceiving(CancellationToken cancellationToken)
    {
        var me = await _botClient.GetMeAsync(cancellationToken: cancellationToken);
        Console.WriteLine($"Bot {me.Username} is running.");
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

        _botClient.StartReceiving(
            (_, update, arg3) => HandleUpdateAsync(update, arg3),
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
    }
    
    private async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        await _handler.Handle(update, cancellationToken);
    }
    
    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}

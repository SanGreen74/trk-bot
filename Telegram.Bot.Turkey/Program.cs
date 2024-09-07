using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Turkey;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static readonly string BotToken = "7509015005:AAFVg2mJx0lr7L3YrpbBbEXN51dXs6YYj80";
    private static TelegramBotClient botClient;

    static async Task Main(string[] args)
    {
        var configureServices = new ServiceCollection()
            .ConfigureServices(null);
        var cts = new CancellationTokenSource();
        var serviceProvider = configureServices.BuildServiceProvider();
        await serviceProvider.GetRequiredService<MessageReceiver>()
            .StartReceiving(cts.Token);
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        await cts.CancelAsync();
    }
}
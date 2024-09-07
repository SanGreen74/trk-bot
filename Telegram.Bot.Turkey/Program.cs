using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Turkey;
using Telegram.Bot.Turkey.Sheets;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var serviceProvider = new ServiceCollection()
            .ConfigureBotServices(configuration)
            .ConfigureSheets(configuration)
            .BuildServiceProvider();
        
        var cts = new CancellationTokenSource();
        
        await serviceProvider.GetRequiredService<MessageReceiver>()
            .StartReceiving(cts.Token);
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        await cts.CancelAsync();
    }
}
﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Turkey;
using Telegram.Bot.Turkey.Sheets;
using Telegram.Bot.Turkey.Sheets.Expenses;
using Telegram.Bot.Turkey.Sheets.Expenses.Models;
using System.Threading.Tasks;

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
        await Task.Delay(-1, cts.Token);
        Console.WriteLine("Cancellation requested. Exiting...");
        await cts.CancelAsync();
    }
}

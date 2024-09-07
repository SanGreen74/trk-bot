using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Telegram.Bot.Turkey.Sheets.BotConfiguration.Models;

namespace Telegram.Bot.Turkey.Sheets.BotConfiguration;

internal class BotConfigurationRepository : IBotConfigurationRepository
{
    private const string ConfigCellKey = "tg-config!A1:A1";

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };
    
    private readonly SheetsService _sheetsService;

    public BotConfigurationRepository(SheetsService sheetsService)
    {
        _sheetsService = sheetsService;
    }

    public async Task<BotConfigurationDto?> GetAsync(CancellationToken cancellationToken)
    {
        var getRequest = _sheetsService.Spreadsheets.Values.Get(SheetConstants.SheetId, ConfigCellKey);
        var response = await getRequest.ExecuteAsync(cancellationToken);
        if (response.Values.Count > 0)
        {
            var cells = response.Values.First();
            var cell = cells.FirstOrDefault();
            if (cell is not string cellValue)
                return null;
            try
            {
                return JsonSerializer.Deserialize<BotConfigurationDto>(cellValue, SerializerOptions);
            }
            catch (JsonException e)
            {
                return null;
            }
        }

        return null;
    }

    public async Task SetAsync(BotConfigurationDto botConfiguration, CancellationToken cancellationToken)
    {
        var valueRange = new ValueRange
        {
            Values = new List<IList<object>>
            {
                new List<object> { JsonSerializer.Serialize(botConfiguration, SerializerOptions) }
            }
        };

        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, SheetConstants.SheetId, ConfigCellKey);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED; // Вставка как есть

        await updateRequest.ExecuteAsync(cancellationToken);
    }
}

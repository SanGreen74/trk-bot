using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Telegram.Bot.Turkey.Sheets.Expenses;

internal class ExpensesService : IExpensesService
{
    private const string SheetName = "Расходы";
    
    private readonly SheetsService _sheets;

    public ExpensesService(SheetsService sheets)
    {
        _sheets = sheets;
    }
    
    public async Task<string[]> GetUsersAsync(CancellationToken cancellationToken)
    {
        var getRequest = _sheets.Spreadsheets.Values.Get(SheetConstants.SheetId, FormatUsersCells());
        var response = await getRequest.ExecuteAsync(cancellationToken);
        return ExtractUsers(response);

        string[] ExtractUsers(ValueRange valueRange)
        {
            throw new NotImplementedException();
        }

        string FormatUsersCells() => $"{SheetName}!D1:G1";
    }
}

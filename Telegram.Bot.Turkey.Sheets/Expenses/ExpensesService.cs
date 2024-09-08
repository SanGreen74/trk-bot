using System.Globalization;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Telegram.Bot.Turkey.Sheets.Expenses.Models;

namespace Telegram.Bot.Turkey.Sheets.Expenses;

internal class ExpensesService : IExpensesService
{
    private const string SheetName = "Расходы (test)";
    
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
            if (valueRange.Values.Count > 0)
            {
                return valueRange.Values.First()
                    .Select(x => x.ToString())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x!)
                    .ToArray();
            }

            return [];
        }

        string FormatUsersCells() => $"{SheetName}!D1:G1";
    }

    public async Task<InsertExpenseRowResponse> InsertExpenseRowAsync(InsertExpenseRowRequest request, CancellationToken cancellationToken)
    {
        // 1. Получаем данные из Google Sheets
        var sheetData = await _sheets.Spreadsheets.Values.Get(SheetConstants.SheetId, "A1:G1000")
            .ExecuteAsync(cancellationToken);
        var values = sheetData.Values ?? new List<IList<object>>();
        var emptyRowIndex = FindFirstEmptyRow(values);

        var range = $"{SheetName}!A{emptyRowIndex}:G{emptyRowIndex}";
        var rowToInsert = new List<object>
        {
            request.Date.ToString("dd.MM.yyyy"), // A: Дата
            request.Comment,                     // B: Комментарий
            request.WhoPaidName                  // C: Кто заплатил
        };
        var participantsHeaders = values
            .First()
            .Skip(3)
            .Select(x => x.ToString())
            .ToList();

        // Заполняем ячейки D:G на основе участников
        foreach (var header in participantsHeaders)
        {
            var participant = request.Participants
                .FirstOrDefault(p => p.Name.Equals(header, StringComparison.InvariantCultureIgnoreCase));
            if (participant != null)
            {
                rowToInsert.Add(participant.Amount); // Если участник не найден, оставляем ячейку пустой}
            }
            else
            {
                rowToInsert.Add(string.Empty);
            }
        }
        var valueRange = new ValueRange
        {
            Values = new List<IList<object>> { rowToInsert }
        };
        
        var updateRequest = _sheets.Spreadsheets.Values.Update(valueRange, SheetConstants.SheetId, range);
        updateRequest.ValueInputOption =
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync(cancellationToken);
        return new InsertExpenseRowResponse
        {
            InsertedInRow = emptyRowIndex
        };
    }

    public async Task<GetExpensesResponse> GetExpensesAsync(string userName, CancellationToken cancellationToken)
    {
        var range = $"{SheetName}!A1:G1000";
        var request = _sheets.Spreadsheets.Values.Get(SheetConstants.SheetId, range);
        var response = await request.ExecuteAsync(cancellationToken);

        IList<IList<object>> values = response.Values;

        if (values == null || values.Count == 0)
        {
            throw new Exception("No data found.");
        }

        // Определяем индекс столбца, соответствующий userName
        var headerRow = values[0];
        var userColumnIndex = -1;

        for (int i = 0; i < headerRow.Count; i++)
        {
            var headerText = headerRow[i].ToString();
            if (headerText != null && headerText.Equals(userName, StringComparison.OrdinalIgnoreCase))
            {
                userColumnIndex = i;
                break;
            }
        }

        if (userColumnIndex == -1)
        {
            throw new Exception($"User '{userName}' not found in the header row.");
        }

        // Извлекаем расходы пользователя из соответствующего столбца
        var items = new List<GetExpensesResponse.Item>();

        for (var rowIndex = 1; rowIndex < values.Count; rowIndex++) // Пропускаем заголовок
        {
            var row = values[rowIndex];

            // Проверяем, есть ли данные в этом ряду для пользователя
            var valueMaybe = row.Count > userColumnIndex
                ? row[userColumnIndex].ToString()?
                    .Replace(",", ".")
                    .Replace("$", "")
                : null;
            if (valueMaybe != null && decimal.TryParse(valueMaybe, CultureInfo.InvariantCulture, out var amount))
            {
                // Получаем дату и комментарий
                var date = DateOnly.ParseExact(row[0].ToString()!, "dd.MM.yyyy");
                var comment = row[1].ToString()!;

                // Создаем объект Item
                var item = new GetExpensesResponse.Item
                {
                    AmountUsd = amount,
                    Date = date,
                    Comment = comment
                };

                items.Add(item);
            }
        }

        // Возвращаем объект с расходами пользователя
        return new GetExpensesResponse
        {
            Items = items.ToArray()
        };
    }

    private static int FindFirstEmptyRow(IList<IList<object>> values)
    {
        for (var i = 0; i < values.Count; i++) // Пропускаем первую строку, т.к. это заголовок
        {
            var row = values[i];
            if (row.All(x => string.IsNullOrEmpty(x.ToString())))
            {
                return i + 1;
            }
        }

        return values.Count + 1;
    }
}

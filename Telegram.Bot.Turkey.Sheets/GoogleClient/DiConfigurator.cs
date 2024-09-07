using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace Telegram.Bot.Turkey.Sheets.GoogleClient;

internal static class DiConfigurator
{
    private const string FilePathSetting = "GOOGLE_CREDENTIALS_FILE_PATH;";

    public static IServiceCollection ConfigureGoogleClient(this IServiceCollection services, IConfiguration configuration)
    {
        var credentialsFilePath = configuration[FilePathSetting];
        if (string.IsNullOrEmpty(credentialsFilePath))
        {
            throw new InvalidOperationException($"Не удалось найти настройку {FilePathSetting}");
        }
        
        var credential = GoogleCredential.FromFile(credentialsFilePath)
            .CreateScoped(SheetsService.Scope.Spreadsheets);
        
        var service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Telegram Bot"
        });
        
        services.AddSingleton<SheetsService>(_ => service);
        return services;
    }
}

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Telegram.Bot.Turkey/Telegram.Bot.Turkey.csproj", "Telegram.Bot.Turkey/"]
RUN dotnet restore "Telegram.Bot.Turkey/Telegram.Bot.Turkey.csproj"
COPY . .
WORKDIR "/src/Telegram.Bot.Turkey"
RUN dotnet build "Telegram.Bot.Turkey.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Telegram.Bot.Turkey.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Telegram.Bot.Turkey.dll"]

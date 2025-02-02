# Используем официальный образ .NET
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Копируем файл .csproj в рабочую папку
COPY ["tiktoktg.csproj", "./"]

# Выполняем восстановление зависимостей
RUN dotnet restore "tiktoktg.csproj"

# Копируем все остальные файлы проекта
COPY . .

# Собираем проект
RUN dotnet build "tiktoktg.csproj" -c Release -o /app/build

# Публикуем проект
FROM build AS publish
RUN dotnet publish "tiktoktg.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "tiktoktg.dll"]



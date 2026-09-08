FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копіюємо весь репозиторій
COPY . .

# Робимо restore та publish прямо для WebApi
RUN dotnet restore src/WebApi/WebApi.csproj
RUN dotnet publish src/WebApi/WebApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
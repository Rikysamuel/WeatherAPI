FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WeatherApi.sln ./
COPY src/api/WeatherApi.csproj src/api/
COPY src/core/WeatherApi.Core.csproj src/core/
COPY src/external/WeatherApi.External.csproj src/external/

RUN dotnet restore src/api/WeatherApi.csproj

COPY . .
RUN dotnet publish src/api/WeatherApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080

RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WeatherApi.dll"]

# Weather API

.NET 8 microservice providing current weather, forecasts, historical data, CSV export, location management, and alerts. Powered by OpenWeatherMap API.

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Current Weather**: Real-time conditions for tracked locations
- **Forecasts**: 7-day forecast with `ForecastDayResponse` (Date, Description, Temperature, Min/Max Temp)
- **Historical Data**: Cached past weather (1-hour TTL)
- **CSV Export**: Unified export with observed (today) and predicted (future) data
- **Location Management**: Add/delete locations with auto-refresh weather
- **Weather Alerts**: Date-filtered alerts, email subscriptions (mock)
- **Auth**: JWT registration + token endpoint
- **Security**: Rate limiting (5/min auth, 60/min weather), security headers, CORS deny-all in production
- **Background Worker**: Configurable interval (default 60 min), refreshes + prunes stale data (30-day cutoff)

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 8 (C# 12) |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Resilience | Standard resilience handler (retry + circuit breaker) |
| Auth | JWT Bearer tokens |
| Logging | Serilog (Console + Azure) |
| API Docs | Swagger / OpenAPI |

## Architecture

```
src/
├── api/                     # Controllers, middleware, AuthenticationService
├── core/                    # Domain models, interfaces, services, entities
└── external/                # OWM client, background workers
tests/
├── unit/                    # 12 unit tests (in-memory DB + mocks)
└── integration/             # 7 integration tests (CustomWebApplicationFactory)
```

## API Endpoints

### Public
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check |
| `GET` | `/api/weather/current/{locationId}` | Current weather |
| `GET` | `/api/weather/forecast/{locationId}?days=5` | Forecast (1-7 days) |
| `GET` | `/api/weather/historical/{locationId}?date=YYYY-MM-DD` | Historical weather (cached) |
| `GET` | `/api/weather/export/{locationId}?days=5` | CSV export |

### Auth (JWT required)
| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/auth/register` | Register user (password: 8+ chars, 1 uppercase, 1 digit) |
| `POST` | `/api/auth/token` | Login, returns JWT |

### Protected
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/location` | List locations |
| `GET` | `/api/location/{id}` | Get location by ID |
| `POST` | `/api/location` | Add location (city name) |
| `DELETE` | `/api/location/{id}` | Delete location |
| `GET` | `/api/alert?from=...&to=...&locationId=` | Filtered alerts (from/to required, max 7 days) |
| `POST` | `/api/alert/subscribe` | Subscribe to alerts |
| `DELETE` | `/api/alert/unsubscribe/{id}` | Unsubscribe |
| `GET` | `/api/alert/subscriptions?email=` | List subscriptions |
| `DELETE` | `/api/weather/{locationId}` | Delete weather data for location |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 16](https://www.postgresql.org/download/)
- [OpenWeatherMap API key](https://openweathermap.org/api) (free tier works)

### Local Setup

1. **Set up the database**
   ```sql
   -- Run db_schemas.sql for fresh install
   psql -h localhost -U your-username -d weather -f sql/db_schemas.sql
   ```

2. **Configure secrets** — create `src/api/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=weather;Username=your-username;Password=your-password"
     },
     "OpenWeatherMap": {
       "ApiKey": "your-openweathermap-api-key",
       "BaseUrl": "https://api.openweathermap.org"
     },
     "Jwt": {
       "Key": "your-256-bit-secret-key"
     }
   }
   ```

3. **Run the API**
   ```bash
   dotnet run --project src/api
   ```

4. **Swagger UI**: `http://localhost:5001/swagger` or `https://localhost:7001/swagger`

### Running Tests

```bash
dotnet test WeatherApi.sln
```

## Docker

```bash
docker build -t your-acr-name.azurecr.io/weather-api:latest .
docker push your-acr-name.azurecr.io/weather-api:latest
```

## Deployment (Azure)

### CI/CD
Push to `master` triggers GitHub Actions with path filter (`src/**`, `tests/**`, `Dockerfile`, workflow files):
1. Build + test (in-memory DB, no PostgreSQL dependency)
2. Login to Azure + ACR
3. Build & push Docker image to ACR
4. Deploy to Azure App Service (B1, Southeast Asia)

### App Settings (Environment Variables)
| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | Production |
| `ASPNETCORE_URLS` | http://+:8080 |
| `ConnectionStrings__DefaultConnection` | Azure PostgreSQL connection string |
| `OpenWeatherMap__ApiKey` | OWM API key |
| `Jwt__Key` | JWT signing secret |
| `Worker__RefreshIntervalMinutes` | 60 |

### Logging
- **Runtime logs**: App Service → Monitoring → Log stream (Serilog console output)

## Database

Six tables across the `weather` PostgreSQL schema:

| Table | Purpose |
|-------|---------|
| `Users` | Authentication (username + hashed password) |
| `Locations` | Tracked cities with coordinates |
| `DailyWeather` | Observed + predicted weather per location per day |
| `HourlySummaries` | Hourly forecast data (replaced each worker cycle) |
| `Alerts` | Weather alerts with severity and active status |
| `AlertSubscriptions` | Email subscriptions per location |

## Design Decisions

### Forecast Response vs Current Weather
- **Current weather** (`GET /api/weather/current/{id}`) returns full `WeatherData` (all observed fields)
- **Forecast** (`GET /api/weather/forecast/{id}`) returns `ForecastDayResponse` (City, Country, Date, Description, Temperature, Min/Max)
- **Export** returns CSV with unified columns: observed fills today's row, predicted fills future rows

### Authentication
- Registration-only (no seeded admin user)
- Password validation: 8+ chars, 1 uppercase, 1 digit
- JWT expiry configurable via `Jwt:ExpiryMinutes`

### Background Worker
- Fetches OWM OneCall API for all tracked locations
- Upserts observed + predicted into `DailyWeather`
- Replaces `HourlySummaries` (delete-all + re-insert)
- Prunes data older than 30 days
- Managed Identity via Azure SDK (no hardcoded credentials)

### Error Handling
| Exception | HTTP Status |
|-----------|-------------|
| `KeyNotFoundException` | 404 |
| `UnauthorizedAccessException` | 401 |
| `InvalidOperationException` | 400 |
| Other | 500 |

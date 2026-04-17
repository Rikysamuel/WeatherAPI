# Weather API

A production-grade weather API built with .NET 8, providing current weather conditions, forecasts, historical data retrieval, CSV export capabilities, location management, and weather alerts. This microservice follows clean architecture principles and implements industry best practices for resilience, scalability, and maintainability.

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Azure-blue)

## Features

- 🌤️ **Current Weather**: Get real-time weather conditions for any tracked location
- 📅 **Forecasts**: 7-day weather forecasts with hourly breakdowns
- 📚 **Historical Data**: Access to past weather conditions (with configurable caching)
- 📍 **Location Management**: CRUD operations for tracked locations
- ⚠️ **Weather Alerts**: Create and manage weather alerts with severity levels
- 📤 **Data Export**: Export weather data in CSV format
- 🔐 **JWT Authentication**: Secure protected endpoints with bearer tokens
- 🛡️ **Resilience Patterns**: Built-in retry mechanisms and circuit breakers
- 📊 **Caching**: Intelligent in-memory caching for improved performance
- 📈 **Health Monitoring**: Built-in health check endpoints
- 📖 **API Documentation**: Interactive Swagger UI for easy exploration

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 8 (C# 12) |
| Database | PostgreSQL 15 |
| ORM | Entity Framework Core 8 |
| Resilience | Polly (retry + circuit breaker) |
| Auth | JWT Bearer tokens |
| Logging | Serilog (Console + Azure) |
| API Docs | Swagger / OpenAPI |

## Architecture

The application follows Clean Architecture principles with a separation of concerns:

```
src/
├── api/                  # Presentation layer (Web API entry point, controllers, middleware)
├── core/                 # Business logic layer (domain models, interfaces, services)
└── external/             # External integrations (OWM client, background workers)
```

## API Endpoints

### Public Endpoints
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check endpoint |
| `GET` | `/api/weather/current/{locationId}` | Get current weather for a location |
| `GET` | `/api/weather/forecast/{locationId}?days=5` | Get weather forecast (1-7 days) |
| `GET` | `/api/weather/historical/{locationId}?date=YYYY-MM-DD` | Get historical weather (cached) |
| `GET` | `/api/weather/export/{locationId}?format=csv` | Export weather data as CSV |

### Protected Endpoints (JWT required)
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/locations` | List all tracked locations |
| `POST` | `/api/locations` | Add a new location |
| `GET` | `/api/alerts` | List all weather alerts |
| `DELETE` | `/api/weather/{locationId}` | Delete weather data for a location |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [OpenWeatherMap API key](https://openweathermap.org/api) (free tier works)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd weather-api
   ```

2. **Set up the database**
   ```bash
   # Create the database
   createdb weatherdb_dev

   # Run migrations (after EF Core is set up)
   dotnet ef database update --project src/core --startup-project src/api
   ```

3. **Configure secrets**
   ```bash
   # Set your OpenWeatherMap API key
   dotnet user-secrets set "OpenWeatherMap:ApiKey" "your-openweathermap-api-key"

   # Set JWT secret (use a strong secret in production)
   dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key"
   ```

4. **Run the API**
   ```bash
   dotnet run --project src/api/WeatherApi.csproj
   ```

5. **Access Swagger UI**
   Open your browser to: `https://localhost:7001/swagger`

### Running Tests

```bash
# Run unit tests
dotnet test tests/unit/WeatherApi.Tests.Unit.csproj

# Run integration tests
dotnet test tests/integration/WeatherApi.Tests.Integration.csproj

# Run all tests
dotnet test WeatherApi.sln
```

## Design Principles

### Caching Strategy
- **Current weather & forecast**: In-memory cache with 10-minute TTL
- **Historical data**: Cache-fetched with 1-hour TTL (not persisted per assignment spec)
- Cache key pattern: `weather:{type}:{locationId}:{date/days}`

### Resilience (Polly)
- **Retry**: 3 attempts with exponential backoff (2s, 4s, 8s)
- **Circuit breaker**: Opens after 5 consecutive failures, resets after 30s
- **Timeout**: 15 seconds per OWM request

### Data Management
- **Background Worker**: Refreshes historical data every 6 hours
- **Data Pruning**: Automatically removes data older than 30 days
- **Location Tracking**: Manage cities and geographic coordinates

### Alerts System
- Alerts are stored in PostgreSQL only (no external notification service)
- Severity levels: `Low`, `Medium`, `High`, `Critical`
- Automatic alert creation from OpenWeatherMap warnings
- Mock email dispatching for demonstration purposes

### Error Handling
- RFC 7807 ProblemDetails for all error responses
- Maps exceptions to appropriate HTTP status codes
- `KeyNotFoundException` → 404
- `UnauthorizedAccessException` → 401
- `InvalidOperationException` → 400
- Other → 500

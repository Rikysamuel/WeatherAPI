# Weather API

A production-grade weather API built with .NET 8, providing current weather, forecasts, historical data, CSV export, location management, and weather alerts.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 8 (C#) |
| Database | PostgreSQL 15 |
| ORM | Entity Framework Core 8 |
| Resilience | Polly (retry + circuit breaker) |
| Auth | JWT Bearer tokens |
| Logging | Serilog (Console + Azure) |
| API Docs | Swagger / OpenAPI |


## Architecture

```
src/
├── api/                  # Web API (entry point, endpoints, middleware)
├── core/                 # Domain models, interfaces, business services
└── infra/                # EF Core DbContext, HTTP client, background workers
tests/
├── unit/                 # Fast, isolated unit tests (xUnit + Moq)
└── integration/          # End-to-end tests (WebApplicationFactory + Testcontainers)
infra/
├── main.bicep            # Azure resource definitions
├── deploy.bicep          # Subscription-level deployment orchestrator
└── azuredeploy.parameters.prod.json  # Production parameter overrides
```

## Endpoints

### Public
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check |
| `GET` | `/api/weather/current/{city}` | Current weather |
| `GET` | `/api/weather/forecast/{city}?days=5` | Weather forecast (1-7 days) |
| `GET` | `/api/weather/historical/{city}?date=YYYY-MM-DD` | Historical weather (cached) |
| `GET` | `/api/weather/export/{city}?format=csv` | Export as CSV |

### Protected (JWT required)
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/locations` | List all tracked locations |
| `POST` | `/api/locations` | Add a new location |
| `GET` | `/api/alerts` | List all weather alerts |
| `POST` | `/api/alerts` | Create a weather alert |

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL 15+
- OpenWeatherMap API key (free tier works)

### Local Development

1. **Clone & configure**
   ```bash
   git clone <repo>
   cd weather-api
   ```

2. **Set up the database**
   ```bash
   # Create the database
   createdb weatherdb_dev
   
   # Run migrations (after EF Core is set up)
   dotnet ef database update --project src/infra --startup-project src/api
   ```

3. **Configure secrets (optional, for production keys)**
   ```bash
   dotnet user-secrets set "OpenWeatherMap:ApiKey" "your-key-here"
   dotnet user-secrets set "Jwt:Key" "your-super-secret-key-here"
   ```

4. **Run the API**
   ```bash
   dotnet run --project src/api/WeatherApi.csproj
   ```

5. **Access Swagger UI**
   ```
   https://localhost:5001/swagger
   ```

### Run Tests
```bash
# Unit tests
dotnet test tests/unit/WeatherApi.Tests.Unit.csproj

# Integration tests
dotnet test tests/integration/WeatherApi.Tests.Integration.csproj

# All tests
dotnet test WeatherApi.sln
```

## Deployment

### Azure (Bicep)

```bash
# Login to Azure
az login

# Create resource group and deploy
az deployment sub create \
  --location southeastasia \
  --template-file infra/deploy.bicep \
  --parameters resourceGroupName=rg-weatherapi

# Or deploy to an existing resource group
az deployment group create \
  --resource-group rg-weatherapi \
  --template-file infra/main.bicep \
  --parameters postgresAdminPassword='your-secure-password'
```

### GitHub Actions

The pipeline (`ci-cd.yml`) runs on every push and PR:
1. **Build & Test** — restore, build, run tests
2. **Deploy** — (main branch only) deploy infrastructure + app to Azure
3. **Validate Infra** — (PRs only) lint and validate Bicep files

Required secrets:
- `AZURE_CREDENTIALS` — Azure service principal JSON
- `DB_PASSWORD` — PostgreSQL admin password
- `OWM_API_KEY` — OpenWeatherMap API key

Required variables:
- `AZURE_RESOURCE_GROUP` — Resource group name
- `APP_SERVICE_NAME` — Azure App Service name

## Design Decisions

### Caching Strategy
- **Current weather & forecast**: In-memory cache with 30-minute TTL
- **Historical data**: Cache-fetched (not persisted in DB per assignment spec)
- Cache key pattern: `weather:{type}:{city}:{date/days}`

### Resilience (Polly)
- **Retry**: 3 attempts with exponential backoff (2s, 4s, 8s)
- **Circuit breaker**: Opens after 5 consecutive failures, resets after 30s
- **Timeout**: 15 seconds per OWM request

### Alerts
- Alerts are stored in PostgreSQL only (no external notification service)
- Severity levels: `Low`, `Medium`, `High`, `Critical`
- Background worker runs every 6 hours to refresh historical data

### Error Handling
- RFC 7807 ProblemDetails for all error responses
- Maps exceptions to appropriate HTTP status codes
- `KeyNotFoundException` → 404
- `UnauthorizedAccessException` → 401
- `InvalidOperationException` → 400
- Other → 500

## TODO / Next Steps

- [ ] Implement actual OWM API response mapping in `WeatherService`
- [ ] Complete `LocationService` with EF Core database operations
- [ ] Complete `AlertService` with EF Core database operations
- [ ] Add EF Core migrations
- [ ] Add JWT token generation endpoint or external auth provider
- [ ] Implement proper JWT integration test with valid tokens
- [ ] Add rate limiting
- [ ] Add health checks with `AspNetCore.Diagnostics.HealthChecks`
- [ ] Add Docker support / Dockerfile

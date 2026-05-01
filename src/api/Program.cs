using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using WeatherApi.Core.Common;
using WeatherApi.Core.Data;
using WeatherApi.Core.Exceptions;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Services;
using WeatherApi.External.Extensions;
using WeatherApi.External.Workers;
using WeatherApi.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext());

    // ── Controllers & Routing ────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddRouting(options => options.LowercaseUrls = true);
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger ──────────────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Weather API",
            Version = "v1",
            Description = "Weather API Microservice"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
        });
    });

    // ── Authentication & Authorization ───────────────────────────────
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
    var jwtIssuer = jwtSettings["Issuer"] ?? "weatherapi";
    var jwtAudience = jwtSettings["Audience"] ?? "weatherapi";

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
    builder.Services.AddAuthorization();

    // ── Database ─────────────────────────────────────────────────────
    builder.Services.AddDbContext<WeatherDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── Application Services ─────────────────────────────────────────
    builder.Services.AddOwmClient(builder.Configuration);
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<IWeatherService, WeatherService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IAuthenticationService, WeatherApi.Services.AuthenticationService>();

    // ── Caching ──────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();
    builder.Services.Configure<MemoryCacheOptions>(options =>
    {
        options.SizeLimit = 1024;
    });

    // ── Background Workers ───────────────────────────────────────────
    builder.Services.AddHostedService<HistoricalDataWorker>();

    // ── Rate Limiting ───────────────────────────────────────────────
    var rateLimitSection = builder.Configuration.GetSection("RateLimiting");
    var authPermitLimit = rateLimitSection.GetValue<int>("AuthPermitLimit", 5);
    var authWindowMinutes = rateLimitSection.GetValue<int>("AuthWindowMinutes", 1);
    var weatherPermitLimit = rateLimitSection.GetValue<int>("WeatherPermitLimit", 60);
    var weatherWindowMinutes = rateLimitSection.GetValue<int>("WeatherWindowMinutes", 1);

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter(Constants.Policy.AuthPolicy, opt =>
        {
            opt.PermitLimit = authPermitLimit;
            opt.Window = TimeSpan.FromMinutes(authWindowMinutes);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
        options.AddFixedWindowLimiter(Constants.Policy.WeatherPolicy, opt =>
        {
            opt.PermitLimit = weatherPermitLimit;
            opt.Window = TimeSpan.FromMinutes(weatherWindowMinutes);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });
        options.RejectionStatusCode = 429;
    });

    // ── CORS ─────────────────────────────────────────────────────────
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    builder.Services.AddCors(options =>
    {
        if (corsOrigins is { Length: > 0 })
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        }
        else if (builder.Environment.IsDevelopment())
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        }
        else
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins();
            });
        }
    });

    var app = builder.Build();

    // ── Environment-specific Middleware ──────────────────────────────
    app.UseSwagger();
    app.UseSwaggerUI();
    if (app.Environment.IsProduction())
        app.UseHsts();

    // ── Middleware Pipeline ──────────────────────────────────────────
    app.UseSerilogRequestLogging();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseCors();
    if (app.Environment.IsProduction())
        app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Endpoints ────────────────────────────────────────────────────
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
       .WithName("HealthCheck")
       .WithTags("Health");

    // ── Error Handling ───────────────────────────────────────────────
    app.UseExceptionHandler("/error");
    app.Map("/error", (HttpContext context) =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: "An error occurred",
            statusCode: statusCode,
            type: "Error",
            detail: statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception?.Message ?? "Error");
    }).ExcludeFromDescription();

    Log.Information("Starting Weather API");
    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

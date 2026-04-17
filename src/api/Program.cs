using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using WeatherApi.Core.Data;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Services;
using WeatherApi.External.Workers;

// ── Serilog ──
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext());

    // ── Controllers & Swagger ──
    builder.Services.AddControllers();
    builder.Services.AddRouting(options => options.LowercaseUrls = true);
    builder.Services.AddEndpointsApiExplorer();

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

    // ── Authentication (JWT) ──
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSettings["Key"];
    var jwtIssuer = jwtSettings["Issuer"] ?? "weatherapi";
    var jwtAudience = jwtSettings["Audience"] ?? "weatherapi";

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddAuthorization();

    // ── Database (EF Core + PostgreSQL) ──
    builder.Services.AddDbContext<WeatherDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    // ── OWM Client ──
    builder.Services.AddOwmClient(builder.Configuration);

    // ── Application Services ──
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<IWeatherService, WeatherService>();
    builder.Services.AddScoped<IExportService, ExportService>();

    // ── Cache (in-memory, 30 min TTL) ──
    builder.Services.AddMemoryCache();
    builder.Services.Configure<MemoryCacheOptions>(options =>
    {
        options.SizeLimit = 1024;
    });

    // ── Hosted Services ──
    builder.Services.AddHostedService<HistoricalDataWorker>();

    // ── CORS ──
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // ── Middleware Pipeline ──
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // ── Health Check ──
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
       .WithName("HealthCheck")
       .WithTags("Health");

    // ── Error Handling ──
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
                    ? "InternalServerErrorexception" 
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

public record TokenRequest(string? Username = null);
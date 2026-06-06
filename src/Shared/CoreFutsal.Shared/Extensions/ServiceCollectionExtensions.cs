using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.RateLimiting;

namespace CoreFutsal.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFutsalDefaults(this IServiceCollection services, IConfiguration config, string serviceName)
    {
        services.AddDbContext<FutsalContext>(o =>
            o.UseSqlServer(config.GetConnectionString("futsalConn")));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
                };
            });

        var redisConn = config["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConn))
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
        else
            services.AddDistributedMemoryCache();

        services.AddAuthorization();
        services.AddHealthChecks()
            .AddDbContextCheck<FutsalContext>("database");

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            o.AddFixedWindowLimiter("login", opts =>
            {
                opts.Window            = TimeSpan.FromMinutes(1);
                opts.PermitLimit       = 10;
                opts.QueueLimit        = 0;
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            o.AddFixedWindowLimiter("register", opts =>
            {
                opts.Window            = TimeSpan.FromMinutes(1);
                opts.PermitLimit       = 5;
                opts.QueueLimit        = 0;
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = $"CoreFutsal — {serviceName}", Version = "v1" });

            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token."
            });

            o.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    []
                }
            });
        });

        return services;
    }

    public static WebApplication UseFutsalDefaults(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");
        return app;
    }

    public static async Task MigrateAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FutsalContext>();
        await db.Database.MigrateAsync();
    }
}

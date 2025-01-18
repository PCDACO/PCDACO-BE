using API.Middlewares;
using Carter;
using Domain.Shared;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistance;
using UseCases;
using UseCases.Utils;

namespace API;

public static class BuilderConfig
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add swagger 
        services.AddSwaggerGen(option =>
        {
            option.EnableAnnotations();
            option.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API",
                Version = "v1"
            });
            option.AddSecurityDefinition(name: "Bearer", securityScheme: new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter the Bearer Authorization string as following: `Bearer Generated-JWT-Token`",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
            {
                new OpenApiSecurityScheme
                {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                },
                []
            }
            });
        });
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["ISSUER"],
                    ValidAudience = configuration["AUDIENCE"],
                });
        services.AddAuthorization();
        // Add cors
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });

        // Add services to the container.
        services.AddSingleton(new JwtSettings()
        {
            Issuer = configuration["ISSUER"] ?? throw new Exception("Issuer is missing"),
            Audience = configuration["AUDIENCE"] ?? throw new Exception("Audience is missing"),
            SecretKey = configuration["SECRET_KEY"] ?? throw new Exception("SecretKey is missing"),
            TokenExpirationInMinutes = int.Parse(
                configuration["TOKEN_EXPIRATION_IN_MINUTES"] ?? throw new Exception("TokenExpirationInMinutes is missing")
            )
        });
        services.AddScoped<TokenService>();
        services.AddScoped<AuthMiddleware>();
        services.AddExceptionHandler<ExceptionHandlerMiddleware>();
        services.AddProblemDetails();
        services.AddCarter();
        // Add services of other layers
        services.AddPersistance(configuration);
        services.AddUseCases();
        services.AddInfrastructure();
        return services;
    }
}
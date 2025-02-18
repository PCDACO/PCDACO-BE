using System.Text;

using API.Middlewares;

using Carter;

using Domain.Data;
using Domain.Shared;

using Infrastructure;
using Infrastructure.Encryption;
using Infrastructure.Medias;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NetTopologySuite.Geometries;

using Persistance;

using UseCases;
using UseCases.Abstractions;
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
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                {
                    options.Authority = configuration["ISSUER"];
                    options.Audience = configuration["AUDIENCE"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        LifetimeValidator = (notBefore, expires, securityToken, validationParameters) =>
                        {
                            // Custom expiration logic
                            if (expires.HasValue && expires.Value < DateTime.UtcNow)
                            {
                                // Token is expired
                                return false;
                            }
                            return true; // Token is valid
                        },
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["SECRET_KEY"] ?? throw new Exception("Secret Key is missing"))),
                    };
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
        services.AddScoped<IAesEncryptionService, AesEncryptionService>();
        services.AddScoped<IKeyManagementService, KeyManagementService>();
        services.AddScoped<ICloudinaryServices, CloudinaryServices>();
        services.AddScoped<TokenService>();
        services.AddScoped<AuthMiddleware>();
        // Add singletons
        services.AddSingleton(new EncryptionSettings()
        {
            Key = configuration["MASTER_KEY"] ?? throw new Exception("Encryption Key is missing")
        });
        services.AddSingleton(NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));
        services.AddSingleton<DeviceStatusesData>();
        services.AddProblemDetails();
        // Add exception handlers
        services.AddExceptionHandler<ValidationAppExceptionHandler>();
        services.AddExceptionHandler<ExceptionHandlerMiddleware>();
        services.AddCarter();
        // Add services of other layers
        services.AddPersistance(configuration);
        services.AddUseCases();
        services.AddInfrastructure();
        services.AddDistributedMemoryCache();
        return services;
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Domain.Entities;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace API.Middlewares;

public class AuthMiddleware(IConfiguration configuration) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // configure issuer
        string issuer = configuration["ISSUER"] ?? throw new ArgumentNullException("Issuer is not configured");
        Console.WriteLine(issuer);
        string? authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.Contains("Bearer "))
        {
            await next.Invoke(context);
            return;
        }
        // get token
        authHeader = authHeader.Replace("Bearer ", "");
        JwtSecurityTokenHandler? handler = new();
        JwtSecurityToken? jwtSecurityToken = handler.ReadJwtToken(authHeader);
        IEnumerable<Claim>? claims = jwtSecurityToken.Claims;
        if (!claims.Any(c => c.Issuer.Equals(issuer, StringComparison.InvariantCultureIgnoreCase)))
        {
            await next.Invoke(context);
            return;
        }

        // check user
        string? id =
            claims
                .FirstOrDefault(c =>
                    c.Type.Equals("sub", StringComparison.InvariantCultureIgnoreCase)
                )
                ?.Value ?? string.Empty;
        if (!Guid.TryParse(id, out Guid userId))
        {
            return;
        }
        IAppDBContext dbContext = context.RequestServices.GetRequiredService<IAppDBContext>();
        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return;
        }
        CurrentUser currentUser = context.RequestServices.GetRequiredService<CurrentUser>();
        currentUser.SetUser(user);
        await next.Invoke(context);
    }
}
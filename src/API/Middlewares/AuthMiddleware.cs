using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UseCases.DTOs;

namespace API.Middlewares;

public class AuthMiddleware(IConfiguration configuration): IMiddleware
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
        if(!IsValidJwt(authHeader))
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
        CurrentUser currentUser = context.RequestServices.GetRequiredService<CurrentUser>();
        await next.Invoke(context);
    }

    private bool IsValidJwt(string token)
{
    var handler = new JwtSecurityTokenHandler();
    return handler.CanReadToken(token);
}
}
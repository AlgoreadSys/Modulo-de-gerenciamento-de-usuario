using Microsoft.AspNetCore.Http;

namespace DotNet.Docker.Helpers;

public static class AuthHelper
{
    public static async Task<bool> IsUserLoggedIn(HttpContext httpContext, Supabase.Client client)
    {
        // Obtém o token JWT do cabeçalho Authorization
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return false;
        }

        var jwtToken = authHeader.Substring("Bearer ".Length).Trim();

        // Verifica o token com o cliente Supabase
        var sessionResponse = await client.Auth.GetUser(jwtToken);
        return sessionResponse != null;
    }
}
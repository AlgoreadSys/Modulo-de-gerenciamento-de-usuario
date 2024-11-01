using Microsoft.AspNetCore.Http;

namespace DotNet.Docker.Helpers;

public static class AuthHelper
{
    public static async Task<bool> IsUserLoggedIn(HttpContext httpContext, Supabase.Client client)
    {
        // Verifica se o cabeçalho Authorization existe
        if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return false;
        }

        // Verifica se o valor começa com "Bearer "
        var bearerToken = authHeader.ToString();
        if (string.IsNullOrEmpty(bearerToken) || !bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Extrai o token JWT
        var jwtToken = bearerToken.Substring("Bearer ".Length).Trim();

        // Verifica o token com o cliente Supabase
        try
        {
            var sessionResponse = await client.Auth.GetUser(jwtToken);

            // Aqui inspecionamos o conteúdo da resposta
            if (sessionResponse != null)
            {
                Console.WriteLine($"Resposta do Supabase: {sessionResponse}");
            }
            else
            {
                Console.WriteLine("sessionResponse é null");
            }

            // Retorna verdadeiro se a resposta contiver um usuário válido (ajuste conforme necessário)
            return sessionResponse != null;
        }
        catch (Exception ex)
        {
            // Trate exceções, como token expirado ou erro na requisição
            Console.WriteLine($"Erro na verificação do token: {ex.Message}");
            return false;
        }
    }
}
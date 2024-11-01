using DotNet.Docker.Properties.Model;

namespace DotNet.Docker;

using Supabase;
using System.Threading.Tasks;

public class SupabaseTest
{
    private readonly Supabase.Client _client;

    public SupabaseTest(Supabase.Client client)
    {
        _client = client;
    }

    public async Task TestSupabaseConnectionAsync()
    {
        try
        {
            // Substitua "TabelaUserTest" pelo nome da sua tabela no Supabase
            var table = _client.From<User>();

            // Execute uma consulta simples
            var response = await table
                .Select("*");
                

            // Verifique a resposta
            if (response.StatusCode == 200)
            {
                Console.WriteLine("Supabase Client está funcionando corretamente.");
                foreach (var user in response.Models)
                {
                    Console.WriteLine($"Name: {user.Name}, Email: {user.Email}");
                }
            }
            else
            {
                Console.WriteLine($"Erro ao se conectar com Supabase: {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exceção: {ex.Message}");
        }
    }
}

using System.Net.Http.Json;
using DotNet.Docker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Supabase;
using Xunit;

namespace DotNet.Docker;

public class UserTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            var currentDir = Directory.GetCurrentDirectory();
            builder.UseContentRoot(Path.Combine(currentDir, @"..\..\..\App")); // Ajuste o caminho conforme necessário
        });


        // Configuração do Supabase Client para o banco principal
        const string url = "https://eabruaevckpvepqqqmfv.supabase.co";
        const string key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVhYnJ1YWV2Y2twdmVwcXFxbWZ2Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3MjU4MjczOTgsImV4cCI6MjA0MTQwMzM5OH0.Eczsx_ij7fybFr3cZFBP0jtmmxbZMZYwP-FeuBjciyE";
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };
        var supabaseClient = new Client(url, key, options);
        supabaseClient.InitializeAsync().Wait();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var newUser = new User
        {
            Name = "John Doe",
            ProfileName = "john_doe",
            BirthData = new DateTime(1990, 1, 1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/UserCreat/1", newUser);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("1", responseString); // Verifica se o ID retornado é correto
    }

    [Fact]
    public async Task ModifyUser_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var updateUser = new User
        {
            Name = "Jane Doe",
            ProfileName = "jane_doe",
            BirthData = new DateTime(1990, 1, 1)
        };

        // Act
        var response = await client.PutAsJsonAsync("/UserModify/1", updateUser);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("1", responseString); // Verifica se o ID retornado é correto
    }

    [Fact]
    public async Task ModifyUserEmail_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var newEmail = "newemail@example.com";

        // Act
        var response = await client.PutAsync($"/UserModifyEmail/1/{Uri.EscapeDataString(newEmail)}", null); // Passa null para o corpo

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("1", responseString); // Verifica se o ID retornado é correto
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNet.Docker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace DotNet.Docker.TestProject1
{
    public class UserApiTests
    {
        private readonly HttpClient _client;

        public UserApiTests()
        {
            // Configure o servidor de teste com seu Startup/Program.cs
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Program>(); // Use o seu Startup ou Program como base

            var testServer = new TestServer(webHostBuilder);
            _client = testServer.CreateClient(); // Cria o HttpClient para fazer requisições
        }

        private void SetAuthorizationHeader(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private string GetJwtToken()
        {
            // Aqui você pode adicionar lógica para recuperar ou gerar um novo token JWT
            return "eyJhbGciOiJIUzI1NiIsImtpZCI6IkV2RWRGUm5Da1NJM25yakEiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2VhYnJ1YWV2Y2twdmVwcXFxbWZ2LnN1cGFiYXNlLmNvL2F1dGgvdjEiLCJzdWIiOiI5MDRmNDJjYy03M2MzLTQ3ZWQtODdlNi01MDE3MzBlMTBkZjIiLCJhdWQiOiJhdXRoZW50aWNhdGVkIiwiZXhwIjoxNzI4OTEyNjIwLCJpYXQiOjE3Mjg5MDkwMjAsImVtYWlsIjoiZXhhbXBsZUB0ZXN0Mi5jb20iLCJwaG9uZSI6IiIsImFwcF9tZXRhZGF0YSI6eyJwcm92aWRlciI6ImVtYWlsIiwicHJvdmlkZXJzIjpbImVtYWlsIl19LCJ1c2VyX21ldGFkYXRhIjp7ImVtYWlsIjoiZXhhbXBsZUB0ZXN0Mi5jb20iLCJlbWFpbF92ZXJpZmllZCI6ZmFsc2UsInBob25lX3ZlcmlmaWVkIjpmYWxzZSwic3ViIjoiOTA0ZjQyY2MtNzNjMy00N2VkLTg3ZTYtNTAxNzMwZTEwZGYyIn0sInJvbGUiOiJhdXRoZW50aWNhdGVkIiwiYWFsIjoiYWFsMSIsImFtciI6W3sibWV0aG9kIjoicGFzc3dvcmQiLCJ0aW1lc3RhbXAiOjE3Mjg5MDkwMjB9XSwic2Vzc2lvbl9pZCI6IjQ0ZDZlNzhlLWVkZWEtNDcxMi1hNWQzLTdlYjQ2YTdlYmQ0ZCIsImlzX2Fub255bW91cyI6ZmFsc2V9.DfC-ghPPXmOeeiyP0HYIPdFy33hd2k0fTisz6rpN5V4";
        }

        [Fact]
        public async Task UserCreate_ShouldReturnOk_WhenUserIsCreated()
        {
            // Arrange
            var user = new User
            {
                Name = "Test User",
                ProfileName = "test_user",
                BirthDate = DateTime.UtcNow.AddYears(-20)
            };

            var token = GetJwtToken();
            SetAuthorizationHeader(token);

            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/UserCreate", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(responseString)); // Verifica se a resposta não está vazia
        }

        [Fact]
        public async Task UserModify_ShouldReturnOk_WhenUserIsModified()
        {
            // Arrange
            var user = new User
            {
                Name = "Updated User",
                ProfileName = "updated_user"
            };

            var token = GetJwtToken();
            SetAuthorizationHeader(token);

            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/UserModify", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(responseString)); // Verifica se a resposta não está vazia
        }
    }
}

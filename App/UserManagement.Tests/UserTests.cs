using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace DotNet.Docker.UserManagement.Tests;

public class UserTests
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri("http://localhost:5001") };
    private const string Token = "eyJhbGciOiJIUzI1NiIsImtpZCI6IkV2RWRGUm5Da1NJM25yakEiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2VhYnJ1YWV2Y2twdmVwcXFxbWZ2LnN1cGFiYXNlLmNvL2F1dGgvdjEiLCJzdWIiOiI5MDRmNDJjYy03M2MzLTQ3ZWQtODdlNi01MDE3MzBlMTBkZjIiLCJhdWQiOiJhdXRoZW50aWNhdGVkIiwiZXhwIjoxNzI4OTEyNjIwLCJpYXQiOjE3Mjg5MDkwMjAsImVtYWlsIjoiZXhhbXBsZUB0ZXN0Mi5jb20iLCJwaG9uZSI6IiIsImFwcF9tZXRhZGF0YSI6eyJwcm92aWRlciI6ImVtYWlsIiwicHJvdmlkZXJzIjpbImVtYWlsIl19LCJ1c2VyX21ldGFkYXRhIjp7ImVtYWxlIjoiZXhhbXBsZUB0ZXN0Mi5jb20iLCJlbWFpbF92ZXJpZmllZCI6ZmFsc2UsInBob25lX3ZlcmlmaWVkIjpmYWxzZSwic3ViIjoiOTA0ZjQyY2MtNzNjMy00N2VkLTg3ZTYtNTAxNzMwZTEwZGYyIn0sInJvbGUiOiJhdXRoZW50aWNhdGVkIiwiYWFsIjoiYWFsMSIsImFtciI6W3sibWV0aG9kIjoicGFzc3dvcmQiLCJ0aW1lc3RhbXAiOjE3Mjg5MDkwMjB9XSwic2Vzc2lvbl9pZCI6IjQ0ZDZlNzhlLWVkZWEtNDcxMi1hNWQzLTdlYjQ2YTdlYmQ0ZCIsImlzX2Fub255bW91cyI6ZmFsc2V9.DfC-ghPPXmOeeiyP0HYIPdFy33hd2k0fTisz6rpN5V4";
    
    [Fact]
    public async Task ModifyUser_ReturnsSuccess_WhenUserIsModified()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        var userToUpdate = new
        {
            Name = "João Souza",
            ProfileName = "Pedro da pop",
            BirthDate = "2020-12-03"
        };

        var content = new StringContent(JsonConvert.SerializeObject(userToUpdate), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/UserModify", content);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica se a resposta é 2xx
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Usuário modificado com sucesso", responseBody); // Ajuste conforme sua resposta esperada
    }

    [Fact]
    public async Task ModifyUser_ReturnsSuccess_WhenOnlyNameIsModified()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        var userToUpdate = new
        {
            Name = "Maria Silva",
            ProfileName = (string)null!, // Não modifica o nome de perfil
            BirthDate = (string)null! // Não modifica a data de nascimento
        };

        var content = new StringContent(JsonConvert.SerializeObject(userToUpdate), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/UserModify", content);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica se a resposta é 2xx
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Usuário modificado com sucesso", responseBody); // Ajuste conforme sua resposta esperada
    }

    [Fact]
    public async Task ModifyUser_ReturnsSuccess_WhenNoFieldsAreModified()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        var userToUpdate = new
        {
            Name = (string)null!, // Não modifica nenhum campo
            ProfileName = (string)null!,
            BirthDate = (string)null!
        };

        var content = new StringContent(JsonConvert.SerializeObject(userToUpdate), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/UserModify", content);

        // Assert
        response.EnsureSuccessStatusCode(); // Verifica se a resposta é 2xx
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Usuário modificado com sucesso", responseBody); // Ajuste conforme sua resposta esperada
    }

  
    [Fact]
    public async Task ModifyUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        // Use um token válido para um usuário que existe, mas que não está presente no banco de dados.
        const string nonExistentUserIdToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJub25FeGlzdGVudFVzZXIiLCJleHBpcmVkQXQiOiIwMjQtMTEtMDEgMTI6MzA6MDAuMDAwMDAwWiIsInVzZXJfbmFtZSI6ImZpcHRpb3VzX3VzZXIiLCJpYXQiOjE2Mzc0NjI1MTgsImVtYWlsIjoidGVzdC5pbmdAbWFpbC5jb20ifQ.Zm9vYmFyYmFyYmFyYmFyYmFyYmFyYmFyYmFyYmFy";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonExistentUserIdToken);

        var userToUpdate = new
        {
            Name = "Inexistente User",
            ProfileName = "N/A",
            BirthDate = "2020-12-03"
        };

        var content = new StringContent(JsonConvert.SerializeObject(userToUpdate), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/UserModify", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode); // Verifica se a resposta é 404
    }


}
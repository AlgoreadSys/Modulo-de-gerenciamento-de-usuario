using DotNet.Docker.Helpers;
using DotNet.Docker.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o serviço Swagger ao container de dependências
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registra o Supabase.Client como Singleton
const string url = "https://eabruaevckpvepqqqmfv.supabase.co";
const string key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVhYnJ1YWV2Y2twdmVwcXFxbWZ2Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3MjU4MjczOTgsImV4cCI6MjA0MTQwMzM5OH0.Eczsx_ij7fybFr3cZFBP0jtmmxbZMZYwP-FeuBjciyE";
var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true,
};

var supabaseClient = new Supabase.Client(url, key, options);
await supabaseClient.InitializeAsync();
builder.Services.AddSingleton(supabaseClient);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // Middleware Swagger
    app.UseSwaggerUI();      // Interface do Swagger UI
}

app.MapPost("/UserCreat/{id:long}", async (long id, User user, Supabase.Client client,HttpContext httpContext) =>
{
    if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    {
        return Results.Unauthorized();
    }
    
    var userUpdate = new User
    {
        Name = user.Name,
        ProfileName = user.ProfileName,
        BirthData = user.BirthData
    };

    try
    {
        var existingUser = await client
            .From<User>()
            .Where(userId => userId.Id == id)
            .Single();

        if (existingUser == null)
        {
            return Results.NotFound($"Usuário com o ID {id} não encontrado.");
        }
        
        var response = await client
            .From<User>()
            .Where(userId => user.Id == id)
            .Set(userSave => userSave.Name!, userUpdate.Name)
            .Set(userSave => userSave.ProfileName!, userUpdate.ProfileName)
            .Set(userSave => userSave.BirthData!, userUpdate.BirthData)
            .Update();
    
        var updatedUser = response.Models.First();
        return Results.Ok(updatedUser.Id);
    }
    catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        return Results.BadRequest($"Erro ao atualizar o usuário: {ex.Message}");
    }

});

app.MapPut("/UserModify/{id:long}", async (long id,User user, Supabase.Client client, HttpContext httpContext) =>
{
    // Verifica se o usuário está logado
    // if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    // {
    //     return Results.Unauthorized();
    // }
    
    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Id == id)
        .Get();

    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        return Results.NotFound("User not found.");
    }
    
    try
    {
        // Atualiza o usuário no banco de dados
        var response = await client
            .From<User>()
            .Where(userBd => userBd.Id == id)
            .Set(userSave => userSave.Name!,user.Name ?? existingUser.Name)
            .Set(userSave => userSave.ProfileName!, user.ProfileName ?? existingUser.ProfileName)
            .Set(userSave => userSave.BirthData!, user.BirthData ?? existingUser.BirthData)
            .Update();
        // Verifica se algum usuário foi atualizado
        var updatedUser = response.Models.FirstOrDefault();
        return updatedUser == null ? Results.NotFound("User not found.") : Results.Ok(new { Id = updatedUser.Id });
    }
    catch (Exception ex)
    {
        // Trata erros e retorna uma mensagem de erro
        return Results.Problem($"Error updating user: {ex.Message}");
    }
});

app.MapPut("/UserModifyEmail/{id:long}", async (long id,string email, Supabase.Client client, HttpContext httpContext) =>
{
    // Verifica se o usuário está logado
    // if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    // {
    //     return Results.Unauthorized();
    // }
    
    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Id == id)
        .Get();

    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        return Results.NotFound("User not found.");
    }
    
    try
    {
        // Atualiza o usuário no banco de dados
        var response = await client
            .From<User>()
            .Set(userSave => userSave.Email!, email ?? existingUser.Email)
            .Update();
        // Verifica se algum usuário foi atualizado
        var updatedUser = response.Models.FirstOrDefault();
        return updatedUser == null ? Results.NotFound("User not found.") : Results.Ok(new { Id = updatedUser.Id });
    }
    catch (Exception ex)
    {
        // Trata erros e retorna uma mensagem de erro
        return Results.Problem($"Error updating user email: {ex.Message}");
    }
});


app.UseHttpsRedirection();
app.Run();
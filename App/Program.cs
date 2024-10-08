﻿using DotNet.Docker.Helpers;
using DotNet.Docker.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

var supabaseClient = new Client(url, key, options);
await supabaseClient.InitializeAsync();
builder.Services.AddSingleton(supabaseClient);

builder.Services.AddCors(corsOptions =>
{
    corsOptions.AddPolicy("AllowAllOrigins",
        corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAllOrigins");  // Teste com qualquer origem permitida

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/UserCreate", async (User user, Client client,HttpContext httpContext) =>
{
    if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    {
        return Results.Unauthorized();
    }

    var userUpdate = new User
    {
        Name = user.Name,
        ProfileName = user.ProfileName,
        BirthDate = user.BirthDate,
        Auth_user_id = Guid.Parse(supabaseClient.Auth.CurrentUser?.Id!)
    };

    try
    {
        var response = await client
            .From<User>()
            .Insert(userUpdate);
        
        var updatedUser = response.Models.First();
        return Results.Ok(updatedUser.Id);
    }
    catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        return Results.BadRequest($"Error creating user: {ex.Message}");
    }

});

app.MapPut("/UserModify", async (User user, Client client, HttpContext httpContext) =>
{
    // Verifica se o usuário está logado
    if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    {
        return Results.Unauthorized();
    }

    // Obtém o ID do usuário logado a partir da autenticação do Supabase
    var id = Guid.Parse(supabaseClient.Auth.CurrentUser?.Id!);

    // Busca o usuário no banco de dados com base no ID autenticado
    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == id)
        .Get();

    // Verifica se o usuário existe
    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        return Results.NotFound("User not found.");
    }

    try
    {
        // Atualiza o usuário no banco de dados com os novos valores, mantendo os atuais se os novos forem nulos
        var response = await client
            .From<User>()
            .Where(userBd => userBd.Auth_user_id == id)
            .Set(userSave => userSave.Name!, user.Name ?? existingUser.Name)  // Atualiza o nome se não for nulo
            .Set(userSave => userSave.ProfileName!, user.ProfileName ?? existingUser.ProfileName)  // Atualiza o nome de perfil se não for nulo
            .Set(userSave => userSave.BirthDate!, user.BirthDate ?? existingUser.BirthDate)  // Atualiza a data de nascimento se não for nula
            .Update();

        // Verifica se algum usuário foi atualizado
        var updatedUser = response.Models.FirstOrDefault();
        
        // Retorna o resultado com sucesso se o usuário foi atualizado
        return updatedUser == null ? Results.NotFound("User not found.") : Results.Ok(new {updatedUser.Id });
    }
    catch (Exception ex)
    {
        // Trata erros e retorna uma mensagem de erro
        return Results.Problem($"Error updating user: {ex.Message}");
    }
});


app.MapPut("/FollowUser/{followId:long}", async (long followId, Client client, HttpContext httpContext) =>
{
    // Verifica se o usuário está logado
    if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    {
        return Results.Unauthorized();
    }

    // Obtenha o usuário atual
    var id = Guid.Parse(supabaseClient.Auth.CurrentUser?.Id!);

    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == id)
        .Get();

    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        return Results.NotFound("User not found.");
    }

    // Obtenha o usuário que será seguido
    var followUserResponse = await client
        .From<User>()
        .Where(userBd => userBd.Id == followId)
        .Get();

    var followUser = followUserResponse.Models.FirstOrDefault();
    if (followUser == null)
    {
        return Results.NotFound("User to follow not found.");
    }

    // Obtenha a lista de quem o usuário atual segue
    var followingList = existingUser.FollowingList ?? new List<long>(); // Verifica se a lista não é nula

    // Verifica se já está seguindo o usuário
    if (followingList.Contains(followId))
    {
        return Results.BadRequest("You are already following this user.");
    }

    // Adiciona o ID do usuário que será seguido à lista
    followingList.Add(followId);

    try
    {
        // Atualiza a lista de quem o usuário atual está seguindo
        await client
            .From<User>()
            .Where(userBd => userBd.Auth_user_id == id)
            .Set(userSave => userSave.FollowingList!, followingList)
            .Update();

        return Results.Ok("User followed successfully.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error following user: {ex.Message}");
    }
});

app.MapPut("/UnfollowUser/{unfollowId:long}", async (long unfollowId, Client client, HttpContext httpContext) =>
{
    // Verifica se o usuário está logado
    if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    {
        return Results.Unauthorized();
    }

    // Obtenha o usuário atual
    var id = Guid.Parse(supabaseClient.Auth.CurrentUser?.Id!);
    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == id)
        .Get();

    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        return Results.NotFound("User not found.");
    }

    // Obtenha a lista de quem o usuário segue
    var followingList = existingUser.FollowingList;

    if (followingList != null && !followingList.Contains(unfollowId))
    {
        return Results.BadRequest("User is not following this person.");
    }

    // Remove o usuário da lista de seguidos
    followingList?.Remove(unfollowId);

    try
    {
        // Atualiza a lista de seguidos do usuário
        await client
            .From<User>()
            .Where(userBd => userBd.Auth_user_id == id)
            .Set(userSave => userSave.FollowingList!, followingList)
            .Update();

        return Results.Ok("User unfollowed successfully.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error unfollowing user: {ex.Message}");
    }
});

app.MapPost("/ReportUser", async (ReportBody reportBody, Client client) =>
{
    // Extrai o reportingUserId e o reason do corpo da requisição
    var reportedUserId = reportBody.reportedUserId;
    var reason = reportBody.Reason;
    
    var reportingUserId = Guid.Parse(supabaseClient.Auth.CurrentUser?.Id!);
    
    // Verifica se o usuário que está sendo denunciado existe
    var reportedUserResponse = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == reportedUserId)
        .Get();

    var reportedUser = reportedUserResponse.Models.FirstOrDefault();
    if (reportedUser == null)
    {
        return Results.NotFound($"Reported user with ID {reportedUserId} not found.");
    }

    // Verifica se o usuário que está denunciando existe
    var reportingUserResponse = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == reportingUserId)
        .Get();

    var reportingUser = reportingUserResponse.Models.FirstOrDefault();
    if (reportingUser == null)
    {
        return Results.NotFound($"Reporting user with ID {reportingUserId} not found.");
    }

    // Cria a denúncia
    var report = new Report
    {
        ReportedUserId = reportedUserId,
        ReportingUserId = reportingUserId,
        Reason = reason
    };

    try
    {
        // Insere a denúncia no banco de dados
        await client
            .From<Report>()
            .Insert(report);
        return Results.Ok("User reported successfully.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error reporting user: {ex.Message}");
    }
});

app.UseHttpsRedirection();
app.Run();
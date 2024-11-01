using System.IdentityModel.Tokens.Jwt;
using DotNet.Docker.Helpers;
using DotNet.Docker.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

app.MapPost("/UserCreate", async (User user, Client client, HttpContext httpContext) =>
{
    // Obtém o token do cabeçalho Authorization
    if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
    {
        return Results.BadRequest("Cabeçalho 'Authorization' não fornecido.");
    }

    // O formato do cabeçalho deve ser "Bearer <token>"
    var token = authorizationHeader.ToString().Replace("Bearer ", "");
    var handler = new JwtSecurityTokenHandler();
    Guid userId;

    try
    {
        // Decodifica o token JWT
        if (handler.ReadToken(token) is JwtSecurityToken jsonToken)
        {
            // Acessa o ID do usuário dentro do token JWT e converte para Guid
            var userIdString = jsonToken.Claims.First(claim => claim.Type == "sub").Value;
            if (!Guid.TryParse(userIdString, out userId))
            {
                return Results.BadRequest("O ID do usuário não está no formato correto.");
            }
        }
        else
        {
            return Results.BadRequest("Token inválido.");
        }
    }
    catch (Exception ex)
    {
        // Em caso de erro na decodificação
        return Results.BadRequest($"Erro ao processar o token: {ex.Message}");
    }

    // Cria um novo objeto de usuário para inserção
    var userUpdate = new User
    {
        Name = user.Name,
        ProfileName = user.ProfileName,
        BirthDate = user.BirthDate,
        Auth_user_id = userId // Usa o ID do usuário autenticado
    };

    try
    {
        // Insere o novo usuário no banco de dados
        var response = await client
            .From<User>()
            .Insert(userUpdate);
        
        // Obtém o usuário inserido
        var createdUser = response.Models.FirstOrDefault();
        if (createdUser == null)
        {
            return Results.BadRequest("Erro ao criar o usuário. Usuário não foi inserido.");
        }

        // Retorna o ID do novo usuário criado
        return Results.Ok(createdUser.Id);
    }
    catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        // Trata erros e retorna uma mensagem de erro específica
        return Results.BadRequest($"Erro ao criar usuário: {ex.Message}");
    }
});

app.MapPut("/UserModify", async (User user, Client client, HttpContext httpContext) =>
{
    // Obtém o token do cabeçalho Authorization
    if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
    {
        return Results.BadRequest("Cabeçalho 'Authorization' não fornecido.");
    }

    // O formato do cabeçalho deve ser "Bearer <token>"
    var token = authorizationHeader.ToString().Replace("Bearer ", "");
    var handler = new JwtSecurityTokenHandler();
    Guid userId;

    try
    {
        // Decodifica o token JWT
        if (handler.ReadToken(token) is JwtSecurityToken jsonToken)
        {
            var userIdString = jsonToken.Claims.First(claim => claim.Type == "sub").Value;
            if (!Guid.TryParse(userIdString, out userId))
            {
                return Results.BadRequest("O ID do usuário não está no formato correto.");
            }
        }
        else
        {
            return Results.BadRequest("Token inválido.");
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Erro ao processar o token: {ex.Message}");
    }

    // Busca o usuário no banco de dados com base no ID autenticado
    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == userId)
        .Get();

    // Verifica se o usuário existe
    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        // Aqui deve retornar 404 se o usuário não for encontrado
        return Results.NotFound("User not found.");
    }

    try
    {
        // Atualiza o usuário no banco de dados
        var response = await client
            .From<User>()
            .Where(userBd => userBd.Auth_user_id == userId)
            .Set(userSave => userSave.Name!, user.Name ?? existingUser.Name)
            .Set(userSave => userSave.ProfileName!, user.ProfileName ?? existingUser.ProfileName)
            .Set(userSave => userSave.BirthDate!, user.BirthDate ?? existingUser.BirthDate)
            .Update();

        var updatedUser = response.Models.FirstOrDefault();
        
        // Verifica se algum usuário foi atualizado
        return updatedUser == null ? Results.NotFound("User not found.") : Results.Ok("Usuário modificado com sucesso");
    }
    catch (Exception ex)
    {
        // Tratar erros e retornar uma mensagem de erro
        return Results.Problem($"Error updating user: {ex.Message}");
    }
});



app.MapPut("/FollowUser", async (Client client, HttpContext httpContext, [FromBody] FollowRequest request) =>
{
    // Obtém o token do cabeçalho Authorization
    if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
    {
        return Results.BadRequest("Cabeçalho 'Authorization' não fornecido.");
    }

    var token = authorizationHeader.ToString().Replace("Bearer ", "");
    var handler = new JwtSecurityTokenHandler();
    Guid userId;

    try
    {
        if (handler.ReadToken(token) is JwtSecurityToken jsonToken)
        {
            var userIdString = jsonToken.Claims.First(claim => claim.Type == "sub").Value;

            if (!Guid.TryParse(userIdString, out userId))
            {
                return Results.BadRequest("O ID do usuário não está no formato correto.");
            }
        }
        else
        {
            return Results.BadRequest("Token inválido.");
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Erro ao processar o token: {ex.Message}");
    }

    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == userId)
        .Get();

    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        return Results.NotFound("User not found.");
    }

    var followUserResponse = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == request.InteractionIdUer)
        .Get();

    var followUser = followUserResponse.Models.FirstOrDefault();
    if (followUser == null)
    {
        return Results.NotFound("User to follow not found.");
    }

    var followingList = existingUser.FollowingList ?? new List<Guid>();

    if (followingList.Contains(request.InteractionIdUer))
    {
        return Results.BadRequest("You are already following this user.");
    }

    followingList.Add(request.InteractionIdUer);

    try
    {
        await client
            .From<User>()
            .Where(userBd => userBd.Auth_user_id == userId)
            .Set(userSave => userSave.FollowingList!, followingList)
            .Update();

        return Results.Ok("User followed successfully.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error following user: {ex.Message}");
    }
});


app.MapPut("/UnfollowUser", async (Client client, HttpContext httpContext, [FromBody] FollowRequest request) =>
{
    // Obtém o token do cabeçalho Authorization
    if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
    {
        return Results.BadRequest("Cabeçalho 'Authorization' não fornecido.");
    }

    // O formato do cabeçalho deve ser "Bearer <token>"
    var token = authorizationHeader.ToString().Replace("Bearer ", "");

    // Decodifica o token JWT
    var handler = new JwtSecurityTokenHandler();
    Guid userId;

    try
    {
        // Decodifica o token JWT
        if (handler.ReadToken(token) is JwtSecurityToken jsonToken)
        {
            // Acessa o ID do usuário dentro do token JWT e converte para Guid
            var userIdString = jsonToken.Claims.First(claim => claim.Type == "sub").Value;
            if (!Guid.TryParse(userIdString, out userId))
            {
                return Results.BadRequest("O ID do usuário não está no formato correto.");
            }
        }
        else
        {
            return Results.BadRequest("Token inválido.");
        }
    }
    catch (Exception ex)
    {
        // Em caso de erro na decodificação
        return Results.BadRequest($"Erro ao processar o token: {ex.Message}");
    }

    // Obtém o usuário atual
    var userResponseInfo = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == userId) // Usa o ID extraído do token
        .Get();

    var existingUser = userResponseInfo.Models.FirstOrDefault();
    if (existingUser == null)
    {
        return Results.NotFound("User not found.");
    }

    // Obtém a lista de quem o usuário segue
    var followingList = existingUser.FollowingList ?? new List<Guid>();

    // Verifica se o usuário não está seguindo a pessoa que está tentando deixar de seguir
    if (!followingList.Contains(request.InteractionIdUer))
    {
        return Results.BadRequest("User is not following this person.");
    }
    
    // Remove o usuário da lista de seguidos
    followingList.Remove(request.InteractionIdUer);

    try
    {
        // Atualiza a lista de seguidos do usuário
        await client
            .From<User>()
            .Where(userBd => userBd.Auth_user_id == userId) // Usa o ID extraído do token
            .Set(userSave => userSave.FollowingList!, followingList)
            .Update();

        return Results.Ok("User unfollowed successfully.");
    }
    catch (Exception ex)
    {
        // Trata erros e retorna uma mensagem de erro
        return Results.Problem($"Error unfollowing user: {ex.Message}");
    }
});

app.MapPost("/ReportUser", async (Client client, HttpContext httpContext, [FromBody] ReportBody reportBody) =>
{
    // Obtém o token do cabeçalho Authorization
    if (!httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
    {
        return Results.BadRequest("Cabeçalho 'Authorization' não fornecido.");
    }

    // O formato do cabeçalho deve ser "Bearer <token>"
    var token = authorizationHeader.ToString().Replace("Bearer ", "");

    // Decodifica o token JWT
    var handler = new JwtSecurityTokenHandler();
    Guid reportingUserId;

    try
    {
        // Decodifica o token JWT
        if (handler.ReadToken(token) is JwtSecurityToken jsonToken)
        {
            // Acessa o ID do usuário dentro do token JWT e converte para Guid
            var userIdString = jsonToken.Claims.First(claim => claim.Type == "sub").Value;
            if (!Guid.TryParse(userIdString, out reportingUserId))
            {
                return Results.BadRequest("O ID do usuário não está no formato correto.");
            }
        }
        else
        {
            return Results.BadRequest("Token inválido.");
        }
    }
    catch (Exception ex)
    {
        // Em caso de erro na decodificação
        return Results.BadRequest($"Erro ao processar o token: {ex.Message}");
    }

    var reportedUserId = reportBody.reportedUserId;
    var reason = reportBody.Reason;

    // Busca o auth_user_id do usuário reportado baseado no reportedUserId enviado do front-end
    var reportedUserResponse = await client
        .From<User>()
        .Where(userBd => userBd.Id == reportedUserId)
        .Get();

    var reportedUser = reportedUserResponse.Models.FirstOrDefault();
    if (reportedUser == null)
    {
        return Results.NotFound($"Usuário com ID {reportedUserId} não encontrado.");
    }

// Acessa o Auth_user_id do usuário encontrado
    var authUserIdOfReportedUser = reportedUser.Auth_user_id;

    // Verifica se o usuário que está denunciando existe
    var reportingUserResponse = await client
        .From<User>()
        .Where(userBd => userBd.Auth_user_id == reportingUserId) // Usa o ID extraído do token
        .Get();

    var reportingUser = reportingUserResponse.Models.FirstOrDefault();
    if (reportingUser == null)
    {
        return Results.NotFound($"Reporting user with ID {reportingUserId} not found.");
    }

    // Cria a denúncia
    var report = new Report
    {
        ReportedUserId = authUserIdOfReportedUser, // Usa o auth_user_id encontrado
        ReportingUserId = reportingUserId, // Usa o ID extraído do token
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

app.MapGet("/InfoUser/{idUserInfo:int}", async (int idUserInfo, Client client) =>
{
    var userResponse = await client
        .From<User>()
        .Where(userBd => userBd.Id == idUserInfo)
        .Get();

    var userInfo = userResponse.Models.FirstOrDefault();

    if (userInfo == null)
    {
        return Results.NotFound($"Usuário com ID {idUserInfo} não encontrado.");
    }

    // Mapeia para o DTO
    var userDto = new UserDto
    {
        Name = userInfo.Name,
        ProfileName = userInfo.ProfileName,
        BirthDate = userInfo.BirthDate
    };

    return Results.Ok(userDto);
});


app.UseHttpsRedirection();
app.Run();
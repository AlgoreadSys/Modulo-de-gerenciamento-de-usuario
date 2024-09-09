using DotNet.Docker;
using DotNet.Docker.Properties.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supabase;

var builder =  WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<Supabase.Client>(provider =>
    new Supabase.Client(
        builder.Configuration["SupabaseUrl"]!,
        builder.Configuration["SupabaseKey"],
        new SupabaseOptions()
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        }
        ));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapPost("/", async (
    User user,
    Supabase.Client client) =>
{
    var userC = new User
    {
        Name = user.Name,
        Email = user.Email
    };

    var response = await client.From<User>().Insert(userC);

    var newNewsletter = response.Models.First();

    return Results.Ok(newNewsletter.Id);
});
//End points
app.MapPut("/UserModify", async (User user, HttpContext httpContext, Supabase.Client client) =>
{
    // // Obtém o token JWT do cabeçalho Authorization
    // var authHeader = httpContext.Request.Headers["Authorization"].ToString();
    // if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
    // {
    //     return Results.Unauthorized();
    // }
    //
    // var jwtToken = authHeader.Substring("Bearer ".Length).Trim();
    //
    // // Verifica o token com o cliente Supabase
    // var sessionResponse = await client.Auth.GetUser(jwtToken);
    // if (sessionResponse == null)
    // {
    //     return Results.Unauthorized();
    // }

    // Agora você tem o usuário autenticado e pode proceder com a atualização
    var response = await client
        .From<User>()
        .Where(userBd => userBd.Id == user.Id)
        .Update(user);

    var newUser = response.Models.FirstOrDefault();

    return Results.Ok(newUser!.Id);
    //return newUser is null ? Results.NotFound("User not found.") : Results.Ok(newUser.Id);
});


app.UseHttpsRedirection();

app.Run();



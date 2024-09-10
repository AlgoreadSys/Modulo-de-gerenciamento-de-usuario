using DotNet.Docker.Helpers;
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
var url = "https://srawzoabyjwblgfgoxxj.supabase.co";
var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNyYXd6b2FieWp3YmxnZmdveHhqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MjU4MzMyMTksImV4cCI6MjA0MTQwOTIxOX0.uHgcP8TDQeyNHh_Rpp_W0iQZbwzo7WWKZUjkqEre80Y";
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

app.MapPost("/UserCreat", async (User user, Supabase.Client client,HttpContext httpContext) =>
{
    if (!await AuthHelper.IsUserLoggedIn(httpContext, client))
    {
        return Results.Unauthorized();
    }
    
    var userC = new User
    {
        Name = user.Name,
        ProfileName = user.ProfileName,
        BirthData = user.BirthData
    };

    var response = await client.From<User>().Insert(userC);
    var newNewsletter = response.Models.First();

    return Results.Ok(newNewsletter.Id);
});

app.MapPut("/UserModify", async (User user, Supabase.Client client) =>
{
    var response = await client
        .From<User>()
        .Where(userBd => userBd.Id == user.Id)
        .Update(user);

    var newUser = response.Models.FirstOrDefault();
    return Results.Ok(newUser!.Id);
});

app.UseHttpsRedirection();
app.Run();
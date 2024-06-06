using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Load config from appsettings.json and user secrets in dev
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Configure Cosmos DB client
builder.Services.AddSingleton<CosmosDbService>(options =>
{
    var configuration = builder.Configuration.GetSection("CosmosDB");
    string account = configuration["Account"];
    string key = configuration["Key"];
    string databaseName = configuration["DatabaseName"];
    string containerName = configuration["ContainerName"];
    CosmosClient client = new CosmosClient(account, key);
    return new CosmosDbService(client, databaseName, containerName);
});

// CORS Config
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();
app.UseCors();

var videogamesBaseUrl = app.MapGroup("/videogames");

videogamesBaseUrl.MapGet("/", async (CosmosDbService dbService) =>
{
    var videoGames = await dbService.GetVideoGamesAsync("SELECT * FROM c");
    return Results.Ok(videoGames);
});

videogamesBaseUrl.MapGet("/complete", async (CosmosDbService dbService) =>
{
    var videoGames = await dbService.GetVideoGamesAsync("SELECT * FROM c WHERE c.IsComplete = true");
    return Results.Ok(videoGames);
});

videogamesBaseUrl.MapGet("/{id}", async (string id, CosmosDbService dbService) =>
{
    var videoGame = await dbService.GetVideoGameAsync(id);
    return videoGame is not null ? Results.Ok(videoGame) : Results.NotFound();
});

videogamesBaseUrl.MapPost("/", async (VideoGame videoGame, CosmosDbService dbService) =>
{
    await dbService.AddVideoGameAsync(videoGame);
    return Results.Created($"/videogames/{videoGame.Id}", videoGame);
});

videogamesBaseUrl.MapPut("/{id}", async (string id, VideoGame inputVideoGame, CosmosDbService dbService) =>
{
    var videoGame = await dbService.GetVideoGameAsync(id);
    if (videoGame is null) return Results.NotFound();

    videoGame.Title = inputVideoGame.Title;
    videoGame.Studio = inputVideoGame.Studio;
    videoGame.ReleaseYear = inputVideoGame.ReleaseYear;
    videoGame.IsComplete = inputVideoGame.IsComplete;

    await dbService.UpdateVideoGameAsync(id, videoGame);

    return Results.NoContent();
});

videogamesBaseUrl.MapDelete("/{id}", async (string id, CosmosDbService dbService) =>
{
    var videoGame = await dbService.GetVideoGameAsync(id);
    if (videoGame is null) return Results.NotFound();

    await dbService.DeleteVideoGameAsync(id);
    return Results.NoContent();
});

app.Run();

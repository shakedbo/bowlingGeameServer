using BowlingGame.API.Middleware;
using BowlingGame.API.Repositories;
using BowlingGame.API.Repositories.Interfaces;
using BowlingGame.API.Services;
using BowlingGame.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register Repository
builder.Services.AddScoped<IGameRepository, GameRepository>();

// Register Service
builder.Services.AddScoped<IBowlingService, BowlingService>();

var app = builder.Build();

// Configure the HTTP request pipeline

// Global exception handling - must be first in pipeline
app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.MapControllers();

app.Run();

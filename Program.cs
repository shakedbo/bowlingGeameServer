using BowlingGame.API.Middleware;
using BowlingGame.API.Middleware.ExceptionMapping;
using BowlingGame.API.Repositories;
using BowlingGame.API.Repositories.Interfaces;
using BowlingGame.API.Services;
using BowlingGame.API.Services.Interfaces;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bowling Game API",
        Version = "v1",
        Description = "A production-grade Bowling Game Backend API"
    });
});

// Register Exception Mapping
builder.Services.AddExceptionMapping();

// Register Repositories
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IRollRepository, RollRepository>();
builder.Services.AddScoped<IApiLogRepository, ApiLogRepository>();

// Register Service
builder.Services.AddScoped<IBowlingService, BowlingService>();

var app = builder.Build();

// Configure the HTTP request pipeline

// Request/Response logging - captures all requests including exceptions
app.UseRequestLogging();

// Global exception handling
app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bowling Game API v1");
        options.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

app.UseRouting();
app.MapControllers();

app.Run();

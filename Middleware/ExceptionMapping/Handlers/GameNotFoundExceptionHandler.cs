using System.Net;
using BowlingGame.API.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BowlingGame.API.Middleware.ExceptionMapping.Handlers;

public class GameNotFoundExceptionHandler : IExceptionHandler
{
    public Type ExceptionType => typeof(GameNotFoundException);

    public (int StatusCode, ProblemDetails ProblemDetails) Handle(Exception exception, HttpContext context)
    {
        var ex = (GameNotFoundException)exception;
        
        return (
            (int)HttpStatusCode.NotFound,
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Game Not Found",
                Detail = ex.Message,
                Instance = context.Request.Path,
                Extensions = { ["gameId"] = ex.GameId }
            }
        );
    }
}

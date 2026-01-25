using System.Net;
using BowlingGame.API.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BowlingGame.API.Middleware.ExceptionMapping.Handlers;

public class GameCompletedExceptionHandler : IExceptionHandler
{
    public Type ExceptionType => typeof(GameCompletedException);

    public (int StatusCode, ProblemDetails ProblemDetails) Handle(Exception exception, HttpContext context)
    {
        var ex = (GameCompletedException)exception;
        
        return (
            (int)HttpStatusCode.BadRequest,
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Game Already Completed",
                Detail = ex.Message,
                Instance = context.Request.Path,
                Extensions = { ["gameId"] = ex.GameId }
            }
        );
    }
}

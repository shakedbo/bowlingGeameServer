using System.Net;
using BowlingGame.API.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BowlingGame.API.Middleware.ExceptionMapping.Handlers;

public class InvalidRollExceptionHandler : IExceptionHandler
{
    public Type ExceptionType => typeof(InvalidRollException);

    public (int StatusCode, ProblemDetails ProblemDetails) Handle(Exception exception, HttpContext context)
    {
        var ex = (InvalidRollException)exception;
        
        return (
            (int)HttpStatusCode.BadRequest,
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Invalid Roll",
                Detail = ex.Message,
                Instance = context.Request.Path
            }
        );
    }
}

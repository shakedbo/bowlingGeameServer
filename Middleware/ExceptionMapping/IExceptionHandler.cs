using Microsoft.AspNetCore.Mvc;

namespace BowlingGame.API.Middleware.ExceptionMapping;

public interface IExceptionHandler
{
    Type ExceptionType { get; }
    (int StatusCode, ProblemDetails ProblemDetails) Handle(Exception exception, HttpContext context);
}

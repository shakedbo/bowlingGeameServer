using Microsoft.AspNetCore.Mvc;

namespace BowlingGame.API.Middleware.ExceptionMapping;

public interface IExceptionMapper
{
    (int StatusCode, ProblemDetails ProblemDetails) Map(Exception exception, HttpContext context);
}

using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace BowlingGame.API.Middleware.ExceptionMapping;

public class ExceptionMapper : IExceptionMapper
{
    private readonly Dictionary<Type, IExceptionHandler> _handlers;

    public ExceptionMapper(IEnumerable<IExceptionHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.ExceptionType, h => h);
    }

    public (int StatusCode, ProblemDetails ProblemDetails) Map(Exception exception, HttpContext context)
    {
        var exceptionType = exception.GetType();

        if (_handlers.TryGetValue(exceptionType, out var handler))
        {
            return handler.Handle(exception, context);
        }

        return CreateDefaultResponse(context);
    }

    private static (int StatusCode, ProblemDetails ProblemDetails) CreateDefaultResponse(HttpContext context)
    {
        return (
            (int)HttpStatusCode.InternalServerError,
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = context.Request.Path
            }
        );
    }
}

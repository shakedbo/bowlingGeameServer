using BowlingGame.API.Middleware.ExceptionMapping.Handlers;

namespace BowlingGame.API.Middleware.ExceptionMapping;

public static class ExceptionMappingServiceExtensions
{
    public static IServiceCollection AddExceptionMapping(this IServiceCollection services)
    {
        services.AddSingleton<IExceptionHandler, GameNotFoundExceptionHandler>();
        services.AddSingleton<IExceptionHandler, GameCompletedExceptionHandler>();
        services.AddSingleton<IExceptionHandler, InvalidRollExceptionHandler>();
        services.AddSingleton<IExceptionMapper, ExceptionMapper>();

        return services;
    }
}

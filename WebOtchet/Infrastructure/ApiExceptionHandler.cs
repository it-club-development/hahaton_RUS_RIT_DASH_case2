using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebOtchet.Infrastructure;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (status, title, detail) = exception switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Доступ запрещён",
                exception.Message
            ),
            UserHasIncidentsException => (
                StatusCodes.Status409Conflict,
                "Учётная запись связана с авариями",
                exception.Message
            ),
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Ошибка проверки данных",
                exception.Message
            ),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Некорректный запрос",
                exception.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "При обработке запроса произошла ошибка."
            )
        };

        if (status >= 500)
            _logger.LogError(exception, "Unhandled API exception");

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken
        );

        return true;
    }
}

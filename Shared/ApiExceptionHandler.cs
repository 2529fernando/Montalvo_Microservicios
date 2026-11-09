using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Microservicios.Common;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error al procesar {Method} {Path}.",
            httpContext.Request.Method, httpContext.Request.Path);
        var sqlException = FindSqlException(exception);
        var concurrentOperation = sqlException?.Number == 1205;
        var databaseUnavailable = sqlException is not null && sqlException.Errors
            .Cast<SqlError>()
            .Any(error => error.Number is -2 or -1 or 2 or 20 or 26 or 40 or 53 or 64 or 233 or 4060
                or 18456 or 10053 or 10054 or 10060 or 10061 or 11001);

        var problem = new ProblemDetails
        {
            Status = concurrentOperation
                ? StatusCodes.Status409Conflict
                : databaseUnavailable
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError,
            Title = concurrentOperation
                ? "Otra operación modificó estos datos. Intenta nuevamente."
                : databaseUnavailable
                ? "No se puede conectar con la base de datos."
                : "No se pudo completar la operación.",
            Detail = concurrentOperation
                ? "La operación se revirtió para mantener los datos consistentes."
                : databaseUnavailable
                ? "Comprueba que SQL Server esté iniciado, la cadena de conexión y que se haya ejecutado el script SQL."
                : "Consulta el registro del servicio para identificar el error.",
            Instance = httpContext.Request.Path
        };
        httpContext.Response.StatusCode = problem.Status.Value;

        if (!await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            { HttpContext = httpContext, Exception = exception, ProblemDetails = problem }))
        {
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }

        return true;
    }

    private static SqlException? FindSqlException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is SqlException sqlException)
                return sqlException;

        return null;
    }
}

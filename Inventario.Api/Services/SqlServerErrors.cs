using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services;

internal static class SqlServerErrors
{
    public static bool EsRelacionORestriccion(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 547 };

    public static bool EsDuplicado(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

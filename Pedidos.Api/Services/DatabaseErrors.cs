using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Pedidos.Api.Services;

internal static class DatabaseErrors
{
    public static bool EsConflictoDeIntegridad(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 547 or 2601 or 2627 };
}

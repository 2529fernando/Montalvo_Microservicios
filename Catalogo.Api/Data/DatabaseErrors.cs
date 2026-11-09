using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Data;

internal static class DatabaseErrors
{
    public static bool IsConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 547 or 2601 or 2627 };
}

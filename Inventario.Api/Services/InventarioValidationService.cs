using Inventario.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services;

public record ErrorValidacionInventario(int StatusCode, string Mensaje);

public class InventarioValidationService(InventarioDBContext context, CatalogoClient catalogo)
{
    public async Task<ErrorValidacionInventario?> ValidarAsync(
        int idBodega, int idProducto, CancellationToken ct = default)
    {
        if (!await context.Bodega.AnyAsync(x => x.IdBodega == idBodega, ct))
            return new(StatusCodes.Status400BadRequest, "La bodega indicada no existe.");

        try
        {
            if (!await catalogo.ExisteProductoAsync(idProducto, ct))
                return new(StatusCodes.Status400BadRequest, "El producto indicado no existe en Catálogo.");
        }
        catch (HttpRequestException)
        {
            return new(StatusCodes.Status503ServiceUnavailable,
                "No se pudo consultar Catálogo. Intenta nuevamente cuando esté disponible.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new(StatusCodes.Status503ServiceUnavailable,
                "Catálogo no respondió a tiempo. Intenta nuevamente.");
        }

        return null;
    }
}

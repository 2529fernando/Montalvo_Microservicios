using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Data;
using Pedidos.Api.Models;
using Pedidos.Api.Services;

namespace Pedidos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DetallePedidoController(PedidosDBContext context, CatalogoClient catalogo) : ControllerBase
{
    private const decimal ImporteMaximo = 9999999999.99m;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DetallePedido>>> ObtenerTodos()
    {
        return Ok(await context.DetallePedido.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DetallePedido>> ObtenerPorId(int id)
    {
        var registro = await context.DetallePedido.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdDetallePedido == id);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<DetallePedido>> Crear(DetallePedido registro)
    {
        var error = await ValidarProductoAsync(registro.IdProducto);
        if (error is not null) return error;
        if (!PrepararImportes(registro))
            return BadRequest("El subtotal supera el límite permitido de 9999999999.99.");

        // El detalle y el total se guardan juntos, incluso con solicitudes simultáneas.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var pedido = await context.Pedido.FindAsync(registro.IdPedido);
        if (pedido is null) return BadRequest("El pedido indicado no existe.");

        registro.IdDetallePedido = 0;
        context.DetallePedido.Add(registro);
        try
        {
            await context.SaveChangesAsync();
            if (!await RecalcularTotalAsync(pedido))
                return BadRequest("El total del pedido supera el límite permitido de 9999999999.99.");
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.EsConflictoDeIntegridad(ex))
        {
            return Conflict("No se pudo guardar el detalle. Revisa sus relaciones.");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdDetallePedido }, registro);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, DetallePedido registro)
    {
        if (id != registro.IdDetallePedido)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");
        if (!await context.DetallePedido.AnyAsync(x => x.IdDetallePedido == id))
            return NotFound();

        var error = await ValidarProductoAsync(registro.IdProducto);
        if (error is not null) return error;
        if (!PrepararImportes(registro))
            return BadRequest("El subtotal supera el límite permitido de 9999999999.99.");

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actual = await context.DetallePedido.FindAsync(id);
        if (actual is null) return NotFound();
        var idPedidoAnterior = actual.IdPedido;
        var pedidos = await context.Pedido
            .Where(x => x.IdPedido == idPedidoAnterior || x.IdPedido == registro.IdPedido)
            .OrderBy(x => x.IdPedido).ToListAsync();
        if (!pedidos.Any(x => x.IdPedido == registro.IdPedido))
            return BadRequest("El pedido indicado no existe.");

        actual.IdPedido = registro.IdPedido;
        actual.IdProducto = registro.IdProducto;
        actual.Cantidad = registro.Cantidad;
        actual.PrecioUnitario = registro.PrecioUnitario;
        actual.Subtotal = registro.Subtotal;
        try
        {
            await context.SaveChangesAsync();
            foreach (var pedido in pedidos)
                if (!await RecalcularTotalAsync(pedido))
                    return BadRequest("El total del pedido supera el límite permitido de 9999999999.99.");
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.EsConflictoDeIntegridad(ex))
        {
            return Conflict("No se pudo actualizar el detalle. Revisa sus relaciones.");
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var registro = await context.DetallePedido.FindAsync(id);
        if (registro is null) return NotFound();
        var pedido = await context.Pedido.FindAsync(registro.IdPedido);
        if (pedido is null) return Conflict("El pedido asociado al detalle no existe.");

        context.DetallePedido.Remove(registro);
        try
        {
            await context.SaveChangesAsync();
            if (!await RecalcularTotalAsync(pedido))
                return Conflict("El total del pedido supera el límite permitido de 9999999999.99.");
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.EsConflictoDeIntegridad(ex))
        {
            return Conflict("No se pudo eliminar el detalle por sus relaciones.");
        }
        return NoContent();
    }

    private async Task<ObjectResult?> ValidarProductoAsync(int idProducto)
    {
        try
        {
            return await catalogo.ExisteProductoAsync(idProducto, HttpContext.RequestAborted)
                ? null : BadRequest("El producto indicado no existe en Catálogo.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "No se pudo consultar Catálogo. Intenta nuevamente cuando esté disponible.");
        }
        catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Catálogo tardó demasiado en responder. Intenta nuevamente.");
        }
    }

    private static bool PrepararImportes(DetallePedido registro)
    {
        registro.PrecioUnitario = decimal.Round(registro.PrecioUnitario, 2, MidpointRounding.AwayFromZero);
        registro.Subtotal = registro.Cantidad * registro.PrecioUnitario;
        return registro.Subtotal <= ImporteMaximo;
    }

    private async Task<bool> RecalcularTotalAsync(Pedido pedido)
    {
        var total = await context.DetallePedido.Where(x => x.IdPedido == pedido.IdPedido)
            .SumAsync(x => (decimal?)x.Subtotal) ?? 0m;
        if (total > ImporteMaximo) return false;
        pedido.Total = total;
        return true;
    }
}


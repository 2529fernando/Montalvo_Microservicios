using Pedidos.Api.Data;
using Pedidos.Api.Models;
using Pedidos.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pedidos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidoController : ControllerBase
{
    private readonly PedidosDBContext _context;

    public PedidoController(PedidosDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pedido>>> ObtenerTodos()
    {
        return Ok(await _context.Pedido.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")] 
    public async Task<ActionResult<Pedido>> ObtenerPorId(int id)
    {
        var registro = await _context.Pedido.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdPedido == id);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<Pedido>> Crear(Pedido registro)
    {
        if (!await _context.Cliente.AnyAsync(x => x.IdCliente == registro.IdCliente))
            return BadRequest("El cliente indicado no existe.");

        // SQL Server genera automáticamente el Id (IDENTITY).
        registro.IdPedido = 0;
        registro.Total = 0;
        _context.Pedido.Add(registro);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.EsConflictoDeIntegridad(ex))
        {
            return BadRequest("No se pudo guardar el registro. Revisa las relaciones y los datos.");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdPedido }, registro);
    }

    [HttpPut("{id:int}")] 
    public async Task<IActionResult> Actualizar(int id, Pedido registro)
    {
        if (id != registro.IdPedido)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        var actual = await _context.Pedido.FindAsync(id);
        if (actual is null)
            return NotFound();

        if (!await _context.Cliente.AnyAsync(x => x.IdCliente == registro.IdCliente))
            return BadRequest("El cliente indicado no existe.");

        actual.IdCliente = registro.IdCliente;
        actual.FechaPedido = registro.FechaPedido;
        actual.Estado = registro.Estado;
        // Total se mantiene como suma de detalles; no se acepta del cliente.
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.EsConflictoDeIntegridad(ex))
        {
            return BadRequest("No se pudo actualizar el registro. Revisa las relaciones y los datos.");
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")] 
    public async Task<IActionResult> Eliminar(int id)
    {
        var registro = await _context.Pedido.FindAsync(id);
        if (registro is null) return NotFound();

        if (await _context.DetallePedido.AnyAsync(x => x.IdPedido == id))
            return Conflict("El pedido tiene detalles asociados. Elimina los detalles primero.");

        _context.Pedido.Remove(registro);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.EsConflictoDeIntegridad(ex))
        {
            return Conflict("El registro tiene referencias asociadas o no puede eliminarse.");
        }
        return NoContent();
    }
}

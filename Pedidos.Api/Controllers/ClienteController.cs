using Pedidos.Api.Data;
using Pedidos.Api.Models;
using Pedidos.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pedidos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    private readonly PedidosDBContext _context;

    public ClienteController(PedidosDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> ObtenerTodos()
    {
        return Ok(await _context.Cliente.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")] 
    public async Task<ActionResult<Cliente>> ObtenerPorId(int id)
    {
        var registro = await _context.Cliente.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdCliente == id);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Crear(Cliente registro)
    {
        // SQL Server genera automáticamente el Id (IDENTITY).
        registro.IdCliente = 0;
        _context.Cliente.Add(registro);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.EsConflictoDeIntegridad(ex))
        {
            return BadRequest("No se pudo guardar el registro. Revisa las relaciones y los datos.");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdCliente }, registro);
    }

    [HttpPut("{id:int}")] 
    public async Task<IActionResult> Actualizar(int id, Cliente registro)
    {
        if (id != registro.IdCliente)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        if (!await _context.Cliente.AnyAsync(x => x.IdCliente == id))
            return NotFound();

        _context.Entry(registro).State = EntityState.Modified;
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
        var registro = await _context.Cliente.FindAsync(id);
        if (registro is null) return NotFound();

        if (await _context.Pedido.AnyAsync(x => x.IdCliente == id))
            return Conflict("El cliente tiene pedidos asociados.");

        _context.Cliente.Remove(registro);
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

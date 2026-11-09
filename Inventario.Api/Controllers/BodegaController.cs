using Inventario.Api.Data;
using Inventario.Api.Models;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BodegaController : ControllerBase
{
    private readonly InventarioDBContext _context;

    public BodegaController(InventarioDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bodega>>> ObtenerTodos(CancellationToken ct)
    {
        return Ok(await _context.Bodega.AsNoTracking().ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Bodega>> ObtenerPorId(int id, CancellationToken ct)
    {
        var registro = await _context.Bodega.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdBodega == id, ct);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<Bodega>> Crear(Bodega registro, CancellationToken ct)
    {
        registro.IdBodega = 0;
        _context.Bodega.Add(registro);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (SqlServerErrors.EsRelacionORestriccion(ex))
        {
            return BadRequest("No se pudo guardar el registro. Revisa las relaciones y los datos.");
        }
        catch (DbUpdateException ex) when (SqlServerErrors.EsDuplicado(ex))
        {
            return Conflict("Ya existe un registro con esos datos.");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdBodega }, registro);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, Bodega registro, CancellationToken ct)
    {
        if (id != registro.IdBodega)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        var existente = await _context.Bodega.FirstOrDefaultAsync(x => x.IdBodega == id, ct);
        if (existente is null)
            return NotFound();

        _context.Entry(existente).CurrentValues.SetValues(registro);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (SqlServerErrors.EsRelacionORestriccion(ex))
        {
            return BadRequest("No se pudo actualizar el registro. Revisa las relaciones y los datos.");
        }
        catch (DbUpdateException ex) when (SqlServerErrors.EsDuplicado(ex))
        {
            return Conflict("Ya existe un registro con esos datos.");
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var registro = await _context.Bodega.FirstOrDefaultAsync(x => x.IdBodega == id, ct);
        if (registro is null) return NotFound();

        if (await _context.Existencias.AnyAsync(x => x.IdBodega == id, ct)
            || await _context.MovimientosInv.AnyAsync(x => x.IdBodega == id, ct)
            || await _context.Stock.AnyAsync(x => x.IdBodega == id, ct))
            return Conflict("La bodega tiene existencias, movimientos o stock asociados.");

        _context.Bodega.Remove(registro);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (SqlServerErrors.EsRelacionORestriccion(ex))
        {
            return Conflict("El registro tiene referencias asociadas o no puede eliminarse.");
        }
        return NoContent();
    }
}

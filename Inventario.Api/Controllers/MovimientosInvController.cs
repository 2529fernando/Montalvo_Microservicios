using Inventario.Api.Data;
using Inventario.Api.Models;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovimientosInvController : ControllerBase
{
    private readonly InventarioDBContext _context;

    private readonly InventarioValidationService _validation;

    public MovimientosInvController(InventarioDBContext context, InventarioValidationService validation)
    {
        _context = context;
        _validation = validation;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovimientosInv>>> ObtenerTodos(CancellationToken ct)
    {
        return Ok(await _context.MovimientosInv.AsNoTracking().ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovimientosInv>> ObtenerPorId(int id, CancellationToken ct)
    {
        var registro = await _context.MovimientosInv.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdMovimiento == id, ct);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<MovimientosInv>> Crear(MovimientosInv registro, CancellationToken ct)
    {
        var error = await _validation.ValidarAsync(registro.IdBodega, registro.IdProducto, ct);
        if (error is not null)
            return StatusCode(error.StatusCode, error.Mensaje);

        registro.IdMovimiento = 0;
        _context.MovimientosInv.Add(registro);
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
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdMovimiento }, registro);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, MovimientosInv registro, CancellationToken ct)
    {
        if (id != registro.IdMovimiento)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        var existente = await _context.MovimientosInv.FirstOrDefaultAsync(x => x.IdMovimiento == id, ct);
        if (existente is null)
            return NotFound();

        var error = await _validation.ValidarAsync(registro.IdBodega, registro.IdProducto, ct);
        if (error is not null)
            return StatusCode(error.StatusCode, error.Mensaje);

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
        var registro = await _context.MovimientosInv.FirstOrDefaultAsync(x => x.IdMovimiento == id, ct);
        if (registro is null) return NotFound();

        _context.MovimientosInv.Remove(registro);
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

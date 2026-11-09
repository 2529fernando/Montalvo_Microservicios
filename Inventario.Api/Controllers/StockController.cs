using Inventario.Api.Data;
using Inventario.Api.Models;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly InventarioDBContext _context;

    private readonly InventarioValidationService _validation;

    public StockController(InventarioDBContext context, InventarioValidationService validation)
    {
        _context = context;
        _validation = validation;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Stock>>> ObtenerTodos(CancellationToken ct)
    {
        return Ok(await _context.Stock.AsNoTracking().ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Stock>> ObtenerPorId(int id, CancellationToken ct)
    {
        var registro = await _context.Stock.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdStock == id, ct);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<Stock>> Crear(Stock registro, CancellationToken ct)
    {
        var error = await _validation.ValidarAsync(registro.IdBodega, registro.IdProducto, ct);
        if (error is not null)
            return StatusCode(error.StatusCode, error.Mensaje);

        registro.IdStock = 0;
        registro.FechaActualizacion = DateTime.UtcNow;
        _context.Stock.Add(registro);
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
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdStock }, registro);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, Stock registro, CancellationToken ct)
    {
        if (id != registro.IdStock)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        var existente = await _context.Stock.FirstOrDefaultAsync(x => x.IdStock == id, ct);
        if (existente is null)
            return NotFound();

        var error = await _validation.ValidarAsync(registro.IdBodega, registro.IdProducto, ct);
        if (error is not null)
            return StatusCode(error.StatusCode, error.Mensaje);

        registro.FechaActualizacion = DateTime.UtcNow;
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
        var registro = await _context.Stock.FirstOrDefaultAsync(x => x.IdStock == id, ct);
        if (registro is null) return NotFound();

        _context.Stock.Remove(registro);
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

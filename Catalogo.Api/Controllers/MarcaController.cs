using Catalogo.Api.Data;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarcaController : ControllerBase
{
    private readonly CatalogoDBContext _context;

    public MarcaController(CatalogoDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Marca>>> ObtenerTodos()
    {
        return Ok(await _context.Marca.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")] 
    public async Task<ActionResult<Marca>> ObtenerPorId(int id)
    {
        var registro = await _context.Marca.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdMarca == id);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<Marca>> Crear(Marca registro)
    {
        // SQL Server genera automáticamente el Id (IDENTITY).
        registro.IdMarca = 0;
        _context.Marca.Add(registro);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsConstraintViolation(ex))
        {
            return BadRequest("No se pudo guardar el registro. Revisa las relaciones y los datos.");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdMarca }, registro);
    }

    [HttpPut("{id:int}")] 
    public async Task<IActionResult> Actualizar(int id, Marca registro)
    {
        if (id != registro.IdMarca)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        if (!await _context.Marca.AnyAsync(x => x.IdMarca == id))
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
        catch (DbUpdateException ex) when (DatabaseErrors.IsConstraintViolation(ex))
        {
            return BadRequest("No se pudo actualizar el registro. Revisa las relaciones y los datos.");
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")] 
    public async Task<IActionResult> Eliminar(int id)
    {
        var registro = await _context.Marca.FindAsync(id);
        if (registro is null) return NotFound();

        if (await _context.Productos.AnyAsync(x => x.IdMarca == id))
            return Conflict("La marca tiene productos asociados y no puede eliminarse.");

        _context.Marca.Remove(registro);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsConstraintViolation(ex))
        {
            return Conflict("El registro tiene referencias asociadas o no puede eliminarse.");
        }
        return NoContent();
    }
}

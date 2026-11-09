using Catalogo.Api.Data;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriaController : ControllerBase
{
    private readonly CatalogoDBContext _context;

    public CategoriaController(CatalogoDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Categoria>>> ObtenerTodos()
    {
        return Ok(await _context.Categoria.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")] 
    public async Task<ActionResult<Categoria>> ObtenerPorId(int id)
    {
        var registro = await _context.Categoria.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdCategoria == id);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<Categoria>> Crear(Categoria registro)
    {
        // SQL Server genera automáticamente el Id (IDENTITY).
        registro.IdCategoria = 0;
        _context.Categoria.Add(registro);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsConstraintViolation(ex))
        {
            return BadRequest("No se pudo guardar el registro. Revisa las relaciones y los datos.");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdCategoria }, registro);
    }

    [HttpPut("{id:int}")] 
    public async Task<IActionResult> Actualizar(int id, Categoria registro)
    {
        if (id != registro.IdCategoria)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        if (!await _context.Categoria.AnyAsync(x => x.IdCategoria == id))
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
        var registro = await _context.Categoria.FindAsync(id);
        if (registro is null) return NotFound();

        if (await _context.Productos.AnyAsync(x => x.IdCategoria == id))
            return Conflict("La categoría tiene productos asociados y no puede eliminarse.");

        _context.Categoria.Remove(registro);
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

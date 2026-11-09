using Catalogo.Api.Data;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly CatalogoDBContext _context;

    public ProductosController(CatalogoDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Productos>>> ObtenerTodos()
    {
        return Ok(await _context.Productos.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")] 
    public async Task<ActionResult<Productos>> ObtenerPorId(int id)
    {
        var registro = await _context.Productos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdProducto == id);
        return registro is null ? NotFound() : Ok(registro);
    }

    [HttpPost]
    public async Task<ActionResult<Productos>> Crear(Productos registro)
    {
        var error = await ValidarRelaciones(registro);
        if (error is not null) return BadRequest(error);

        // SQL Server genera automáticamente el Id (IDENTITY).
        registro.IdProducto = 0;
        registro.Precio = decimal.Round(registro.Precio, 2, MidpointRounding.AwayFromZero);
        _context.Productos.Add(registro);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsConstraintViolation(ex))
        {
            return BadRequest("No se pudo guardar el registro. Revisa las relaciones y los datos.");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = registro.IdProducto }, registro);
    }

    [HttpPut("{id:int}")] 
    public async Task<IActionResult> Actualizar(int id, Productos registro)
    {
        if (id != registro.IdProducto)
            return BadRequest("El identificador de la ruta no coincide con el del objeto.");

        if (!await _context.Productos.AnyAsync(x => x.IdProducto == id))
            return NotFound();

        var error = await ValidarRelaciones(registro);
        if (error is not null) return BadRequest(error);

        registro.Precio = decimal.Round(registro.Precio, 2, MidpointRounding.AwayFromZero);
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
        var registro = await _context.Productos.FindAsync(id);
        if (registro is null) return NotFound();

        _context.Productos.Remove(registro);
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

    private async Task<string?> ValidarRelaciones(Productos registro)
    {
        if (!await _context.Marca.AnyAsync(x => x.IdMarca == registro.IdMarca))
            return "La marca indicada no existe.";

        if (!await _context.Categoria.AnyAsync(x => x.IdCategoria == registro.IdCategoria))
            return "La categoría indicada no existe.";

        return null;
    }
}

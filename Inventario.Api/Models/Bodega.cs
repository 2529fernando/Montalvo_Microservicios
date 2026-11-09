using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventario.Api.Models;

[Table("Bodega")]
public class Bodega
{
    [Key]
    public int IdBodega { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Ubicacion { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Catalogo.Api.Models;

[Table("Productos")]
public class Productos
{
    [Key]
    public int IdProducto { get; set; }

    [Range(1, int.MaxValue)]
    public int IdMarca { get; set; }

    [Range(1, int.MaxValue)]
    public int IdCategoria { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Precio { get; set; } = 0;

    public bool Estado { get; set; } = true;
}

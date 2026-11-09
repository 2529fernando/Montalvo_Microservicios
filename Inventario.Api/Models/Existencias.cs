using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventario.Api.Models;

[Table("Existencias")]
public class Existencias
{
    [Key]
    public int IdExistencias { get; set; }

    [Range(1, int.MaxValue)]
    public int IdProducto { get; set; }

    [Range(1, int.MaxValue)]
    public int IdBodega { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 999999999.99)]
    [DecimalScale(2)]
    public decimal Cantidad { get; set; } = 0;

    [Column(TypeName = "datetime2(0)")]
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

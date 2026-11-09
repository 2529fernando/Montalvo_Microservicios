using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventario.Api.Models;

[Table("MovimientosInv")]
public class MovimientosInv
{
    [Key]
    public int IdMovimiento { get; set; }

    [Range(1, int.MaxValue)]
    public int IdBodega { get; set; }

    [Range(1, int.MaxValue)]
    public int IdProducto { get; set; }

    [Required]
    [MaxLength(20)]
    public string TipoMovimiento { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0.01", "999999999.99", ParseLimitsInInvariantCulture = true)]
    [DecimalScale(2)]
    public decimal Cantidad { get; set; } = 0;

    [Column(TypeName = "datetime2(0)")]
    public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string? Observacion { get; set; }
}

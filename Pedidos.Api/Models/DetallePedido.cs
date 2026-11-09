using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Pedidos.Api.Models;

[Table("DetallePedido")]
public class DetallePedido
{
    [Key]
    public int IdDetallePedido { get; set; }

    [Range(1, int.MaxValue)]
    public int IdPedido { get; set; }

    [Range(1, int.MaxValue)]
    public int IdProducto { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal PrecioUnitario { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
    public decimal Subtotal { get; set; } = 0;
}

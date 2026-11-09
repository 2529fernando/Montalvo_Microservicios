using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Pedidos.Api.Models;

[Table("Pedido")]
public class Pedido
{
    [Key]
    public int IdPedido { get; set; }

    [Range(1, int.MaxValue)]
    public int IdCliente { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(30)]
    public string Estado { get; set; } = "Pendiente";

    [Column(TypeName = "decimal(12,2)")]
    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
    public decimal Total { get; set; } = 0;
}

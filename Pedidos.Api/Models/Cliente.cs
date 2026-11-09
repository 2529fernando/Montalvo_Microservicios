using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pedidos.Api.Models;

[Table("Cliente")]
public class Cliente
{
    [Key]
    public int IdCliente { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress]
    public string? Correo { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }
}

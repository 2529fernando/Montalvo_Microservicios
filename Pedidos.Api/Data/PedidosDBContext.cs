using Pedidos.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Pedidos.Api.Data;

public class PedidosDBContext : DbContext
{
    public PedidosDBContext(DbContextOptions<PedidosDBContext> options) : base(options) { }

    public DbSet<Cliente> Cliente => Set<Cliente>();
    public DbSet<Pedido> Pedido => Set<Pedido>();
    public DbSet<DetallePedido> DetallePedido => Set<DetallePedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Pedido>()
            .HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(x => x.IdCliente)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Pedido_Cliente");
        modelBuilder.Entity<DetallePedido>()
            .HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(x => x.IdPedido)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_DetallePedido_Pedido");
    }
}

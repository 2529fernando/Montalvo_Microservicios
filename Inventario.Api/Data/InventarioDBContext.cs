using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Data;

public class InventarioDBContext : DbContext
{
    public InventarioDBContext(DbContextOptions<InventarioDBContext> options) : base(options) { }

    public DbSet<Bodega> Bodega => Set<Bodega>();
    public DbSet<Existencias> Existencias => Set<Existencias>();
    public DbSet<MovimientosInv> MovimientosInv => Set<MovimientosInv>();
    public DbSet<Stock> Stock => Set<Stock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Existencias>()
            .HasOne<Bodega>()
            .WithMany()
            .HasForeignKey(x => x.IdBodega)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Existencias_Bodega");
        modelBuilder.Entity<MovimientosInv>()
            .HasOne<Bodega>()
            .WithMany()
            .HasForeignKey(x => x.IdBodega)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_MovimientosInv_Bodega");
        modelBuilder.Entity<Stock>()
            .HasOne<Bodega>()
            .WithMany()
            .HasForeignKey(x => x.IdBodega)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Stock_Bodega");
    }
}

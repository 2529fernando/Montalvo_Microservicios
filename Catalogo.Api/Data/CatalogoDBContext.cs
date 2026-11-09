using Catalogo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Data;

public class CatalogoDBContext : DbContext
{
    public CatalogoDBContext(DbContextOptions<CatalogoDBContext> options) : base(options) { }

    public DbSet<Marca> Marca => Set<Marca>();
    public DbSet<Categoria> Categoria => Set<Categoria>();
    public DbSet<Productos> Productos => Set<Productos>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Productos>()
            .HasOne<Marca>()
            .WithMany()
            .HasForeignKey(x => x.IdMarca)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Productos_Marca");
        modelBuilder.Entity<Productos>()
            .HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(x => x.IdCategoria)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Productos_Categoria");
    }
}

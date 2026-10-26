using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Models;

namespace RestauranteMVC.Data;

public class RestauranteDbContext : DbContext
{
    public RestauranteDbContext(DbContextOptions<RestauranteDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Plato> Platos { get; set; }
    public DbSet<Cargo> Cargos { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<Mesa> Mesas { get; set; }
    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<DetallePedido> DetallePedidos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Plato>().Property(p => p.Precio).HasPrecision(10, 2);
        modelBuilder.Entity<Pedido>().Property(p => p.Total).HasPrecision(10, 2);

        modelBuilder.Entity<Plato>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Platos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
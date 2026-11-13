using Microsoft.EntityFrameworkCore;
using Pedidos.Api.Models;

namespace Pedidos.Api.Data;

public class PedidosDbContext : DbContext
{
    public PedidosDbContext(DbContextOptions<PedidosDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallePedidos => Set<DetallePedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(c => c.IdCliente);
            entity.Property(c => c.Nombres).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Cedula).IsRequired().HasMaxLength(20);
            entity.HasIndex(c => c.Cedula).IsUnique();
            entity.Property(c => c.Correo).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.Correo).IsUnique();
            entity.Property(c => c.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("Pedidos");
            entity.HasKey(p => p.IdPedido);
            entity.Property(p => p.Fecha).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(p => p.Total).HasPrecision(18, 2);
            entity.Property(p => p.Estado).IsRequired().HasMaxLength(20).HasDefaultValue("PENDIENTE");

            entity.HasOne(p => p.Cliente)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetallePedido>(entity =>
        {
            entity.ToTable("DetallePedido");
            entity.HasKey(d => d.IdDetalle);
            entity.Property(d => d.IdProducto).IsRequired();
            entity.Property(d => d.Cantidad).IsRequired();
            entity.Property(d => d.PrecioUnitario).HasPrecision(18, 2);

            entity.HasOne(d => d.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.Cascade);
        });

        var fechaSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Cliente>().HasData(
            new Cliente { IdCliente = 1, Nombres = "Ana María Cárdenas", Cedula = "0900000001", Correo = "ana@correo.com", FechaCreacion = fechaSeed },
            new Cliente { IdCliente = 2, Nombres = "Luis Tomalá Ortega", Cedula = "0900000019", Correo = "luis@correo.com", FechaCreacion = fechaSeed },
            new Cliente { IdCliente = 3, Nombres = "Sofía López Paredes", Cedula = "1700000027", Correo = "sofia@correo.com", FechaCreacion = fechaSeed }
        );
    }
}

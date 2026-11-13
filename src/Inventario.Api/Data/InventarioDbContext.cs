using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Data;

public class InventarioDbContext : DbContext
{
    public InventarioDbContext(DbContextOptions<InventarioDbContext> options) : base(options)
    {
    }

    public DbSet<Bodega> Bodegas => Set<Bodega>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<ReservaStock> ReservasStock => Set<ReservaStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Bodega
        modelBuilder.Entity<Bodega>(entity =>
        {
            entity.ToTable("Bodegas");
            entity.HasKey(b => b.IdBodega);
            entity.Property(b => b.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Ciudad).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Direccion).IsRequired().HasMaxLength(200);
            entity.Property(b => b.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(b => b.Existencias)
                  .WithOne(e => e.Bodega)
                  .HasForeignKey(e => e.IdBodega)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(b => b.Movimientos)
                  .WithOne(m => m.Bodega)
                  .HasForeignKey(m => m.IdBodega)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(b => b.Reservas)
                  .WithOne(r => r.Bodega)
                  .HasForeignKey(r => r.IdBodega)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Existencia
        modelBuilder.Entity<Existencia>(entity =>
        {
            entity.ToTable("Existencias");
            entity.HasKey(e => e.IdExistencia);
            entity.Property(e => e.Stock).IsRequired();
            entity.Property(e => e.UltimaActualizacion).HasDefaultValueSql("GETUTCDATE()");

            // Índice único (IdProducto, IdBodega)
            entity.HasIndex(e => new { e.IdProducto, e.IdBodega }).IsUnique();

            // NOTA: IdProducto es una referencia LÓGICA entre microservicios; NO tiene FK física.
        });

        // MovimientoInventario
        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.ToTable("MovimientosInventario");
            entity.HasKey(m => m.IdMovimiento);
            entity.Property(m => m.Tipo).IsRequired().HasMaxLength(20);
            entity.Property(m => m.Cantidad).IsRequired();
            entity.Property(m => m.Fecha).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(m => m.Observaciones).HasMaxLength(250);

            // NOTA: IdProducto es una referencia LÓGICA entre microservicios; NO tiene FK física.
        });

        // ReservaStock
        modelBuilder.Entity<ReservaStock>(entity =>
        {
            entity.ToTable("ReservasStock");
            entity.HasKey(r => r.IdReserva);
            entity.Property(r => r.Cantidad).IsRequired();
            entity.Property(r => r.Estado).IsRequired().HasMaxLength(20).HasDefaultValue("ACTIVA");
            entity.Property(r => r.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(r => r.ReferenciaExterna).HasMaxLength(100);

            // NOTA: IdProducto es una referencia LÓGICA entre microservicios; NO tiene FK física.
        });

        // Seed data
        var fechaSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Bodega>().HasData(
            new Bodega { IdBodega = 1, Nombre = "Bodega Central Quito", Ciudad = "Quito", Direccion = "Av. 10 de Agosto y Mariana de Jesús", FechaCreacion = fechaSeed },
            new Bodega { IdBodega = 2, Nombre = "Bodega Guayaquil Puerto", Ciudad = "Guayaquil", Direccion = "Av. Juan Tanca Marengo Km 4.5", FechaCreacion = fechaSeed },
            new Bodega { IdBodega = 3, Nombre = "Bodega Cuenca Austro", Ciudad = "Cuenca", Direccion = "Av. España y Huayna Cápac", FechaCreacion = fechaSeed }
        );

        modelBuilder.Entity<Existencia>().HasData(
            // Quito (Bodega 1)
            new Existencia { IdExistencia = 1, IdBodega = 1, IdProducto = 1, Stock = 25, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 2, IdBodega = 1, IdProducto = 2, Stock = 15, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 3, IdBodega = 1, IdProducto = 3, Stock = 30, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 4, IdBodega = 1, IdProducto = 5, Stock = 50, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 5, IdBodega = 1, IdProducto = 7, Stock = 40, UltimaActualizacion = fechaSeed },

            // Guayaquil (Bodega 2)
            new Existencia { IdExistencia = 6, IdBodega = 2, IdProducto = 1, Stock = 20, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 7, IdBodega = 2, IdProducto = 3, Stock = 25, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 8, IdBodega = 2, IdProducto = 4, Stock = 20, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 9, IdBodega = 2, IdProducto = 6, Stock = 35, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 10, IdBodega = 2, IdProducto = 8, Stock = 60, UltimaActualizacion = fechaSeed },

            // Cuenca (Bodega 3)
            new Existencia { IdExistencia = 11, IdBodega = 3, IdProducto = 2, Stock = 10, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 12, IdBodega = 3, IdProducto = 4, Stock = 15, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 13, IdBodega = 3, IdProducto = 5, Stock = 20, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 14, IdBodega = 3, IdProducto = 7, Stock = 25, UltimaActualizacion = fechaSeed },
            new Existencia { IdExistencia = 15, IdBodega = 3, IdProducto = 8, Stock = 30, UltimaActualizacion = fechaSeed }
        );
    }
}

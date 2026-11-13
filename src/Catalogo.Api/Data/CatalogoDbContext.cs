using Catalogo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Data;

public class CatalogoDbContext : DbContext
{
    public CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("Categorias");
            entity.HasKey(c => c.IdCategoria);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Descripcion).HasMaxLength(250);
            entity.Property(c => c.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(c => c.Productos)
                  .WithOne(p => p.Categoria)
                  .HasForeignKey(p => p.IdCategoria)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Marca
        modelBuilder.Entity<Marca>(entity =>
        {
            entity.ToTable("Marcas");
            entity.HasKey(m => m.IdMarca);
            entity.Property(m => m.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(m => m.PaisOrigen).IsRequired().HasMaxLength(100);
            entity.Property(m => m.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(m => m.Productos)
                  .WithOne(p => p.Marca)
                  .HasForeignKey(p => p.IdMarca)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Producto
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(p => p.IdProducto);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(p => p.SKU).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.SKU).IsUnique();
            entity.Property(p => p.Precio).HasPrecision(18, 2);
            entity.Property(p => p.Estado).IsRequired().HasMaxLength(20).HasDefaultValue("ACTIVO");
            entity.Property(p => p.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");
        });

        // Seed data
        var fechaSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { IdCategoria = 1, Nombre = "Laptops y Portátiles", Descripcion = "Equipos portátiles para trabajo, estudio y gaming", FechaCreacion = fechaSeed },
            new Categoria { IdCategoria = 2, Nombre = "Smartphones y Tablets", Descripcion = "Dispositivos móviles y tablets de última generación", FechaCreacion = fechaSeed },
            new Categoria { IdCategoria = 3, Nombre = "Periféricos y Accesorios", Descripcion = "Teclados, ratones, audio y conectividad de escritorio", FechaCreacion = fechaSeed },
            new Categoria { IdCategoria = 4, Nombre = "Redes y Conectividad", Descripcion = "Routers, switches y tarjetas de red cableada e inalámbrica", FechaCreacion = fechaSeed }
        );

        modelBuilder.Entity<Marca>().HasData(
            new Marca { IdMarca = 1, Nombre = "Lenovo", PaisOrigen = "China", FechaCreacion = fechaSeed },
            new Marca { IdMarca = 2, Nombre = "Apple", PaisOrigen = "Estados Unidos", FechaCreacion = fechaSeed },
            new Marca { IdMarca = 3, Nombre = "Logitech", PaisOrigen = "Suiza", FechaCreacion = fechaSeed },
            new Marca { IdMarca = 4, Nombre = "TP-Link", PaisOrigen = "China", FechaCreacion = fechaSeed }
        );

        modelBuilder.Entity<Producto>().HasData(
            new Producto { IdProducto = 1, IdCategoria = 1, IdMarca = 1, Nombre = "Laptop Lenovo ThinkPad E14 Gen 5", SKU = "PROD-THINKPAD-E14", Precio = 899.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed },
            new Producto { IdProducto = 2, IdCategoria = 1, IdMarca = 2, Nombre = "MacBook Air M2 13 pulgadas", SKU = "PROD-MACBOOK-M2", Precio = 1199.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed },
            new Producto { IdProducto = 3, IdCategoria = 2, IdMarca = 2, Nombre = "iPhone 15 Pro 128GB", SKU = "PROD-IPHONE-15P", Precio = 1049.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed },
            new Producto { IdProducto = 4, IdCategoria = 2, IdMarca = 2, Nombre = "iPad Air 11 pulgadas M2", SKU = "PROD-IPAD-AIR-M2", Precio = 699.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed },
            new Producto { IdProducto = 5, IdCategoria = 3, IdMarca = 3, Nombre = "Mouse Inalámbrico Logitech MX Master 3S", SKU = "PROD-LOGI-MXM3S", Precio = 99.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed },
            new Producto { IdProducto = 6, IdCategoria = 3, IdMarca = 3, Nombre = "Teclado Mecánico Logitech MX Mechanical", SKU = "PROD-LOGI-MXMECH", Precio = 149.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed },
            new Producto { IdProducto = 7, IdCategoria = 4, IdMarca = 4, Nombre = "Router WiFi 6 TP-Link Archer AX55", SKU = "PROD-TPLINK-AX55", Precio = 79.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed },
            new Producto { IdProducto = 8, IdCategoria = 4, IdMarca = 4, Nombre = "Switch Gigabit TP-Link 8 Puertos TL-SG108", SKU = "PROD-TPLINK-SG108", Precio = 29.99m, Estado = "ACTIVO", FechaCreacion = fechaSeed }
        );
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalogo.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    IdCategoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.IdCategoria);
                });

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    IdMarca = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaisOrigen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.IdMarca);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    IdProducto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCategoria = table.Column<int>(type: "int", nullable: false),
                    IdMarca = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVO"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.IdProducto);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_IdCategoria",
                        column: x => x.IdCategoria,
                        principalTable: "Categorias",
                        principalColumn: "IdCategoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_Marcas_IdMarca",
                        column: x => x.IdMarca,
                        principalTable: "Marcas",
                        principalColumn: "IdMarca",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "IdCategoria", "Descripcion", "FechaCreacion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Equipos portátiles para trabajo, estudio y gaming", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Laptops y Portátiles" },
                    { 2, "Dispositivos móviles y tablets de última generación", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Smartphones y Tablets" },
                    { 3, "Teclados, ratones, audio y conectividad de escritorio", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Periféricos y Accesorios" },
                    { 4, "Routers, switches y tarjetas de red cableada e inalámbrica", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Redes y Conectividad" }
                });

            migrationBuilder.InsertData(
                table: "Marcas",
                columns: new[] { "IdMarca", "FechaCreacion", "Nombre", "PaisOrigen" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lenovo", "China" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Apple", "Estados Unidos" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Logitech", "Suiza" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TP-Link", "China" }
                });

            migrationBuilder.InsertData(
                table: "Productos",
                columns: new[] { "IdProducto", "Estado", "FechaCreacion", "IdCategoria", "IdMarca", "Nombre", "Precio", "SKU" },
                values: new object[,]
                {
                    { 1, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, "Laptop Lenovo ThinkPad E14 Gen 5", 899.99m, "PROD-THINKPAD-E14" },
                    { 2, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 2, "MacBook Air M2 13 pulgadas", 1199.99m, "PROD-MACBOOK-M2" },
                    { 3, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2, "iPhone 15 Pro 128GB", 1049.99m, "PROD-IPHONE-15P" },
                    { 4, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2, "iPad Air 11 pulgadas M2", 699.99m, "PROD-IPAD-AIR-M2" },
                    { 5, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 3, "Mouse Inalámbrico Logitech MX Master 3S", 99.99m, "PROD-LOGI-MXM3S" },
                    { 6, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 3, "Teclado Mecánico Logitech MX Mechanical", 149.99m, "PROD-LOGI-MXMECH" },
                    { 7, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 4, "Router WiFi 6 TP-Link Archer AX55", 79.99m, "PROD-TPLINK-AX55" },
                    { 8, "ACTIVO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 4, "Switch Gigabit TP-Link 8 Puertos TL-SG108", 29.99m, "PROD-TPLINK-SG108" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_IdCategoria",
                table: "Productos",
                column: "IdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_IdMarca",
                table: "Productos",
                column: "IdMarca");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_SKU",
                table: "Productos",
                column: "SKU",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Marcas");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Inventario.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bodegas",
                columns: table => new
                {
                    IdBodega = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ciudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bodegas", x => x.IdBodega);
                });

            migrationBuilder.CreateTable(
                name: "Existencias",
                columns: table => new
                {
                    IdExistencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    IdBodega = table.Column<int>(type: "int", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Existencias", x => x.IdExistencia);
                    table.ForeignKey(
                        name: "FK_Existencias_Bodegas_IdBodega",
                        column: x => x.IdBodega,
                        principalTable: "Bodegas",
                        principalColumn: "IdBodega",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    IdMovimiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    IdBodega = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.IdMovimiento);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Bodegas_IdBodega",
                        column: x => x.IdBodega,
                        principalTable: "Bodegas",
                        principalColumn: "IdBodega",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservasStock",
                columns: table => new
                {
                    IdReserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    IdBodega = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVA"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenciaExterna = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasStock", x => x.IdReserva);
                    table.ForeignKey(
                        name: "FK_ReservasStock_Bodegas_IdBodega",
                        column: x => x.IdBodega,
                        principalTable: "Bodegas",
                        principalColumn: "IdBodega",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Bodegas",
                columns: new[] { "IdBodega", "Ciudad", "Direccion", "FechaCreacion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Quito", "Av. 10 de Agosto y Mariana de Jesús", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bodega Central Quito" },
                    { 2, "Guayaquil", "Av. Juan Tanca Marengo Km 4.5", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bodega Guayaquil Puerto" },
                    { 3, "Cuenca", "Av. España y Huayna Cápac", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bodega Cuenca Austro" }
                });

            migrationBuilder.InsertData(
                table: "Existencias",
                columns: new[] { "IdExistencia", "IdBodega", "IdProducto", "Stock", "UltimaActualizacion" },
                values: new object[,]
                {
                    { 1, 1, 1, 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 1, 2, 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 1, 3, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 1, 5, 50, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, 1, 7, 40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, 2, 1, 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, 2, 3, 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, 2, 4, 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, 2, 6, 35, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, 2, 8, 60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, 3, 2, 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, 3, 4, 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, 3, 5, 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, 3, 7, 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, 3, 8, 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Existencias_IdBodega",
                table: "Existencias",
                column: "IdBodega");

            migrationBuilder.CreateIndex(
                name: "IX_Existencias_IdProducto_IdBodega",
                table: "Existencias",
                columns: new[] { "IdProducto", "IdBodega" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_IdBodega",
                table: "MovimientosInventario",
                column: "IdBodega");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasStock_IdBodega",
                table: "ReservasStock",
                column: "IdBodega");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Existencias");

            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "ReservasStock");

            migrationBuilder.DropTable(
                name: "Bodegas");
        }
    }
}

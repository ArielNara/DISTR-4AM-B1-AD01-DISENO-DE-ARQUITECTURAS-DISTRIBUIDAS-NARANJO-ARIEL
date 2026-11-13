using Inventario.Api.Data;
using Inventario.Api.Middleware;
using Inventario.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// DbContext SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue configurada.");

builder.Services.AddDbContext<InventarioDbContext>(options =>
    options.UseSqlServer(connectionString));

// Cliente HTTP tipado hacia Catalogo.Api
var catalogoApiUrl = builder.Configuration["Services:CatalogoApiUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient<ICatalogoHttpClient, CatalogoHttpClient>(client =>
{
    client.BaseAddress = new Uri(catalogoApiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Controladores
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inventario.Api",
        Version = "v1",
        Description = "Microservicio de Almacenamiento, Existencias, Movimientos y Reservas - IST Cordillera AD01"
    });
});

var app = builder.Build();

// Middleware global de manejo de excepciones
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventario.Api v1");
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();

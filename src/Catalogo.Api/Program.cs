using Catalogo.Api.Data;
using Catalogo.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuración de DbContext con SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue configurada.");

builder.Services.AddDbContext<CatalogoDbContext>(options =>
    options.UseSqlServer(connectionString));

// Controladores con serialización JSON
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalogo.Api",
        Version = "v1",
        Description = "Microservicio de Catálogo Comercial (Categorías, Marcas, Productos) - IST Cordillera AD01"
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalogo.Api v1");
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();

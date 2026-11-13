using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Pedidos.Api.Data;
using Pedidos.Api.Middleware;
using Pedidos.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue configurada.");

builder.Services.AddDbContext<PedidosDbContext>(options =>
    options.UseSqlServer(connectionString));

var catalogoApiUrl = builder.Configuration["Services:CatalogoApiUrl"] ?? "http://localhost:5001";
var inventarioApiUrl = builder.Configuration["Services:InventarioApiUrl"] ?? "http://localhost:5002";

builder.Services.AddHttpClient<ICatalogoHttpClient, CatalogoHttpClient>(client =>
{
    client.BaseAddress = new Uri(catalogoApiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<IInventarioHttpClient, InventarioHttpClient>(client =>
{
    client.BaseAddress = new Uri(inventarioApiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pedidos.Api",
        Version = "v1",
        Description = "Microservicio de pedidos y orquestación del comercio multibodega - IST Cordillera AD01"
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pedidos.Api v1");
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();

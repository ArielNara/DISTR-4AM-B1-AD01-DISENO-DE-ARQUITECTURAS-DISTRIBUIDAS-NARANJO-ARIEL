# AD01 - Diseño de Arquitecturas Distribuidas

Este repositorio implementa una solución con tres microservicios ASP.NET Core Web API independientes:

- Catalogo.Api: administración del catálogo comercial.
- Inventario.Api: control de bodegas, existencias, movimientos y reservas.
- Pedidos.Api: gestión de clientes y pedidos, con orquestación entre los dos servicios.

## Requisitos de ejecución

- .NET 8 SDK
- SQL Server local (instancia por defecto `localhost` o `./SQLEXPRESS` si corresponde)
- Ejecutar cada API en su puerto definido:
  - Catalogo.Api: `http://localhost:5001`
  - Inventario.Api: `http://localhost:5002`
  - Pedidos.Api: `http://localhost:5003`

## Estructura

- `src/Catalogo.Api`
- `src/Inventario.Api`
- `src/Pedidos.Api`

## Conexión a base de datos

Las cadenas de conexión se definen en `appsettings.json` de cada API y usan la forma recomendada:

```text
Server=localhost;Database=<NombreDB>;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Si la instancia del servidor local no está disponible en este entorno, debe verificarse la presencia de SQL Server y, si aplica, usar `./SQLEXPRESS`.

## Regla de llaves foráneas

| Relación | Tipo de FK | Regla de borrado | Observación |
|---|---|---|---|
| Categoria -> Producto | Física dentro de CatalogoDB | Restrict | Evita eliminar categorías que tienen productos asociados. |
| Marca -> Producto | Física dentro de CatalogoDB | Restrict | Evita eliminar marcas que tienen productos asociados. |
| Bodega -> Existencia | Física dentro de InventarioDB | Restrict | Evita eliminar bodegas con stock registrado. |
| Bodega -> MovimientoInventario | Física dentro de InventarioDB | Restrict | Evita eliminar bodegas con historial de movimientos. |
| Bodega -> ReservaStock | Física dentro de InventarioDB | Restrict | Evita eliminar bodegas con reservas asociadas. |
| Pedido -> DetallePedido | Física dentro de PedidosDB | Cascade | Cuando se elimina un pedido, se eliminan sus líneas. |
| Cliente -> Pedido | Física dentro de PedidosDB | Restrict | Un cliente no puede eliminarse si ya tiene pedidos. |
| Producto (Inventario) | Lógica | Sin FK física | Se valida contra Catalogo.Api por HTTP. |
| Producto (Pedidos) | Lógica | Sin FK física | Se valida contra Catalogo.Api por HTTP. |

## Seeding

Los servicios incluyen datos iniciales coherentes para:

- 4 categorías de productos
- 4 marcas
- 8 productos con SKU únicos
- 3 bodegas
- 3 clientes con cédulas ecuatorianas válidas
- existencias base para las bodegas

## Endpoints clave

- Catalogo:
  - `GET /api/categorias`
  - `GET /api/marcas`
  - `GET /api/productos`
  - `GET /api/productos/{id}`
  - `GET /api/productos/sku/{sku}`
- Inventario:
  - `GET /api/bodegas`
  - `GET /api/existencias`
  - `GET /api/existencias/producto/{idProducto}`
  - `PUT /api/existencias/ajuste`
  - `POST /api/movimientos`
  - `GET /api/movimientos`
  - `POST /api/reservas`
  - `PUT /api/reservas/{id}/confirmar`
  - `PUT /api/reservas/{id}/cancelar`
- Pedidos:
  - `GET /api/clientes`
  - `POST /api/pedidos`
  - `GET /api/pedidos`
  - `GET /api/pedidos/{id}`
  - `PUT /api/pedidos/{id}/cancelar`

## Validaciones principales

- SKU único en catálogo
- Cédula ecuatoriana de 10 dígitos con módulo 10
- Correo electrónico válido y único
- Cantidades mayores a 0
- Precios mayores a 0
- Estados con valores controlados (`ACTIVO`, `INACTIVO`, `PENDIENTE`, etc.)
- Problemas de negocio devueltos con `ProblemDetails`

## Ejecutar la solución

```bash
dotnet build DISTR.sln
```

Cada API puede arrancarse de forma independiente con:

```bash
dotnet run --project src/Catalogo.Api/Catalogo.Api.csproj --launch-profile http
dotnet run --project src/Inventario.Api/Inventario.Api.csproj --launch-profile http
dotnet run --project src/Pedidos.Api/Pedidos.Api.csproj --launch-profile http
```

## Pruebas con Postman

Importa como **Collection** el archivo `postman/AD01 - Proyecto completo.postman_collection.json`. La colección incluye los endpoints CRUD y un flujo E2E para crear **Manzana Cordillera**, registrar stock y generar un pedido. No requiere importar un Environment.

Para el flujo E2E, ejecuta las solicitudes numeradas de la carpeta `10 - Flujo E2E - Manzana Cordillera` en orden. Los servicios deben estar ejecutándose localmente en los puertos 5001, 5002 y 5003. Los POST guardan los identificadores generados en variables de la colección.

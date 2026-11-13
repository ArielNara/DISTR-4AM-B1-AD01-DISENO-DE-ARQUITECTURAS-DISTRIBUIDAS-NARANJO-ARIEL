$catJob = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Catalogo.Api/Catalogo.Api.csproj --launch-profile http" -PassThru -NoNewWindow
$invJob = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Inventario.Api/Inventario.Api.csproj --launch-profile http" -PassThru -NoNewWindow

Start-Sleep -Seconds 8

try {
    Write-Host "--- TEST 1: GET /api/bodegas ---"
    $bodegas = Invoke-RestMethod -Uri "http://localhost:5002/api/bodegas" -Method Get
    Write-Host "Bodegas recibidas: $($bodegas.Count) | Primera: $($bodegas[0].Nombre)"

    Write-Host "--- TEST 2: GET /api/existencias/producto/1 (valida contra Catalogo.Api) ---"
    $resumen = Invoke-RestMethod -Uri "http://localhost:5002/api/existencias/producto/1" -Method Get
    Write-Host "Producto 1 -> Total Stock Fisico: $($resumen.TotalStockFisico) | Bodegas: $($resumen.Bodegas.Count)"

    Write-Host "--- TEST 3: POST /api/reservas (Reserva exitosa de 5 unidades) ---"
    $reservaBody = '{"IdProducto":1,"IdBodega":1,"Cantidad":5,"ReferenciaExterna":"TEST-ORD-01"}'
    $reserva = Invoke-RestMethod -Uri "http://localhost:5002/api/reservas" -Method Post -Body $reservaBody -ContentType "application/json"
    $reservaId = $reserva.IdReserva
    Write-Host "Reserva creada con Id: $reservaId | Estado: $($reserva.Estado)"

    Write-Host "--- TEST 4: GET /api/existencias?idProducto=1&idBodega=1 (Verificar stock disponible) ---"
    $existencias = Invoke-RestMethod -Uri "http://localhost:5002/api/existencias?idProducto=1&idBodega=1" -Method Get
    Write-Host "Stock fisico: $($existencias[0].Stock) | Reservas activas: $($existencias[0].ReservasActivas) | Disponible: $($existencias[0].StockDisponible)"

    Write-Host "--- TEST 5: POST /api/reservas (409 Conflict por stock disponible insuficiente) ---"
    try {
        $excesoBody = '{"IdProducto":1,"IdBodega":1,"Cantidad":25,"ReferenciaExterna":"TEST-OVERFLOW"}'
        Invoke-RestMethod -Uri "http://localhost:5002/api/reservas" -Method Post -Body $excesoBody -ContentType "application/json"
        Write-Host "FALLO: Debio retornar 409"
    } catch {
        Write-Host "EXITO: Retorno status $([int]$_.Exception.Response.StatusCode)"
    }

    Write-Host "--- TEST 6: POST /api/reservas (404 Not Found producto inexistente en Catálogo) ---"
    try {
        $noProdBody = '{"IdProducto":999,"IdBodega":1,"Cantidad":1}'
        Invoke-RestMethod -Uri "http://localhost:5002/api/reservas" -Method Post -Body $noProdBody -ContentType "application/json"
        Write-Host "FALLO: Debio retornar 404"
    } catch {
        Write-Host "EXITO: Retorno status $([int]$_.Exception.Response.StatusCode)"
    }

    Write-Host "--- TEST 7: PUT /api/reservas/{id}/confirmar (Descuenta stock y genera SALIDA) ---"
    $conf = Invoke-RestMethod -Uri "http://localhost:5002/api/reservas/$reservaId/confirmar" -Method Put
    Write-Host "Reserva confirmada. Estado: $($conf.Estado)"
    $existenciasPost = Invoke-RestMethod -Uri "http://localhost:5002/api/existencias?idProducto=1&idBodega=1" -Method Get
    Write-Host "Stock fisico tras confirmacion: $($existenciasPost[0].Stock) (deberia ser 20) | Reservas activas: $($existenciasPost[0].ReservasActivas)"

    Write-Host "--- TEST 8: POST /api/movimientos (ENTRADA de 10 unidades) ---"
    $movEntrada = '{"IdProducto":1,"IdBodega":1,"Tipo":"ENTRADA","Cantidad":10,"Observaciones":"Reabastecimiento"}'
    $m1 = Invoke-RestMethod -Uri "http://localhost:5002/api/movimientos" -Method Post -Body $movEntrada -ContentType "application/json"
    Write-Host "Movimiento registrado: $($m1.Tipo) de $($m1.Cantidad) unidades"

    Write-Host "--- TEST 9: POST /api/movimientos (409 Conflict por SALIDA excesiva) ---"
    try {
        $movSalida = '{"IdProducto":1,"IdBodega":1,"Tipo":"SALIDA","Cantidad":500,"Observaciones":"Exceso"}'
        Invoke-RestMethod -Uri "http://localhost:5002/api/movimientos" -Method Post -Body $movSalida -ContentType "application/json"
        Write-Host "FALLO: Debio retornar 409"
    } catch {
        Write-Host "EXITO: Retorno status $([int]$_.Exception.Response.StatusCode)"
    }

    Write-Host "--- TEST 10: DELETE /api/bodegas/1 (409 Conflict - Restrict) ---"
    try {
        Invoke-RestMethod -Uri "http://localhost:5002/api/bodegas/1" -Method Delete
        Write-Host "FALLO: Debio retornar 409"
    } catch {
        Write-Host "EXITO: Retorno status $([int]$_.Exception.Response.StatusCode)"
    }

} finally {
    Get-Process Catalogo.Api, Inventario.Api -ErrorAction SilentlyContinue | Stop-Process -Force
}

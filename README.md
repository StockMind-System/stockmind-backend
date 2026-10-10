# StockMind Backend

API REST construida con ASP.NET Core 8 como monolito modular y arquitectura
hexagonal. MongoDB almacena la informacion operacional y el servicio Python de
analytics realiza el entrenamiento, las predicciones y la evaluacion.

## Arquitectura

Los cuatro proyectos de `src` representan los puertos y adaptadores de la
arquitectura. Dentro de cada proyecto, `Modules` separa las capacidades de
negocio:

- `Identity`: usuarios, autenticacion, JWT y hashing.
- `Catalog`: productos y categorias.
- `Inventory`: movimientos de inventario.
- `Sales`: ventas y detalles.
- `Forecasting`: predicciones, evaluaciones y puerto hacia analytics.
- `Alerts`: calculo de alertas de reposicion.
- `Reporting`: consultas consolidadas.

Cada modulo se distribuye entre cuatro capas:

- `Domain`: entidades y reglas de negocio, sin dependencias externas.
- `Application`: puertos y casos de uso.
- `Infrastructure`: adaptadores de salida para MongoDB, HTTP o seguridad.
- `Api`: adaptadores de entrada HTTP.

`src/StockMind.API` es el composition root. Solo este proyecto conecta los
modulos y configura el proceso web.

La direccion de dependencias es `API -> Application <- Infrastructure`, con
`Application -> Domain`. El dominio no depende de ASP.NET Core, MongoDB ni del
servicio de analitica.

## Contratos y responsabilidades

- Angular consume exclusivamente los endpoints `/api/*` de este backend.
- C# controla autenticacion, catalogo, inventario, ventas, alertas y reportes.
- El modulo `Forecasting` traduce el contrato HTTP publico en camelCase al
  contrato snake_case del servicio Python; los detalles de transporte no se
  filtran hacia el frontend.
- Python entrena, predice y evalua. C# persiste la trazabilidad devuelta:
  SKU, fecha de corte, horizonte, unidad, metodo y versiones de modelo/dataset.
- Los movimientos, productos y ventas se crean mediante reglas del dominio;
  las solicitudes invalidas se responden como HTTP 400.

## Ejecucion local

Requisitos: .NET SDK 8, MongoDB y el servicio `stockmind-analytics` en el puerto
`8000`.

```powershell
dotnet restore
dotnet run --project src/StockMind.API
```

La comprobacion de salud queda disponible en `GET /health`. La configuracion se
puede sobrescribir con variables de entorno usando doble guion bajo, por ejemplo
`MongoDb__ConnectionString`, `Analytics__BaseUrl` y
`Cors__AllowedOrigins__0`. El origen de desarrollo permitido es
`http://localhost:4200`.

## Pruebas

```powershell
dotnet test
```

## Docker

```powershell
docker compose up --build
```

Antes de desplegar, reemplaza la clave JWT de desarrollo. El inicio de sesion
consulta la coleccion `usuarios`; las cuentas deben almacenarse con una
contrasena generada por el `PasswordHasher` del modulo Identity.

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

## Base de datos de desarrollo

Al iniciar la API con `MongoDb:InitializeOnStartup=true`, el backend crea los
indices de MongoDB. En el entorno `Development` y en Docker tambien activa
`SeedData:Enabled=true` para cargar un dataset sintetico reproducible solo en
las colecciones vacias. La semilla
predeterminada es `202610` y queda registrada en
`metadata_sistema` junto con la version del dataset y la zona horaria del
negocio. Incluye categorias, 12 productos, usuarios de demostracion, 120 dias
de ventas, movimientos de inventario coherentes con esas ventas y pronosticos
de muestra. Los productos y movimientos conservan un stock final auditable.

Las cuentas de desarrollo son:

| Correo | Rol |
| --- | --- |
| `dayana@stockmind.demo` | `ADMIN` |
| `milton@stockmind.demo` | `USER` |
| `oscar@stockmind.demo` | `USER` |
| `isamar@stockmind.demo` | `ANALYST` |

La contrasena inicial es `StockMind-123`. En Docker puede reemplazarse con
`SEED_DEFAULT_PASSWORD`; en ejecucion local, con
`SeedData__DefaultPassword`. El sembrado no elimina ni reemplaza datos
operacionales existentes.

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

# AGENTS.md — StockMind / reglas comunes

## Alcance y propósito
Estas instrucciones corresponden a StockMind: aplicación web para ventas, inventario y pronóstico de demanda de una MYPE ficticia, Lima 2026. El piloto utiliza datos sintéticos, con reglas de generación documentadas.

Componentes acordados:
- Angular + TypeScript + PrimeNG: interfaz.
- C# + ASP.NET Core: API y reglas de negocio.
- Python + FastAPI + XGBoost Regressor: servicio analítico.
- MongoDB: persistencia operativa y resultados.

Este archivo es opcional en la carpeta que contiene los tres proyectos. Cada proyecto conserva su propio AGENTS.md, que también funciona de forma independiente. No asumir que las instrucciones de una carpeta hermana se cargaron: leer explícitamente su AGENTS.md si la tarea requiere revisar o modificar ese proyecto.

## Responsabilidades entre proyectos
- Angular consume la API de C#. No se conecta directamente a MongoDB ni a FastAPI.
- C# autoriza operaciones, registra ventas/compras/movimientos, mantiene saldos, calcula ABC, stock de seguridad, punto de reposición y EOQ, y conserva auditoría.
- Python prepara series semanales por SKU, entrena, realiza backtesting, compara XGBoost con promedio móvil de cuatro semanas y devuelve pronósticos y métricas.
- C# coordina la comunicación con Python. Evitar dos implementaciones autoritativas de la misma regla.
- Ningún componente ejecuta compras automáticas: las sugerencias requieren revisión y aprobación humana.

## Reglas de trabajo
1. Leer README, instrucciones aplicables, manifiestos, configuración y código relacionado antes de editar.
2. Identificar versiones y herramientas realmente instaladas. No actualizar frameworks, cambiar gestor de paquetes ni añadir dependencias por rutina.
3. Para una refactorización de arquitectura, presentar primero un diagnóstico breve y el orden de cambios. Continuar con la implementación si la tarea ya la autoriza; si se pidió solo análisis, entregar solo análisis.
4. Conservar contratos, comportamiento y datos existentes salvo cambio solicitado. Refactorizar en pasos pequeños.
5. No reformatear archivos ajenos al cambio ni sobrescribir trabajo del usuario.
6. Elegir la solución más simple que satisfaga el caso. No crear carpetas vacías, interfaces por cada clase, microservicios nuevos ni patrones sin necesidad concreta.
7. Aplicar SRP en componentes/servicios; OCP en variaciones reales; DIP en acceso a datos y servicios externos; contratos pequeños e intercambiables para ISP y LSP.
8. Mantener nombres descriptivos, funciones enfocadas, tipado y tratamiento explícito de errores.
9. No inventar comandos, pruebas aprobadas, mediciones ni resultados.
10. Al finalizar, informar qué cambió, por qué, cómo se verificó y qué quedó pendiente.

## Contratos e integración
Antes de modificar un DTO, endpoint, enum o campo:
- Identificar productores y consumidores en los otros repositorios.
- Revisar OpenAPI y los contratos existentes; acordar nombres, unidades, nulabilidad, fechas y formato de errores.
- Actualizar consumidores, documentación y pruebas de integración afectados.
- Mantener identificadores externos como cadenas; no exponer tipos internos de MongoDB.
- Usar ISO 8601 en intercambios. Conservar instantes en UTC y definir explícitamente la zona de negocio y el inicio de semana al agregar demanda.
- Definir precisión y redondeo de importes, cantidades y métricas. Los cálculos monetarios autoritativos viven en C#.
- No serializar NaN o infinito; una métrica no calculable se devuelve como null con motivo.
- Diferenciar resultado válido, falta de datos, error y resultado antiguo.

El pronóstico identifica al menos SKU, fecha de corte, horizonte, semanas estimadas, unidad, método, versión del modelo/dataset y estado. El horizonte previsto es de cuatro semanas, configurable si el contrato lo permite.

## Datos, evaluación y reposición
- Mantener datos sintéticos identificables y generación reproducible por semilla. No presentarlos como registros ni mejoras observadas en una empresa real.
- Respetar orden temporal en entrenamiento, validación y prueba. Nunca utilizar ventas posteriores al corte para construir variables, seleccionar modelos o aplicar ABC en una simulación histórica.
- Priorizar SKU A/B con historial suficiente. La suficiencia se define antes de evaluar, considerando rezagos y horizonte. 52 semanas es una referencia de diseño y 26 un piloto propuesto, no una garantía estadística.
- Documentar contingencia por promedio móvil o categoría; no repartir una predicción de categoría entre SKU sin regla explícita.
- MAE, RMSE y WAPE son métricas principales de regresión; R² es complementaria. F1 solo aplica a una clasificación con etiquetas y umbrales definidos.
- Separar métricas predictivas de indicadores operativos: quiebres, excedente, exactitud y tiempo de planificación.
- No afirmar que XGBoost mejora la referencia ni que se logró la meta preliminar de 10 % sin evidencia.
- No generar EOQ sin costos requeridos, ni stock de seguridad/ROP sin sus insumos y unidades coherentes. Indicar qué falta.
- Definir estados de sugerencia, aprobación y ejecución; aprobar una sugerencia no equivale a recibir productos.
- Conservar usuario, fecha, motivo y valores relevantes en la auditoría.

## Seguridad y calidad
- Secretos mediante configuración externa; ejemplos sin credenciales reales.
- Permisos efectivos en C#, aunque Angular oculte acciones.
- Entradas externas validadas, límites de carga y paginación donde corresponda.
- No registrar tokens, contraseñas ni payloads sensibles.
- Probar reglas y escenarios de fallo relevantes: saldos, duplicados, concurrencia, permisos, cortes temporales y fallos de integración.
- Ejecutar las comprobaciones configuradas que correspondan al cambio; no añadir pruebas que solo reproduzcan detalles internos.
- Azure fue considerado como opción de despliegue. No imponer servicios ni publicar sin una tarea que lo autorice.



---

# AGENTS.md — StockMind / C# y ASP.NET Core

## Contexto y tecnologías
Este proyecto es la API de negocio de StockMind: ventas, compras, inventario, reposición e indicadores de una MYPE ficticia, con datos sintéticos para el piloto.

Tecnologías acordadas: C#, ASP.NET Core y MongoDB. Usar MongoDB.Driver como integración técnica recomendada; verificar el paquete y versión existentes. HttpClient/IHttpClientFactory y DI son mecanismos del framework para integrar Python y organizar dependencias.

Autenticación, OpenAPI y pruebas deben seguir los paquetes ya configurados. JWT Bearer, xUnit y FluentValidation son posibles opciones, no decisiones previas confirmadas; no instalarlas ni imponerlas por defecto. Para validación simple usar capacidades existentes antes de añadir una librería.

Revisar .sln/.slnx, .csproj, global.json, Directory.Build.props, NuGet, configuración, README y CI. Respetar TargetFramework y versiones; no importar decisiones de otros proyectos del usuario.

## Responsabilidades
- Exponer API a Angular; autenticar y autorizar cada operación.
- Mantener productos, ventas, compras, movimientos, conteos, parámetros y auditoría en MongoDB.
- Asegurar consistencia de stock e idempotencia.
- Calcular clasificación ABC, stock de seguridad, punto de reposición y EOQ cuando existan insumos.
- Coordinar solicitudes al servicio FastAPI; conservar pronósticos, versiones y resultados cuando lo requiera el diseño.
- Calcular indicadores operativos con definiciones explícitas.
- Las compras requieren revisión humana. Una aprobación no incrementa stock: el ingreso/recepción confirmado lo hace.
- No implementar entrenamiento de XGBoost ni duplicar sus métricas en C#.

## Arquitectura y dependencias
Aplicar separación lógica de capas sin obligar a crear cuatro proyectos nuevos. Si existe una arquitectura coherente, conservarla.

Referencia:
- Domain/: entidades, invariantes y reglas de inventario/reposición.
- Application/: casos de uso, coordinación, contratos/puertos y resultados.
- Infrastructure/: persistencia MongoDB, documentos/mapeos, cliente de FastAPI y proveedores externos.
- Api/ o Presentation/: endpoints/controladores, DTO HTTP, autorización y traducción de errores.

Si hay ensamblados separados: Domain no referencia ASP.NET/MongoDB; Application depende de Domain y define contratos de infraestructura; Infrastructure implementa esos contratos; Api compone dependencias. DTO HTTP no deben convertirse en dependencias del dominio.

Organizar dentro de las capas por módulos cuando aporte claridad. Controladores/endpoints delgados: validar el contrato, invocar un caso de uso y traducir su resultado. No incluir consultas MongoDB ni fórmulas de reposición en controladores.

No imponer CQRS, MediatR, AutoMapper, DDD completo, eventos distribuidos o GenericRepository. Extraer abstracciones para persistencia, predicción y otras variaciones reales, no una interfaz por cada clase. Evitar un UnitOfWork genérico que oculte las garantías de MongoDB.

## SOLID y código limpio
- SRP: separar validación, casos de uso, reglas, persistencia y transporte.
- OCP: estrategias para métodos de reposición realmente distintos.
- DIP: casos de uso dependen de contratos para MongoDB/FastAPI, inyectados por DI.
- ISP: contratos pequeños orientados a necesidades del módulo.
- LSP: implementaciones mantienen el comportamiento y significado del contrato.
- Métodos enfocados, nombres descriptivos, nullable reference types cuando sea compatible.
- async/await para I/O; propagar CancellationToken. Evitar .Result, .Wait() y async void.
- Para valores monetarios usar decimal y representación MongoDB compatible (por ejemplo Decimal128), con precisión/redondeo definidos.
- No silenciar excepciones; traducir errores esperados y conservar trazabilidad técnica sin exponer detalles internos.

## MongoDB y consistencia
- Encapsular acceso con MongoDB.Driver en Infrastructure. No filtrar BSON/ObjectId a DTO públicos.
- Reutilizar MongoClient mediante DI con ciclo de vida apropiado; no crear una conexión por request.
- Definir documentos e índices según consultas: SKU único dentro del alcance del catálogo, claves de idempotencia/documento y búsquedas por producto/fecha.
- Agregar paginación y proyecciones; evitar cargas completas de colecciones.
- No realizar “leer stock y luego reemplazar saldo” sin control de concurrencia.
- Una salida debe aplicar un decremento atómico condicionado al stock disponible, o una estrategia equivalente verificable.
- Si una operación afecta venta/compra, varios productos, movimientos y auditoría en documentos diferentes, resolver su atomicidad antes de considerarla implementada.
- Usar transacciones con sesiones cuando la topología MongoDB las soporte. Validar replica set/sharded cluster; no asumir transacciones multidocumento en una instancia standalone.
- Si no hay soporte transaccional, adaptar diseño/entorno o implementar una estrategia de recuperación verificable y documentar sus garantías. Nunca presentar escrituras independientes como una transacción.
- Un reintento no debe duplicar ventas, ingresos, ajustes ni movimientos. Definir clave de idempotencia, índice único, detección de payload distinto y recuperación de resultado.
- Las anulaciones se representan mediante estado y movimientos compensatorios auditables; no borrar el historial arbitrariamente.
- Coordinar cambios de saldo con persistencia del movimiento y estado de operación. Probar fallos parciales y ventas concurrentes.

## Reglas de negocio
- Venta válida: producto existente/activo, cantidad positiva, precio válido, permiso y stock suficiente según la política documentada.
- Compra/ingreso: documento sin duplicar, cantidad/costo válidos y actualización coherente de movimientos.
- Ajuste/conteo: permiso, motivo, responsable, fecha y saldo previo/nuevo; resolver conflictos con operaciones simultáneas.
- ABC: definir periodo, criterio económico y umbrales configurables. Mantener trazabilidad; en evaluación histórica utilizar únicamente datos disponibles al corte.
- Stock de seguridad: registrar método, nivel de servicio y datos usados; no asumir una distribución o desviación sin información.
- ROP: demanda durante el plazo + stock de seguridad bajo las hipótesis documentadas; usar unidades temporales compatibles.
- EOQ: aplicar sqrt(2*D*S/H) únicamente con demanda anual D, costo por pedido S y costo anual de mantener una unidad H > 0, bajo supuestos explícitos.
- No confundir EOQ con cantidad obligatoria de compra; considerar existencias, pedidos pendientes, lotes y restricciones del caso.
- Si falta un insumo, devolver estado no calculable y campos pendientes, sin inventar valores.
- Definir transiciones de sugerencias: pendiente, aprobada/rechazada y ejecutada si aplica; registrar quién/por qué y evitar aprobación doble.

## Contrato con FastAPI
- Utilizar cliente tipado/encapsulado, URL por configuración, timeout y CancellationToken.
- Angular no llama a Python. Las credenciales del servicio permanecen en el backend.
- No llamar a FastAPI dentro de una transacción de inventario.
- No entrenar en cada venta ni bloquear una operación de stock para recalcular un modelo.
- Entrenamientos prolongados deben tener ejecución y estado controlados; no introducir un sistema distribuido de tareas si el piloto no lo necesita.
- Validar solicitud/respuesta: SKU, corte, horizonte, semanas, unidades, método, versión, métricas y estado.
- Horizonte previsto: cuatro semanas. Método principal: XGBoost Regressor; referencia: promedio móvil de cuatro semanas.
- Métricas principales: MAE, RMSE, WAPE. Null con motivo para métricas no calculables; nunca NaN/infinito.
- Registrar el resultado y su vigencia. Si Python falla, no inventar un pronóstico; indicar indisponibilidad o utilizar una contingencia autorizada y identificada.
- Reintentar solo lecturas o operaciones con idempotencia. Una solicitud de entrenamiento requiere identidad para no crear trabajos duplicados.

## API y seguridad
- Mantener contratos existentes y documentar OpenAPI, validación, paginación y errores.
- No exponer documentos de persistencia como contrato HTTP por comodidad.
- Autorización en servidor por rol/política para administrador, vendedor, inventario y compras, según matriz implementada.
- Separar 400/401/403/404/409 y fallos externos según el contrato; usar ProblemDetails o el formato coherente existente.
- No aceptar usuario responsable, precio autorizado o permisos únicamente porque el cliente los envíe; validar contra contexto y reglas.
- Configuración y secretos externos; .env/appsettings de ejemplo sin valores reales.
- No registrar credenciales ni stack traces en respuestas. CORS explícito por entorno.
- Conservar instantes en UTC y usar zona de negocio configurada para cortes semanales. En JSON, fechas ISO 8601 e IDs como cadenas.

## Indicadores, pruebas y flujo Codex
- Indicadores operativos: quiebres, excedente, exactitud y minutos de planificación; documentar denominadores, cero/faltantes y periodos.
- No inferir oportunidades de demanda perdidas solo desde ventas atendidas. Para evaluar quiebres, necesitar eventos de demanda o datos sintéticos que los registren.
- No declarar mejora real de una empresa a partir de una simulación.
- Probar invariantes y casos de uso con el framework existente; pruebas de integración MongoDB para atomicidad/idempotencia/concurrencia que los mocks no verifican.
- Cubrir permisos, duplicados, stock insuficiente, ajuste auditado, costos faltantes, timeout Python y respuestas inválidas.
- Revisar instrucciones de Angular/Python cuando se afecten sus contratos.
- Para refactorizar arquitectura, presentar diagnóstico y pasos breves, después implementar si está autorizado.
- Ejecutar dotnet build, dotnet test y herramientas configuradas según el alcance; si no existe proyecto de pruebas, indicarlo. Respetar restauración de paquetes y CI.
- Informar cambios y evidencia. No confundir compilación exitosa con integración probada.

Referencias: https://learn.microsoft.com/aspnet/core/ · https://www.mongodb.com/docs/drivers/csharp/current/



# AGENT.md — SimpleEcom

Guía de referencia para agentes de IA y desarrolladores que trabajan en este repositorio.
Léela antes de crear o modificar código: define la tecnología, la arquitectura y los patrones obligatorios.

---

## 1. Visión general

SimpleEcom es un ecommerce dividido en backend y frontend:

| Proyecto | Tipo | Responsabilidad |
|---|---|---|
| `SimpleEcom.Shared` | Class library | Capa transversal: bloques base de DDD, patrón Result, contratos (DTOs) y cliente HTTP de la Api. |
| `SimpleEcom.Api` | ASP.NET Core Minimal API | Lógica de negocio del ecommerce. Contiene el **dominio** y las **features** (vertical slice). |
| `SimpleEcom.UI.Web` | Blazor Web App (Interactive Server) | Tienda para los compradores. |
| `SimpleEcom.UI.Web.Admin` | Blazor Web App (Interactive Server) | Panel de administración del ecommerce. |
| `SimpleEcom.Api.Test` | xUnit | Tests del dominio y de los handlers de la Api. |
| `SimpleEcom.UI.Web.Admin.Test` | xUnit | Tests de los ViewModels del panel de administración (sin bUnit, con una Api HTTP simulada). |
| `SimpleEcom.UI.Web.Test` | xUnit | Tests de los ViewModels y reglas de presentación de la tienda (mismo enfoque que el Admin). |

Las UIs **nunca** acceden a datos ni a lógica de negocio directamente: todo pasa por `SimpleEcom.Api` vía HTTP.

---

## 2. Tecnología

- **.NET 11** (`net11.0`), SDK fijado en [global.json](global.json) (`11.0.100`, `rollForward: latestFeature`).
- **C#** con `Nullable` e `ImplicitUsings` habilitados.
- **Solución**: formato `.slnx` → [SimpleEcom.slnx](SimpleEcom.slnx).
- **Api**: Minimal APIs, `Microsoft.AspNetCore.OpenApi` (documento en `/openapi/v1.json` en Development), `ProblemDetails`.
- **UI**: Blazor Server (`AddInteractiveServerComponents`), Bootstrap del template.
- **HTTP**: `IHttpClientFactory` con cliente tipado (`AddHttpClient<ProductsApiClient>`).
- **Tests**: xUnit **2.x** (`xunit 2.9.3`) — no usar APIs de xUnit v3 como `TestContext.Current`.
- **Persistencia**: EF Core 11 + SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, base `SimpleEcom`). Migraciones con la herramienta local `dotnet-ef`. `InMemoryProductRepository` se conserva solo como doble para tests.
- **Sin MediatR, AutoMapper ni FluentValidation**: los handlers son clases simples, el mapeo es manual y la validación vive en el dominio. No añadir estas librerías sin acordarlo.

---

## 3. Estructura de la solución

```
SimpleEcom.slnx
global.json
src/
├── SimpleEcom.Shared/
│   ├── Domain/          Entity<TId>, AggregateRoot<TId>, ValueObject, IDomainEvent
│   ├── Results/         Result, Result<T>, Error, ErrorType
│   ├── Contracts/       DTOs de request/response por contexto (Products/, Branches/…)
│   └── Http/            Clientes tipados de la Api (ProductsApiClient, BranchesApiClient)
├── SimpleEcom.Api/
│   ├── Common/
│   │   ├── Endpoints/   IEndpoint + auto-registro (AddEndpoints, AddFeatureHandlers, MapEndpoints)
│   │   └── Http/        ResultHttpExtensions (Result → ProblemDetails)
│   ├── Domain/
│   │   ├── Products/    Product, ProductId, Money, ProductErrors, IProductRepository, Events/
│   │   └── Branches/    Branch (sucursal), BranchId, BranchErrors, IBranchRepository, Events/
│   ├── Features/
│   │   ├── Branches/    CRUD de sucursales (mismo patrón que Products)
│   │   └── Products/
│   │       ├── CreateProduct/CreateProduct.cs
│   │       ├── GetProductById/GetProductById.cs
│   │       ├── GetProducts/GetProducts.cs
│   │       └── ProductMappings.cs
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── SimpleEcomDbContext.cs
│   │   │   ├── Configurations/   IEntityTypeConfiguration por agregado
│   │   │   ├── Migrations/       generadas con dotnet-ef (no editar a mano)
│   │   │   ├── SqlProductRepository.cs / SqlBranchRepository.cs
│   │   │   ├── InMemoryProductRepository.cs / InMemoryBranchRepository.cs   (solo tests)
│   │   │   └── DatabaseInitializer.cs         migra + carga datos de ejemplo y la sucursal principal
│   │   └── DependencyInjection.cs
│   └── Program.cs
├── SimpleEcom.UI.Web/
│   ├── Common/                 ViewModelBase, AsyncOperation, AddViewModels (copia del Admin)
│   ├── Components/
│   │   ├── Layout/             MainLayout: cabecera con buscador, navegación y pie
│   │   └── Pages/              Home (portada), Error, NotFound
│   └── Features/
│       ├── Catalog/            catálogo, ficha de producto, destacados: ViewModels + componentes .razor (+ CatalogSeo)
│       ├── Seo/                robots.txt, sitemap.xml y códigos HTTP (404/503) de las páginas
│       └── Branches/           barra "Comprando en…", página /branches y endpoint POST que guarda la sucursal en cookie
└── SimpleEcom.UI.Web.Admin/
    ├── Common/
    │   ├── ViewModelBase.cs        evento StateChanged + Notify()
    │   ├── AsyncOperation.cs       IsBusy + Error + RunAsync(...) para llamadas a la Api
    │   └── ViewModelExtensions.cs  AddViewModels(): registra los ViewModels como Transient
    ├── Components/
    │   ├── Layout/                 MainLayout, NavMenu
    │   ├── Pages/                  Home, Error, NotFound (páginas sin lógica)
    │   └── Shared/                 BusyButton y otros componentes reutilizables
    └── Features/
        ├── Products/               módulo completo: ViewModels + FormModel + páginas .razor
        └── Branches/               módulo de sucursales (/branches), mismo reparto que Products
tests/
├── SimpleEcom.Api.Test/
│   ├── Domain/          Tests de agregados y objetos de valor
│   └── Features/        Tests de handlers
├── SimpleEcom.UI.Web.Admin.Test/
│   ├── FakeApiHandler.cs   Api simulada a nivel HTTP (rutas → respuestas)
│   └── Features/           Tests de ViewModels
└── SimpleEcom.UI.Web.Test/
    ├── FakeApiHandler.cs   copia del de Admin.Test
    └── Features/Catalog/   Tests de ViewModels y de CatalogDisplay
```

### Reglas de dependencia

```
UI.Web ──────┐
UI.Web.Admin ┼──► Shared ◄── Api ◄── Api.Test
             │
             └──(HTTP)──► Api
```

- `Shared` no depende de ningún otro proyecto y no contiene lógica de negocio de un contexto concreto.
- `Api` depende solo de `Shared`.
- Las UIs dependen solo de `Shared` (contratos + cliente HTTP). **Nunca** referencian `Api`.
- Dentro de `Api`: `Features` → `Domain`; `Infrastructure` → `Domain`. `Domain` no depende de `Features` ni de `Infrastructure`, ni de ASP.NET Core.

---

## 4. Arquitectura

### 4.1 DDD (dominio)

El dominio vive en `SimpleEcom.Api/Domain/<Contexto>/`. Bloques base en `SimpleEcom.Shared/Domain`:

- **Entidad** (`Entity<TId>`): igualdad por `Id`.
- **Raíz de agregado** (`AggregateRoot<TId>`): frontera de consistencia; acumula eventos con `Raise(...)`.
- **Objeto de valor** (`ValueObject`): inmutable, igualdad por `GetEqualityComponents()`.
- **Evento de dominio** (`IDomainEvent`): record en `Domain/<Contexto>/Events/`, nombre en pasado + `DomainEvent` (`ProductCreatedDomainEvent`).
- **Id fuertemente tipado**: `readonly record struct XxxId(Guid Value)` con `New()` usando `Guid.CreateVersion7()`.
- **Repositorio**: interfaz en `Domain/<Contexto>/IXxxRepository.cs`, implementación en `Infrastructure/Persistence/`. Un repositorio por agregado.
- **Errores de dominio**: clase estática `XxxErrors` con `Error` predefinidos (`Product.NameRequired`, …).

Reglas del agregado:

- Constructor **privado**; creación mediante factoría estática `Create(...)` que devuelve `Result<T>`.
- Setters `private set`; los cambios de estado se hacen con métodos de negocio (`DecreaseStock`, `ChangePrice`, `Deactivate`).
- Las invariantes se validan **dentro** del agregado/objeto de valor, no en el endpoint.
- Usar `TimeProvider` (inyectado) para fechas; nunca `DateTime.Now`/`UtcNow`.

### 4.2 Vertical Slice (Api)

Cada caso de uso es una carpeta en `Features/<Contexto>/<CasoDeUso>/` con **un archivo** `<CasoDeUso>.cs` que contiene:

1. **Handler** — `public sealed class <CasoDeUso>Handler` con método `HandleAsync(...)` que devuelve `Result<T>` (o el valor directo si no puede fallar). Recibe dependencias por constructor primario.
2. **Endpoint** — `public sealed class <CasoDeUso>Endpoint : IEndpoint` que mapea la ruta y delega en el handler.

El registro es **automático** por reflexión (`Program.cs`):

- `AddFeatureHandlers`: registra como `Scoped` toda clase cuyo nombre termine en `Handler` y cuyo namespace contenga `.Features.`.
- `AddEndpoints` / `MapEndpoints`: registra y mapea todas las implementaciones de `IEndpoint` bajo el grupo `/api`.

Plantilla:

```csharp
namespace SimpleEcom.Api.Features.Products.GetProductById;

public sealed class GetProductByIdHandler(IProductRepository repository)
{
    public async Task<Result<ProductResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(id);
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        return product is null ? ProductErrors.NotFound(productId) : product.ToResponse();
    }
}

public sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("products/{id:guid}", async (Guid id, GetProductByIdHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .WithName("GetProductById")
            .WithTags("Products")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}
```

Reglas del slice:

- Rutas **relativas** sin `/api` (`"products"`, `"products/{id:guid}"`); el prefijo lo pone el grupo.
- Siempre `.WithName(...)`, `.WithTags(...)`, `.Produces...` y `.ProducesProblem(...)` para OpenAPI.
- El endpoint no contiene lógica de negocio: deserializa, llama al handler y traduce el `Result`.
- Código compartido entre slices del mismo contexto (p. ej. mapeos) va en `Features/<Contexto>/` (`ProductMappings.cs`), **no** en `Common`.
- Preferir duplicación pequeña entre slices antes que abstracciones genéricas prematuras.

### 4.3 Persistencia (EF Core + SQL Server LocalDB)

- Cadena de conexión en `appsettings.json` → `ConnectionStrings:SimpleEcom`.
- `DatabaseInitializer.InitializeAsync` se ejecuta al arrancar la Api: aplica `MigrateAsync()`, inserta productos de ejemplo si `Products` está vacía y crea la sucursal `PRINCIPAL` si `Branches` está vacía (cada tabla se comprueba por separado). No hace falta crear la base a mano.
- Mapeo en `Infrastructure/Persistence/Configurations/<Agregado>Configuration.cs`:
  - Id tipado → `HasConversion(id => id.Value, v => new XxxId(v))`.
  - Objetos de valor → `ComplexProperty` (columnas en la misma tabla). `Money` se guarda como `Price` + `Currency` (texto).
  - `Ignore(a => a.DomainEvents)`: los eventos no se persisten.
- El agregado necesita un constructor privado sin parámetros para EF (`private Product() : base(default)`), marcado con comentario; nunca usarlo desde código de aplicación.
- Cada método de escritura del repositorio llama a `SaveChangesAsync`. No hay Unit of Work todavía; se añadirá cuando un caso de uso toque más de un agregado.
- Migraciones (desde la raíz del repo):

```bash
dotnet ef migrations add <Nombre> --project src/SimpleEcom.Api --output-dir Infrastructure/Persistence/Migrations
dotnet ef database update --project src/SimpleEcom.Api
```

### 4.4 UIs (Blazor Server)

- Consumen la Api a través de los clientes tipados de `SimpleEcom.Shared/Http`.
- URL de la Api en `appsettings.json` → `Api:BaseUrl` (por defecto `https://localhost:7221/`).
- Los componentes trabajan con los DTOs de `SimpleEcom.Shared/Contracts`, nunca con entidades de dominio.
- `UI.Web` = experiencia del comprador; `UI.Web.Admin` = gestión (productos, pedidos, etc.). No mezclar responsabilidades.

### 4.5 Admin: MVVM ligero por funcionalidad

Cada módulo del panel vive en `Features/<Contexto>/` y sigue este reparto:

| Pieza | Responsabilidad |
|---|---|
| `XxxViewModel : ViewModelBase` | Todo el estado y la lógica de una pantalla: datos, filtros, operaciones contra la Api, mensajes. Sin dependencias de Blazor (ni `NavigationManager`, ni `IJSRuntime`). |
| `XxxFormModel` | POCO con DataAnnotations para el `EditForm`; convierte a/desde los DTOs de `Shared/Contracts`. |
| Página `.razor` | Solo renderiza y delega: `@inject XxxViewModel Vm`, se suscribe a `Vm.StateChanged += StateHasChanged` en `OnInitialized` y se desuscribe en `Dispose`. La navegación tras una operación la decide la página. |
| `AsyncOperation` | Una por operación visible (`Load`, `Save`, `RowAction`…). Expone `IsBusy` y `Error`; `RunAsync` pone busy, ejecuta, captura el error del `Result` y notifica. No repetir ese patrón a mano. |

Reglas:

- Los ViewModels se registran automáticamente como **Transient** (`AddViewModels` en `Program.cs`): una instancia por página. No usar Scoped (en Blazor Server dura todo el circuito y el estado sobrevive a la navegación).
- Mientras una operación está en curso la página bloquea las acciones (botones con `BusyButton`, enlaces con la clase `disabled` + `aria-disabled`) y el ViewModel ignora llamadas repetidas (`if (IsAnyBusy) return;`).
- Los mensajes de error salen de `AsyncOperation.Error`, que ya contiene la descripción del `ProblemDetails` de la Api. No capturar excepciones en la página.
- Los ViewModels se prueban en `SimpleEcom.UI.Web.Admin.Test` con `FakeApiHandler` (rutas HTTP simuladas) y el `ProductsApiClient` real; no hace falta bUnit.
- `Components/` queda para layout, páginas sin lógica (Home, Error, NotFound) y componentes compartidos (`Shared/`). Nada de negocio ahí.

### 4.6 Tienda pública (UI.Web)

Rutas: `/` (portada con destacados), `/products` (catálogo), `/products/{id}` (ficha), `/branches` (elegir sucursal), `/robots.txt`, `/sitemap.xml`.

- **Renderizado estático en servidor, sin circuito interactivo y SIN `[StreamRendering]` en las páginas**: el servidor espera a la Api y envía el HTML completo en la primera respuesta. Con streaming el contenido llegaba en bloques `<template>` aplicados por JavaScript, el `<title>` inicial era genérico y no se podía devolver un 404 real (las cabeceras ya se habían enviado). La espera es de decenas de ms. Solo se permite streaming en componentes secundarios que no aporten al SEO (p. ej. `BranchBar`).
- **SEO obligatorio en cada página pública**: `<PageTitle>` + `<SeoMeta>` (`Components/Seo/SeoMeta.razor`: description, canonical absoluta, robots, Open Graph y JSON-LD opcional).
  - La canonical no lleva parámetros de búsqueda ni orden; los resultados de búsqueda (`?q=`) van con `noindex, follow`.
  - La ficha publica JSON-LD `Product` con `Offer` (precio, moneda, disponibilidad) generado por `CatalogSeo.ProductJsonLd` (System.Text.Json escapa `<`, así que no puede cerrar el `<script>`).
  - 404 (producto inexistente o inactivo): `NavigationManager.NotFound()`, que pinta `Pages/NotFound` (con mensaje propio en `products/…` y `noindex`). **Nunca** `Response.StatusCode = 404` a mano: Blazor descarta el HTML de la página y devuelve un 404 vacío, o corta la conexión si `BranchBar` ya empezó el streaming.
  - 503 (Api caída): `HttpStatus.SetUnavailable` (`Features/Seo/`). Desactiva `UseStatusCodePagesWithReExecute` para esa petición, así se conserva el aviso de la página. Solo funciona sin streaming.
  - Al añadir una página pública nueva, añadir su ruta a `SeoEndpoints.StaticPaths` (sitemap).
- Los filtros del catálogo viajan en la URL (`?q=`, `?sort=`, `?available=true`) mediante formularios `GET`; las páginas los leen con `[SupplyParameterFromQuery]` y los pasan al ViewModel. No hay `@onclick` ni `@bind` en la tienda.
- Mismo reparto MVVM que el Admin: ViewModels en `Features/<Contexto>/`, registrados como Transient con `AddViewModels`. `Common/` es una copia deliberada de la del Admin (duplicación pequeña antes que mover estado de UI a `Shared`).
- La tienda **solo muestra productos activos**: pide `isActive=true` y vuelve a filtrar en el ViewModel. Un producto inactivo o inexistente se presenta como "no disponible", no como error.
- Reglas de presentación (formato de precio por moneda, umbral de "últimas unidades", imagen de reserva por SKU) en `Features/Catalog/CatalogDisplay.cs`. Los órdenes válidos en `CatalogSort.cs`.
- Si se necesita interactividad (p. ej. el carrito), usar islas con `@rendermode InteractiveServer` en el componente concreto, no en toda la página. El modo interactivo ya está registrado en `Program.cs`.
- Estilos propios en `wwwroot/app.css`, con tokens CSS en `:root` sobre Bootstrap.
- **Sucursal del comprador** (`Features/Branches/`):
  - Se guarda en una cookie con el Id de la sucursal (HttpOnly, SameSite=Lax, Path=/, 1 año): `__Host-simpleecom-branch` por HTTPS (Secure) y `simpleecom-branch` solo por HTTP local (el prefijo `__Host-` exige Secure). Leerla con `BranchCookie.Read(HttpContext)` usando `[CascadingParameter] HttpContext`.
  - Se elige con un formulario `POST` normal (sin `data-enhance`, con `<AntiforgeryToken />`) a `branches/select` (`BranchSelectionEndpoint`): valida en la Api que la sucursal exista y esté activa, escribe la cookie y redirige al `returnUrl` solo si es una ruta local (`SafeReturnUrl`). Es un endpoint y no un componente porque las páginas se envían en streaming y la cookie debe escribirse antes de empezar la respuesta. `Program.cs` llama a `UseAntiforgery()`.
  - `BranchBar` (en `MainLayout`, con `[StreamRendering]`) muestra "Comprando en X · Cambiar" o invita a elegir. No bloquea la navegación. Si solo hay una sucursal activa se asume esa; una cookie de una sucursal inactiva o inexistente cuenta como "sin elegir".
  - Pendiente (fases 2–3): usar la sucursal elegida para pedir precios y disponibilidad por sucursal al catálogo y a la ficha.

---

## 5. Patrones y convenciones

### Patrón Result (obligatorio para errores de negocio)

- Los errores de negocio se devuelven como `Result`/`Result<T>` con un `Error`; **no** se lanzan excepciones para el flujo de negocio.
- Las excepciones quedan para fallos inesperados (las captura `UseExceptionHandler`).
- Conversiones implícitas: `return valor;` → éxito, `return error;` → fallo.
- `Error` tiene `Code` (`"<Contexto>.<Motivo>"`), `Description` (en español, orientada al usuario) y `ErrorType`.
- Mapeo HTTP (`ResultHttpExtensions.ToProblem`):

| ErrorType | HTTP |
|---|---|
| `Validation` | 400 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` | 500 |

### Contratos (DTOs)

- `sealed record` en `SimpleEcom.Shared/Contracts/<Contexto>/`.
- Sufijos: `XxxRequest` para entrada, `XxxResponse` para salida.
- Solo tipos primitivos/serializables (`Guid`, `decimal`, `string`…) y enums de `Shared/Contracts`; nunca tipos de dominio.
- **Moneda**: enum `Currency` en `Shared/Contracts/Currency.cs` (ISO 4217, por defecto `USD`). Se serializa como texto en JSON. Para admitir otra moneda, añadir el miembro al enum; no usar `string` para monedas.
- Mapeo dominio → DTO con métodos de extensión `ToResponse()` en `Features/<Contexto>/XxxMappings.cs`.

### Estilo de código

- `sealed` por defecto en clases concretas.
- Constructores primarios para inyección de dependencias.
- Namespaces de archivo (`namespace X;`), que coinciden con la ruta de carpetas.
- Todos los métodos async aceptan `CancellationToken` y lo propagan.
- Expresiones de colección (`[]`) y pattern matching cuando aporten claridad.
- Código (identificadores) en **inglés**; mensajes de error, comentarios XML y documentación en **español**.
- Archivos de texto en UTF-8.

---

## 6. Tests

- Proyecto `tests/SimpleEcom.Api.Test`, xUnit 2.x, referencia `Api` y `Shared`.
- Proyecto `tests/SimpleEcom.UI.Web.Admin.Test`, xUnit 2.x: tests de ViewModels. `FakeApiHandler.On(método, ruta, estado, cuerpo)` define las respuestas; `Requests` registra las llamadas en orden para afirmar sobre ellas.
- Organización espejo: `Domain/<Contexto>/XxxTests.cs`, `Features/<Contexto>/XxxHandlerTests.cs`.
- Nombre: `Metodo_Escenario_ResultadoEsperado`.
- Proyecto `tests/SimpleEcom.UI.Web.Test`, xUnit 2.x: tests de los ViewModels de la tienda y de `CatalogDisplay`, con el mismo `FakeApiHandler`.
- Los handlers se prueban instanciándolos directamente con `InMemoryProductRepository` (o fakes) y `TimeProvider.System` / un `FakeTimeProvider`.
- Toda regla de negocio nueva en el dominio debe tener su test.

---

## 7. Comandos

```bash
dotnet build SimpleEcom.slnx
dotnet test  SimpleEcom.slnx
dotnet run --project src/SimpleEcom.Api
dotnet run --project src/SimpleEcom.UI.Web
dotnet run --project src/SimpleEcom.UI.Web.Admin
```

Puertos de desarrollo (`Properties/launchSettings.json`):

| Proyecto | HTTPS | HTTP |
|---|---|---|
| Api | `https://localhost:7221` | `http://localhost:5013` |
| UI.Web | `https://localhost:7171` | `http://localhost:5296` |
| UI.Web.Admin | `https://localhost:7146` | `http://localhost:5283` |

Peticiones de ejemplo: [src/SimpleEcom.Api/SimpleEcom.Api.http](src/SimpleEcom.Api/SimpleEcom.Api.http).

Validación de extremo a extremo (compila, tests, arranca las tres aplicaciones, comprueba Api, tienda —SEO, 404/503,
sitemap, móvil, elegir sucursal— y Admin con CRUD real en Edge mediante Playwright, y las detiene):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/site-validation/run.ps1   # -SkipTests, -UseRunning
```

Resultados en `%TEMP%\simpleecom-validation\` (`summary.txt`, `report.json`, logs y capturas). Los datos de prueba
llevan el prefijo `QAVAL` y se borran al terminar. En Claude Code, el agente `site-validator`
(`.claude/agents/`) lo ejecuta e interpreta los fallos. Si cambias un texto o un `id` del Admin o de la tienda,
actualiza `tools/site-validation/validate.mjs`.

> Nota del entorno local: si los tests fallan con `An Application Control policy has blocked this file (0x800711C7)`, es una política de Windows (Smart App Control/WDAC) bloqueando DLLs sin firmar, no un error de código. Compilar fuera de la carpeta de OneDrive suele evitarlo: `dotnet test SimpleEcom.slnx --artifacts-path %TEMP%\simpleecom-out` (igual con `dotnet run`).

---

## 8. Checklist para añadir funcionalidad

**Nuevo caso de uso en un contexto existente**
1. Si hace falta, añadir comportamiento al agregado en `Domain/<Contexto>/` (+ errores en `XxxErrors`).
2. Crear DTOs en `Shared/Contracts/<Contexto>/`.
3. Crear `Features/<Contexto>/<Caso>/<Caso>.cs` con `Handler` + `Endpoint`.
4. Añadir el método correspondiente al cliente en `Shared/Http/`.
5. Añadir tests de dominio y de handler.
6. Añadir la petición de ejemplo al `.http`.

**Nuevo contexto (p. ej. Orders, Customers, Carts)**
1. `Domain/<Contexto>/`: agregado, Id tipado, objetos de valor, `XxxErrors`, `IXxxRepository`, `Events/`.
2. `Infrastructure/Persistence/`: `Configurations/<Agregado>Configuration.cs`, `DbSet` en `SimpleEcomDbContext`, repositorio SQL, registro en `DependencyInjection.cs` y nueva migración con `dotnet ef migrations add`.
3. `Features/<Contexto>/`: slices + `XxxMappings.cs`.
4. `Shared/Contracts/<Contexto>/` y `Shared/Http/XxxApiClient.cs`; registrar el cliente en las UIs que lo usen.

**Nuevo módulo en el Admin (p. ej. Orders)**
1. `Features/<Contexto>/`: `XxxListViewModel`, `XxxEditorViewModel` (si hay alta/edición), `XxxFormModel`, páginas `.razor`.
2. Una `AsyncOperation` por operación visible; `BusyButton` en cada acción.
3. Enlace en `Components/Layout/NavMenu.razor` y tarjeta en `Home.razor`.
4. Tests de los ViewModels en `tests/SimpleEcom.UI.Web.Admin.Test/Features/<Contexto>/`.
   El registro en DI es automático (`AddViewModels`).

---

## 9. Lo que NO hay que hacer

- Referenciar `SimpleEcom.Api` desde las UIs.
- Poner lógica de negocio en endpoints, componentes Blazor o en `Shared`.
- Poner lógica de pantalla en el `@code` de una página del Admin: va en su ViewModel.
- Inyectar `NavigationManager` o `IJSRuntime` en un ViewModel.
- Exponer entidades de dominio en respuestas HTTP.
- Lanzar excepciones para errores de validación o de negocio.
- Crear capas horizontales genéricas (`Services/`, `Managers/`, `Helpers/`) dentro de la Api: el código va en su slice.
- Usar `DateTime.Now` / `DateTime.UtcNow` en lugar de `TimeProvider`.
- Añadir paquetes NuGet nuevos sin justificarlo.
- Cambiar el `TargetFramework` o el SDK de `global.json` sin acordarlo.

---

## 10. Estado actual y próximos pasos

- ✅ Solución, proyectos y referencias creados en .NET 11.
- ✅ Contexto **Products** en la Api: crear, listar (con filtros `search` e `isActive`), obtener por Id, editar, activar/desactivar y eliminar.
- ✅ Módulo de administración de productos en `UI.Web.Admin` (`/products`, `/products/new`, `/products/{id}/edit`), con MVVM ligero (`Features/Products/`) y tests de ViewModels.
- ✅ Persistencia con EF Core 11 sobre SQL Server LocalDB, migración `InitialCreate` y carga de datos de ejemplo al arrancar.
- ✅ Contexto **Branches** (sucursales): CRUD en la Api (`/api/branches`), módulo en el Admin (`/branches`), migración `AddBranches` y sucursal `PRINCIPAL` inicial. Cada sucursal ofrece retiro en tienda, envío a domicilio o ambos (al menos uno).
- ⏳ **Multi-sucursal** (diseño acordado), siguientes fases:
  2. Precios y surtido: `Product.Price` → `BasePrice`; `BranchProduct` (IsListed, PriceOverride). Precio efectivo = override ?? base. Una sola moneda (USD).
  3. Inventario: `BranchStock` (OnHand, Reserved, RowVersion) + `StockMovement` (kardex: entrada, venta, ajuste, traslado). Migrar `Product.Stock` a la sucursal principal y quitarlo de `Product`. Al existir stock, impedir borrar una sucursal con movimientos (desactivarla).
  4. Tienda: ✅ el cliente elige su sucursal (barra + `/branches` + cookie). ⏳ El catálogo mostrará precio y disponibilidad de esa sucursal cuando existan las fases 2 y 3.
  5. Pedidos: una sola sucursal de venta (fija el precio). Si no tiene stock se surte desde otra de forma transparente: en retiro hay traslado y se avisa "listo para retirar en X días"; en envío sale directo de la sucursal de origen.
- ⏳ Despacho de eventos de dominio (hoy se acumulan en el agregado pero no se publican).
- ⏳ Contextos Customers, Carts y Orders.
- ✅ Tienda pública en `UI.Web`: portada, catálogo con búsqueda/orden/disponibilidad y ficha de producto, con tests de ViewModels.
- ⏳ Carrito y checkout en la tienda (requiere los contextos Carts y Orders en la Api; hoy el botón "Añadir al carrito" está deshabilitado).
- ⏳ Autenticación y autorización (compradores y administradores).

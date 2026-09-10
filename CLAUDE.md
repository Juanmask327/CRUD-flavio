# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project context

This is a course assignment ("Taller Práctico No. 3", Arquitectura de Software / Ingeniería de Software,
Uniempresarial): a minimal CRUD API for `Usuario` built with .NET 8 and SQL Server via EF Core, demonstrating a
layered architecture (Controller → Service → Repository → DbContext → SQL Server).

**Note:** this repo was authored without a local .NET 8 SDK or SQL Server instance, so nothing here has actually
been compiled or run yet. Before trusting `dotnet build`/`dotnet test` to pass, actually run them — don't assume
the note in README.md is stale.

## Commands

All commands run from the relevant project folder (not the repo root, which has no top-level `.sln`).

```bash
# Restore + build the API
cd UsuarioApi
dotnet restore
dotnet build

# EF Core migrations (requires `dotnet tool install --global dotnet-ef` once)
dotnet ef migrations add <Name>
dotnet ef database update

# Run the API (Swagger UI at the printed https://localhost:XXXX/swagger, dev only)
dotnet run
```

```bash
# Run all tests
cd UsuarioApi.Tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~UsuarioServiceTests.CrearAsync_CorreoYaRegistrado_LanzaExcepcion"
```

If migrations can't be applied (e.g., no DB-create permission), run `scripts/001_CrearBaseDeDatos.sql` directly in
SSMS/Azure Data Studio — it creates the same `SoftwareArchitectureDb` database and `Usuarios` table by hand. Keep
this script in sync with the EF model if the entity changes.

The SQL Server connection string lives in `UsuarioApi/appsettings.json` (`ConnectionStrings:DefaultConnection`,
defaults to `Server=localhost` with `Trusted_Connection=True`) — adjust it for your local SQL Server instance.

## Architecture

Strict one-way layering, enforced by depending on interfaces rather than concrete types (DIP):

```
UsuariosController → IUsuarioService (UsuarioService) → IUsuarioRepository (UsuarioRepository) → AppDbContext → SQL Server
```

- **`Controllers/UsuariosController.cs`** — HTTP only. Maps requests to service calls and service results to
  `IActionResult` (`Ok`/`NotFound`/`CreatedAtAction`/`NoContent`). No business logic, no EF Core references.
- **`Services/UsuarioService.cs`** — business rules live here exclusively, notably **unique-email enforcement**
  on both create and update (checked via `IUsuarioRepository.ObtenerPorCorreoAsync`, comparing normalized
  lowercase emails). Violations throw `InvalidOperationException`, which the middleware turns into `400`. Also
  owns pagination defaults/clamping (`pagina` min 1, `tamanoPagina` clamped to 1–100, default 10) and
  entity↔DTO mapping (`Mapear`).
- **`Repositories/UsuarioRepository.cs`** — the only place with EF Core query logic (`Where`/`Skip`/`Take`,
  `AsNoTracking` for reads). No business rules — it only knows *how* to query, not *what* a valid state is.
- **`Data/AppDbContext.cs`** — single `DbSet<Usuario>`, otherwise unconfigured (no fluent API / custom mappings).
- **`DTOs/UsuarioDtos.cs`** — `CrearUsuarioDto`, `ActualizarUsuarioDto` (input, with `[Required]`/`[MinLength]`/
  `[EmailAddress]`/`[Phone]` data annotations — validated automatically by `[ApiController]`'s automatic
  `ModelState` validation before the controller method runs, so no manual validation code is needed), plus
  `UsuarioResponseDto` (output) and `PaginacionResponseDto<T>` (generic paged envelope: `Items`, `PaginaActual`,
  `TamanoPagina`, `TotalRegistros`, `TotalPaginas`). DTOs exist so the API contract can evolve independently of
  the `Usuario` entity and to prevent over-posting (e.g. clients can't set `Id` or `FechaCreacion`).
- **`Middleware/ExceptionHandlingMiddleware.cs`** — global exception handler registered first in the
  `Program.cs` pipeline. Translates `InvalidOperationException` (business-rule violation) → `400`, anything
  else → `500`, both as `{ "mensaje": "..." }` JSON. This is why controller actions have no try/catch.
- **`Program.cs`** — composition root: DI registrations (`AddScoped` for repository/service), `AddDbContext` with
  `UseSqlServer`, Swagger (dev-only), middleware pipeline order.

When adding a new business rule, put it in the Service layer, not the Controller or Repository. When adding a
new query shape (filter/sort), add a method to `IUsuarioRepository`/`UsuarioRepository` rather than exposing
`IQueryable` upward.

## Testing conventions

`UsuarioApi.Tests` uses xUnit + Moq, unit-testing `UsuarioService` in isolation by mocking `IUsuarioRepository`
(never a real `AppDbContext`/SQL Server). This is only possible because the service depends on the repository
interface. Follow the existing naming pattern `Metodo_Escenario_ResultadoEsperado` (Spanish, matching the rest of
the codebase) for new test methods.

## Conventions

- All identifiers, messages, and route/query parameter names are in **Spanish** (`Nombre`, `Correo`, `busqueda`,
  `pagina`, `tamanoPagina`) — match this in any new code rather than mixing in English names.
- Emails are always normalized with `.Trim().ToLower()` before comparison or storage — do this anywhere else an
  email is read or written to avoid duplicate-detection bugs.

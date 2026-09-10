# Guía de despliegue y solución de problemas

Esta guía complementa el README: aquí está el paso a paso para desplegar el proyecto desde cero en una máquina
nueva, y una lista de errores reales (varios encontrados al verificar este mismo proyecto) con su solución.

## 0. Prerrequisitos

| Herramienta | Cómo verificar | Instalación |
|---|---|---|
| .NET 8 SDK | `dotnet --version` → debe imprimir `8.x` | https://dotnet.microsoft.com/download/dotnet/8.0 |
| SQL Server (real, LocalDB o Docker) | ver sección 2 | — |
| `dotnet-ef` (herramienta global) | `dotnet ef --version` | `dotnet tool install --global dotnet-ef --version 8.0.10` |

> Instala `dotnet-ef` **con la misma versión mayor** que `Microsoft.EntityFrameworkCore.Design` en el `.csproj`
> (aquí es 8.0.10). Instalarlo sin `--version` puede traer una versión más nueva (p. ej. 10.x) que no coincide
> con el SDK 8 instalado y falla al ejecutar (ver error #1 abajo).

---

## 1. Levantar la base de datos

Elige una opción:

### Opción A — SQL Server propio / LocalDB
Ya tienes una instancia corriendo. Anota servidor, usuario/autenticación y contraseña.

### Opción B — Docker (recomendada si no tienes SQL Server instalado)
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<TuPassword_Segura123>" \
  -p 1433:1433 --name sql-taller3 -d mcr.microsoft.com/mssql/server:2019-latest
```
Espera a que esté lista (puede tardar 30-90s la primera vez):
```bash
docker logs sql-taller3 | grep "ready for client connections"
```

**Checkpoint:** si el comando anterior no imprime nada tras esperar, revisa el error #4 más abajo.

---

## 2. Configurar la cadena de conexión

En `UsuarioApi/appsettings.json` (o vía variable de entorno, sin tocar el archivo):

```bash
# Windows (PowerShell)
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=SoftwareArchitectureDb;User Id=sa;Password=<TuPassword_Segura123>;TrustServerCertificate=True;"
```
```bash
# Git Bash / Linux / macOS
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=SoftwareArchitectureDb;User Id=sa;Password=<TuPassword_Segura123>;TrustServerCertificate=True;"
```

Si usas SQL Server con autenticación de Windows (`Trusted_Connection=True`, el valor por defecto del repo),
no necesitas la variable de entorno; el `appsettings.json` ya está configurado así.

---

## 3. Restaurar, compilar y migrar

```bash
cd UsuarioApi
dotnet restore
dotnet build
dotnet ef database update
```

**Checkpoint:** `dotnet build` debe terminar con `0 Errores`. `dotnet ef database update` debe listar la
migración `InitialCreate` aplicándose y terminar en `Done.`

Si no tienes `Migrations/` en el repo o quieres regenerarla:
```bash
dotnet ef migrations add InitialCreate
```

Si `dotnet ef` falla por completo, usa el script SQL manual como alternativa (no requiere `dotnet-ef`):
ejecuta `scripts/001_CrearBaseDeDatos.sql` en SSMS / Azure Data Studio / `sqlcmd`.

---

## 4. Ejecutar las pruebas unitarias (opcional pero recomendado)

```bash
cd ../UsuarioApi.Tests
dotnet test
```
No requieren base de datos (usan mocks). Deben pasar 6/6.

---

## 5. Ejecutar la API

```bash
cd ../UsuarioApi
dotnet run
```

Esto usa `launchSettings.json`, que fija `ASPNETCORE_ENVIRONMENT=Development` automáticamente (necesario para
que Swagger esté disponible). La consola imprime la URL, típicamente `https://localhost:7080` o similar.

---

## 6. Verificar el despliegue

1. Abre `https://localhost:<puerto>/swagger` (o `http://...` si usas HTTP) en el navegador.
2. Deben aparecer 5 endpoints: `GET /api/Usuarios`, `POST /api/Usuarios`, `GET /api/Usuarios/{id}`,
   `PUT /api/Usuarios/{id}`, `DELETE /api/Usuarios/{id}`.
3. Prueba el flujo completo con "Try it out":
   - `GET /api/Usuarios` → `200`, lista vacía si es la primera vez.
   - `POST /api/Usuarios` con un body válido → `201 Created`.
   - `GET /api/Usuarios/{id}` con el id devuelto → `200`.
   - `PUT /api/Usuarios/{id}` → `204 No Content`.
   - `DELETE /api/Usuarios/{id}` → `204`, y repetirlo da `404`.
4. Repite el `POST` con el mismo correo → debe dar `400` ("El correo ya está registrado.").

Si todos estos pasos responden como se describe, el despliegue está verificado y funcionando de extremo a
extremo (API → Service → Repository → EF Core → SQL Server).

---

## Errores comunes y solución

### 1. `dotnet ef` falla con "You must install .NET to run this application" / `hostfxr.dll` no encontrado
**Causa:** la herramienta global `dotnet-ef` se instaló en una versión (p. ej. 10.x) que no coincide con ningún
SDK de .NET instalado en la máquina, o la variable `DOTNET_ROOT` no apunta a la instalación del SDK.
**Solución:**
```bash
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 8.0.10
```
Si sigue fallando en una terminal tipo Git Bash/MSYS, define `DOTNET_ROOT` explícitamente:
```bash
export DOTNET_ROOT="/c/Program Files/dotnet"   # ajusta a tu ruta real de instalación
```
*(Encontrado y verificado en esta sesión.)*

### 2. Login failed for user 'sa': "An error occurred while evaluating the password"
**Causa:** bug conocido en builds específicos de la imagen `mcr.microsoft.com/mssql/server:2022-latest` (se
confirmó con la build RTM-CU26-GDR) — el login de `sa` falla sin importar qué tan segura sea la contraseña.
**Solución:** usa la imagen de SQL Server 2019 en su lugar:
```bash
docker rm -f sql-taller3
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<TuPassword>" -p 1433:1433 --name sql-taller3 -d mcr.microsoft.com/mssql/server:2019-latest
```
*(Encontrado y verificado en esta sesión — si te vuelve a pasar con una imagen distinta, prueba fijar un tag de
build más antiguo, p. ej. `2022-CU12-ubuntu-22.04`.)*

### 3. Todo `POST`/`PUT` responde 400 con un mensaje sobre "validation metadata... must be associated with the constructor parameter"
**Causa:** bug de código ya corregido en este repo (los DTOs tenían `[property: Required]` en vez de
`[Required]` sobre records con constructor primario). Si ves este error, probablemente estás en una copia
anterior al fix.
**Solución:** confirma que `UsuarioApi/DTOs/UsuarioDtos.cs` **no** tiene el prefijo `property:` en los
atributos de validación. Si lo tiene, quítalo:
```csharp
// Mal (rompe en runtime):
[property: Required(...)]
// Bien:
[Required(...)]
```
*(Encontrado, diagnosticado y corregido en esta sesión — ver README, sección "Nota sobre este repositorio".)*

### 4. `docker run` falla con "failed to connect to the docker API... El sistema no puede encontrar el archivo especificado"
**Causa:** Docker Desktop no está corriendo.
**Solución (Windows):** inicia Docker Desktop y espera a que el ícono de la bandeja indique que el motor está
listo, luego reintenta:
```powershell
Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"
```
Puede tardar 20-60s en levantar el daemon la primera vez.

### 5. Swagger da 404 en `/swagger` aunque la API está corriendo
**Causa:** Swagger solo se registra cuando `ASPNETCORE_ENVIRONMENT=Development` (ver `Program.cs`). Si
ejecutaste con `dotnet run --no-launch-profile`, o publicaste/ejecutaste el `.dll` directamente, el entorno por
defecto es `Production` y Swagger no se expone (esto es intencional, no un bug).
**Solución:** usa `dotnet run` sin `--no-launch-profile` (usa `launchSettings.json`), o exporta la variable
manualmente:
```bash
export ASPNETCORE_ENVIRONMENT="Development"
```

### 6. `dotnet ef database update` falla con timeout o "A network-related or instance-specific error"
**Causa:** SQL Server aún no terminó de inicializar (el contenedor puede tardar hasta 1-2 minutos en la primera
ejecución en crear las bases de sistema), o el puerto/host de la cadena de conexión no coincide.
**Solución:** espera a que el log del contenedor diga "SQL Server is now ready for client connections" antes
de correr la migración. Verifica también que el puerto en la cadena de conexión (`1433` por defecto) coincida
con el mapeado en `docker run -p`.

### 7. Puerto 1433 ya está en uso
**Causa:** otra instancia de SQL Server (local o en otro contenedor) ya usa el puerto.
**Solución:** mapea un puerto distinto y actualízalo en la cadena de conexión:
```bash
docker run ... -p 14330:1433 ...
# Cadena de conexión: Server=localhost,14330;...
```

### 8. `dotnet ef migrations add` falla con errores de compilación
**Causa:** el proyecto no compila (EF Core necesita compilar el proyecto para inspeccionar el modelo).
**Solución:** corre `dotnet build` primero y corrige cualquier error de compilación antes de generar la
migración.

### 9. IDs inesperados (p. ej. empiezan en 0 en vez de 1) tras vaciar la tabla manualmente
**Causa:** si truncas la tabla y además ejecutas `DBCC CHECKIDENT ('Usuarios', RESEED, 0)` sobre una tabla ya
vacía, SQL Server usa ese mismo valor (`0`) para el próximo insert en vez de `valor + 1` — es un comportamiento
documentado de SQL Server, no un bug de la aplicación.
**Solución:** si quieres que el próximo id sea `1`, simplemente usa `TRUNCATE TABLE Usuarios;` solo (sin el
`DBCC CHECKIDENT` adicional) — `TRUNCATE` ya reinicia el contador al valor semilla.

### 10. Certificado HTTPS no confiable en el navegador al abrir Swagger
**Causa:** el certificado de desarrollo de ASP.NET Core no está confiado en el sistema.
**Solución:**
```bash
dotnet dev-certs https --trust
```

---

## Checklist final

- [ ] `dotnet build` sin errores
- [ ] `dotnet test` → 6/6 pruebas pasan
- [ ] SQL Server accesible (contenedor `Up` o instancia propia respondiendo)
- [ ] `dotnet ef database update` terminó en `Done.`
- [ ] `dotnet run` levanta la API sin excepciones en consola
- [ ] Swagger carga en `/swagger`
- [ ] Los 5 endpoints responden con los códigos HTTP esperados (200/201/204/400/404)

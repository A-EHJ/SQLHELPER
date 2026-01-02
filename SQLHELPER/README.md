# SQL Maintenance Helper

Aplicación Blazor Server para conectarse a una instancia de SQL Server, crear la base `SQLHELPER` y gestionar notas/queries de mantenimiento.

## Ejecutar la app
1. Requiere .NET 8 SDK.
2. Desde la raíz del repo: `dotnet run --project SQLHELPER/SQLHELPER.csproj`.
3. Abre el navegador en la URL indicada por la consola (p.ej. `https://localhost:5001`).

## Conectar contra tu instancia
1. En la UI abre **Connect** (`/connect`).
2. Completa Server, User, Password, DefaultTargetDb (por defecto `SIN`) y las opciones de Encrypt/TrustServerCertificate.
3. Guarda y presiona **Test connection** para validar. El perfil se almacena en `ProtectedLocalStorage` del navegador (no se guardan credenciales en archivos).

## Ejecutar Setup
1. Abre **Setup** (`/setup`).
2. Revisa el script T-SQL que crea la base `SQLHELPER` con las tablas `dbo.Notes` y `dbo.SavedQueries`.
3. Presiona **Run setup**. El script se ejecuta usando la conexión a `master` y deja la base lista para Notes/Library.

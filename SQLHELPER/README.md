# SQLHELPER

Esta aplicación ejecuta un bootstrap ligero de SQL Server al iniciar para garantizar que exista la base de datos **SqlMaintenanceHub**, su esquema `maint` y algunas semillas opcionales.

## Scripts de base de datos
Los scripts viven en el directorio `sql/` y se copian junto con la aplicación publicada:

1. `000_create_hub_db.sql`: crea la base **SqlMaintenanceHub** en caso de que no exista (usa `IF DB_ID(...) IS NULL`).
2. `001_create_schema_tables.sql`: asegura el esquema `maint` y crea tablas/índices principales con comprobaciones `IF OBJECT_ID(...) IS NULL`.
3. `002_seed.sql`: inserta datos de ejemplo solo si no existen.

Los scripts están diseñados para ser idempotentes, por lo que pueden ejecutarse varias veces sin efectos adversos.

## Configuración de la conexión
Define una cadena llamada `SqlServer` en `appsettings.json`, `appsettings.Development.json` o variables de entorno. La cadena debe apuntar a la instancia de SQL Server (por ejemplo `Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;`).

El bootstrap usa esa cadena para conectarse a `master` al ejecutar `000_create_hub_db.sql` y vuelve a usarla contra `SqlMaintenanceHub` para los scripts restantes.

## Ejecución durante el arranque
`BootstrapService` se registra en `Program.cs` y se ejecuta al iniciar la aplicación:

- Busca los archivos en `sql/` (el `ContentRootPath`).
- Ejecuta los scripts en orden (000, 001, 002), separando lotes por la palabra clave `GO`.
- Omite scripts inexistentes o vacíos pero registra avisos.

Si falta la cadena de conexión o el directorio `sql/`, el proceso se omite y se registra una advertencia.

## Personalizar semillas y tareas
Puedes agregar más archivos SQL dentro de `sql/` y ejecutarlos desde `BootstrapService`. Cualquier archivo adicional debería incluir comprobaciones `IF OBJECT_ID(...) IS NULL` o `IF NOT EXISTS` para seguir siendo seguro en ejecuciones repetidas.

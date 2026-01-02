# SQLHELPER

SQLHELPER es una aplicación Blazor Server orientada a simplificar las tareas recurrentes con SQL Server desde una interfaz web. El proyecto prioriza la configuración guiada y la operación segura para ambientes donde se requiere observar o validar datos sin exponer credenciales en el repositorio.

## Prerrequisitos

- **.NET 8 SDK** para compilar y ejecutar el proyecto.
- **Acceso a un servidor SQL Server** (instancia accesible y credenciales válidas). La aplicación asume que el host es alcanzable desde la máquina donde se ejecuta.
- Permisos de red para llegar al servidor y, opcionalmente, habilitar el cifrado de conexión si la instancia lo exige.

## Primera ejecución

1. Restaurar dependencias y construir el proyecto:
   - `dotnet restore`
   - `dotnet run --project SQLHELPER`
2. Si no existen parámetros de conexión, la aplicación redirige automáticamente al asistente de configuración en `/setup` para capturar los datos mínimos (servidor, base, autenticación y modo de operación).
3. Tras completar el asistente, se puede navegar al módulo deseado desde el menú lateral.

## Flujo de configuración

- **Captura de datos**: servidor, base de datos y método de autenticación (SQL Server o Windows/AD). El asistente valida que los campos obligatorios no estén vacíos.
- **Prueba de conexión**: se recomienda ejecutar la prueba antes de guardar para confirmar que el host y las credenciales son correctos.
- **Persistencia local**: los valores se almacenan en `appsettings.Development.json` (solo ambiente de desarrollo) o en almacenamiento local cifrado. No se debe commitear ninguna versión con credenciales.
- **Revisión**: el asistente muestra un resumen final antes de habilitar el resto de módulos.

## Módulos disponibles

- **Home**: página de bienvenida y punto de acceso al resto de la aplicación.
- **Counter (demo interactiva)**: ejemplo de componente interactivo para validar el estado del servidor y la funcionalidad de SignalR en modo servidor.
- **Weather (demo de datos)**: muestra de carga y renderizado de datos en tabla, útil para verificar latencia y streaming de componentes.

## Modo seguro (safe mode)

El modo seguro está pensado para operar con privilegios mínimos cuando se trabaja sobre bases sensibles:

- Solo permite operaciones de lectura (`SELECT`), sin ejecutar `INSERT`, `UPDATE`, `DELETE` ni DDL (`CREATE`, `ALTER`, `DROP`).
- Bloquea la ejecución de múltiples sentencias encadenadas y comandos que habiliten `xp_cmdshell` u otros procedimientos extendidos.
- Desactiva la posibilidad de modificar configuraciones de servidor o base de datos.
- Se debe usar el asistente para activar o desactivar el modo seguro por conexión. Mantenerlo habilitado en ambientes productivos.

## Publicación Windows self-contained

Para generar un artefacto autocontenido para Windows x64 en un solo archivo ejecutable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

El resultado se ubicará en `bin/Release/net8.0/win-x64/publish/`.

## Seguridad

- **No subir contraseñas ni cadenas de conexión al repositorio**. Evitar commitear `appsettings.*.json` con secretos.
- En entornos Windows, los ajustes locales se protegen con **DPAPI**; los valores quedan ligados al perfil de usuario, impidiendo su lectura fuera del host original.
- Para trabajo colaborativo, preferir variables de entorno o `dotnet user-secrets` durante el desarrollo y Azure Key Vault u otro gestor de secretos en despliegues.

## Arquitectura de carpetas

- `Components/`: componentes Razor.
  - `Pages/`: páginas enrutables (`Home`, `Counter`, `Weather`).
  - `Layout/`: diseño principal y menú de navegación.
  - `App.razor` y `Routes.razor`: definen la raíz de la aplicación y el enrutamiento.
- `wwwroot/`: archivos estáticos como CSS, iconos y recursos de Bootstrap.
- `Program.cs`: configuración mínima de servicios y pipeline de ASP.NET Core.
- `appsettings*.json`: configuración base y de desarrollo (mantener sin secretos en control de versiones).

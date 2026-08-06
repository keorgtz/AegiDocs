# Inventario de dependencias y licencias

## Estado actual

Al 2026-08-05, AegiDocs declara exclusivamente las dependencias NuGet de su
infraestructura de pruebas. No se han aprobado dependencias de producción.

`AegiDocs.Recorder` usa `net9.0-windows` con WPF habilitado. .NET 9,
WindowsDesktop y WPF son el framework de plataforma suministrado por el SDK y la
instalación de .NET; no son paquetes NuGet directos y no deben añadirse a la
tabla de paquetes de este documento.

La administración central de versiones está habilitada en
`Directory.Packages.props` mediante `ManagePackageVersionsCentrally` y
`CentralPackageTransitivePinningEnabled`.

## Política de incorporación

Se debe preferir la biblioteca de clases base (BCL) y las capacidades incluidas
en el SDK. Cualquier dependencia fuera de la BCL requiere aprobación explícita
antes de añadirse, incluso si es transitiva o proviene de Microsoft.

La aprobación requiere:

1. Un ADR cuando la dependencia introduce una decisión de arquitectura,
   seguridad, privacidad, almacenamiento, exportación, interoperabilidad o
   distribución; de lo contrario, una justificación de alcance equivalente en
   la tarea o PR.
2. Revisión de licencia, mantenimiento, compatibilidad con .NET 9 y Windows,
   superficie de seguridad, tamaño y alternativa basada en BCL.
3. Registro completo en la tabla de este documento antes o dentro del mismo
   cambio que agrega la dependencia.
4. La versión centralizada en `Directory.Packages.props`. Está prohibido fijar
   versiones en línea en archivos `.csproj`.

No se deben inventar, inferir ni copiar licencias sin verificarlas desde la
fuente de distribución oficial del paquete o su repositorio mantenido.

## Registro de paquetes aprobados

Cada paquete NuGet directo aprobado debe ocupar una fila y contener todos los
campos obligatorios:

| ID del paquete | Versión central | Propósito en AegiDocs | Licencia verificada | Distribución / atribución | Propietario | ADR, riesgo y fecha de revisión |
| --- | --- | --- | --- | --- | --- | --- |
| `Microsoft.NET.Test.Sdk` | `18.8.1` | Descubrimiento y ejecución de pruebas de los proyectos `tests/**`; solo test, sin uso de producción. | MIT, verificada en el metadato de licencia de [NuGet](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1). | Se distribuye únicamente como dependencia de desarrollo/prueba; no se empaqueta ni se atribuye en el binario de producción. | AegiDocs Engineering | `ADR-0001`; riesgo: cadena de suministro de test; revisión: 2027-08-05. |
| `xunit` | `2.9.3` | Framework de pruebas unitarias e integración para `tests/**`; solo test, sin uso de producción. | Apache-2.0, verificada en el metadato de licencia de [NuGet](https://www.nuget.org/packages/xunit/2.9.3). | Se distribuye únicamente como dependencia de desarrollo/prueba; no se empaqueta ni se atribuye en el binario de producción. | AegiDocs Engineering | `ADR-0001`; riesgo: cadena de suministro de test; revisión: 2027-08-05. |
| `xunit.runner.visualstudio` | `3.1.5` | Adaptador de descubrimiento y ejecución de xUnit desde `dotnet test` y Visual Studio para `tests/**`; solo test. | Apache-2.0, verificada en el metadato de licencia de [NuGet](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5). | Se distribuye únicamente como dependencia de desarrollo/prueba; `PrivateAssets="all"` evita su propagación a consumidores. | AegiDocs Engineering | `ADR-0001`; riesgo: cadena de suministro de test; revisión: 2027-08-05. |

Campos obligatorios:

- **ID del paquete:** identificador exacto publicado en NuGet.
- **Versión central:** versión definida únicamente en `Directory.Packages.props`.
- **Propósito en AegiDocs:** necesidad concreta, límite arquitectónico y
  alternativa BCL descartada.
- **Licencia verificada:** SPDX o texto de licencia confirmado, con fuente y
  cualquier obligación relevante.
- **Distribución / atribución:** impacto al redistribuir la aplicación,
  avisos, `NOTICE`, código fuente u otros requisitos que deban incluirse.
- **Propietario:** persona o área responsable de vigilar el paquete.
- **ADR, riesgo y fecha de revisión:** enlace al ADR o justificación aprobada,
  riesgos conocidos (incluidos CVE si aplica) y próxima fecha de revisión.

Las dependencias transitivas relevantes para seguridad o distribución se
anotan como parte del registro del paquete directo que las introduce; si se
fijan centralmente, también requieren una fila individual.

## Actualizaciones, vulnerabilidades y retiro

1. Antes de actualizar, identificar los paquetes afectados y revisar sus notas
   de versión, cambios incompatibles, licencia y dependencias transitivas.
2. Actualizar la versión solo en `Directory.Packages.props`; no modificar la
   versión dentro de un `.csproj`.
3. Actualizar esta tabla, el ADR o la justificación de aprobación y los avisos
   de distribución cuando cambie cualquiera de esos datos.
4. Ejecutar como mínimo `dotnet restore`, `dotnet format AegiDocs.slnx
   --verify-no-changes`, `dotnet build AegiDocs.slnx --configuration Release`
   y las pruebas aplicables. Revisar además vulnerabilidades reportadas por las
   herramientas de restore o auditoría configuradas por el repositorio.
5. Ante una vulnerabilidad, registrar severidad, paquete afectado, versiones,
   exposición en AegiDocs, mitigación, propietario y fecha objetivo. Priorizar
   retiro, actualización o mitigación documentada; no silenciar avisos sin
   decisión explícita y fecha de revisión.
6. Al retirar un paquete, eliminar su `PackageReference` y su versión central,
   actualizar la fila a historial o eliminarla según la política de releases,
   y confirmar que ya no quede como dependencia transitiva requerida.

Las actualizaciones no pueden añadir telemetría, red, recolección de contenido
capturado ni secretos implícitos. Cualquier cambio de ese tipo exige un ADR de
privacidad y una aprobación humana antes de distribuirlo.

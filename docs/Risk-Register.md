# Registro de riesgos y decisiones pendientes

## Propósito

Este registro concentra los riesgos que pueden afectar la privacidad, confiabilidad, mantenibilidad, experiencia de uso y entrega de AegiDocs.Recorder. Debe revisarse antes de iniciar una tarea relacionada y actualizarse al descubrir evidencia nueva, adoptar una mitigación o aceptar un riesgo residual.

No reemplaza un ADR: una decisión con alternativas de arquitectura, seguridad, licencia o producto debe documentarse además en `docs/adr/`.

## Escalas

### Probabilidad

| Nivel | Criterio |
| --- | --- |
| Baja | Poco probable con los controles actuales; requiere una condición inusual. |
| Media | Puede ocurrir en flujos o equipos reales y debe probarse explícitamente. |
| Alta | Es esperable durante el desarrollo, en equipos heterogéneos o sin una mitigación implementada. |

### Impacto

| Nivel | Criterio |
| --- | --- |
| Bajo | Afecta un flujo no crítico; existe una recuperación simple y local. |
| Medio | Degrada una capacidad relevante, genera trabajo manual o retrasa una entrega. |
| Alto | Puede exponer datos, perder trabajo, impedir grabar/exportar o bloquear una beta. |
| Crítico | Compromete privacidad, seguridad, integridad de proyectos o distribución segura. |

## Estados

| Estado | Significado |
| --- | --- |
| Abierto | Requiere seguimiento y no tiene todos los controles verificados. |
| En mitigación | Hay trabajo planificado o en curso para reducirlo. |
| En decisión | Falta una decisión explícita, normalmente un ADR o aprobación humana. |
| Aceptado | El riesgo residual fue aceptado con alcance, responsable y fecha de revisión. |
| Cerrado | Mitigación verificada; conservar la evidencia y reabrir si cambian las condiciones. |

## Cadencia de revisión

- El orquestador revisa los riesgos que afecten una tarea antes de marcarla `in_progress`.
- En cada gate de epic se revisan todos los riesgos abiertos o en mitigación de ese epic; los de impacto Alto o Crítico requieren evidencia explícita en el handoff.
- El registro se revisa al menos una vez por ola de implementación y antes de cualquier beta, empaquetado o distribución.
- Cualquier incidente de privacidad, pérdida de datos, fallo de recursos nativos o hallazgo de licencia abre o actualiza un registro en la misma iteración.

## Riesgos y decisiones pendientes

| ID | Categoría | Descripción | Probabilidad | Impacto | Trigger | Mitigación | Contingencia | Propietario | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| R-001 | Captura Windows | `Windows.Graphics.Capture` puede no estar disponible, no permitir el elemento elegido o fallar por permisos, política corporativa, versión de Windows o cambios de ventana. | Media | Alto | Inicio de captura rechazado, sesión nula, error HRESULT o resultado vacío. | Encapsular WGC tras una interfaz; comprobar capacidad/versión antes de iniciar; mostrar consentimiento y errores accionables; pruebas en Windows 10/11 compatibles. | Detener la sesión sin crear pasos incompletos, conservar el proyecto y ofrecer reintentar o seleccionar otro destino; no sustituir silenciosamente la tecnología. | Responsable Windows | Abierto |
| R-002 | Input y recursos nativos | Hooks globales, hotkeys, message pumps, handles o frame pools pueden quedarse activos, bloquear UI o filtrarse tras stop, cancelación o error. | Media | Crítico | Stop/cancel repetido, excepción en callback, cierre de aplicación o prueba de estrés deja recursos registrados. | ADR y wrappers testeables con ownership explícito; callback mínimo; `SafeHandle` cuando aplique; cierre idempotente y pruebas de carreras/cancelación. | Deshabilitar la grabación, liberar en un cierre de emergencia, registrar diagnóstico local sin PII y pedir reinicio de la app solo si la liberación no puede confirmarse. | Responsable Windows | Abierto |
| R-003 | DPI y multimonitor | Coordenadas físicas, escalado DPI, límites de monitor y cambios de pantalla pueden producir clics o anotaciones desplazados. | Alta | Alto | Monitor distinto, DPI 125–200 %, docking/undocking, cambio de resolución o captura con dimensiones inesperadas. | Normalizar geometría en Domain; aislar resolución de monitor/DPI; pruebas de conversiones y matriz manual multi-DPI; declarar multimonitor fuera del MVP hasta cumplir contrato. | Marcar el paso como no confiable, permitir corregirlo en el editor y bloquear la publicación automática de coordenadas erróneas. | Responsable Windows | En mitigación |
| R-004 | Privacidad | Las capturas, títulos de ventana, OCR futuro o datos de interacción pueden contener información sensible y salir del equipo o de los exports sin una redacción deliberada. | Alta | Crítico | Captura muestra PII, secreto, token, conversación o datos de cliente; intento de activar IA/proveedor externo. | Local-first sin red/telemetría por defecto; exclusión de AegiDocs; minimización de metadatos; redacción manual irreversible en outputs; consentimiento explícito para todo servicio futuro. | Pausar captura/exportación afectada, advertir a la persona autora, permitir borrar assets y repetir el paso; no transmitir ni registrar los datos. | Responsable de producto y privacidad | Abierto |
| R-005 | Persistencia | Un cierre, disco lleno, fallo de I/O o serialización defectuosa puede corromper un `.aegidocs` o perder ediciones recientes. | Media | Crítico | Escritura interrumpida, excepción de archivo, manifiesto ilegible o fallo durante autosave. | Guardado atómico con temporal, flush y recuperación; DTOs versionados; pruebas de fallo inyectado, round-trip y cancelación; un escritor por proyecto. | Conservar la última versión válida, ofrecer restaurar o descartar recovery, y mostrar un error tipado sin sobrescribir de forma silenciosa. | Responsable Storage | Abierto |
| R-006 | Seguridad de archivos | Un proyecto o asset malicioso puede usar rutas absolutas, `..`, enlaces, tamaños excesivos o metadatos corruptos para escapar del directorio administrado o agotar recursos. | Media | Crítico | Apertura de proyecto externo, ruta no relativa, enlace simbólico, tamaño/límite excedido o versión no admitida. | Validar rutas, root canónico, extensiones, conteos y tamaños; rechazar versiones futuras; no seguir enlaces fuera del root; parser con límites y pruebas adversariales. | Rechazar la apertura sin tocar archivos externos, preservar diagnóstico seguro y permitir abrir una copia recuperable solo tras validación. | Responsable Storage y seguridad | Abierto |
| R-007 | Dependencias y licencias | Paquetes NuGet o herramientas de exportación pueden introducir licencias incompatibles, vulnerabilidades, costo o requisitos de distribución no aceptados. | Media | Alto | Solicitud de paquete nuevo, actualización de versión o elección de motor PDF/HTML. | Administración central de paquetes; inventario de propósito/licencia; ADR y revisión Sol antes de incorporar dependencias no aprobadas; preferir BCL cuando sea viable. | No integrar ni distribuir el paquete; mantener feature tras flag o elegir una alternativa con licencia aprobada. | Orquestador | En decisión |
| R-008 | Arquitectura modular | La rapidez inicial puede acoplar WPF con Win32, almacenamiento, exportación o IA, haciendo imposible probar y paralelizar sin regresiones. | Media | Alto | Domain referencia WPF/Win32, view models contienen lógica de negocio o infraestructura llega directo a views. | ADR de arquitectura; referencias de proyecto permitidas; puertos en Application; tests de arquitectura y revisión en Gate E01. | Detener cambios dependientes, extraer el contrato mínimo y migrar con pruebas de regresión antes de aumentar funcionalidad. | Orquestador y responsable de arquitectura | En mitigación |
| R-009 | Rendimiento y presión de recursos | Una ráfaga de clics, imágenes grandes o un proyecto extenso puede saturar memoria, disco, UI o colas y perder pasos silenciosamente. | Media | Alto | Cola llena, latencia creciente, OOM, timeouts, 500 pasos o captura de alta resolución. | Cola acotada con backpressure definido; procesamiento fuera de hooks/UI; límites de tamaño; thumbnails y benchmarks; métricas locales sin PII. | Detener de manera controlada, conservar pasos ya confirmados, informar capacidad alcanzada y permitir continuar en una sesión nueva. | Responsable de grabación | Abierto |
| R-010 | Accesibilidad y MeridianUI | Una UI que use tokens ad hoc, contraste insuficiente, navegación de teclado incompleta o estados no comunicados incumple MeridianUI y reduce la usabilidad. | Media | Alto | Nueva view/control, auditoría a 100–200 % DPI, navegación sin mouse o revisión visual detecta valores locales. | Leer la skill y referencias MeridianUI antes de UI; reutilizar `MeridianTokens.xaml`; MVVM; estados loading/empty/error/disabled; pruebas visuales y de teclado. | Bloquear la aceptación de la view hasta corregir tokens, contraste, foco, copy es-MX y estados; documentar cualquier extensión del sistema. | Responsable UI/UX | Abierto |
| R-011 | Empaquetado y firma | El instalador puede fallar en equipo limpio, requerir permisos inesperados, quedar sin firmar o generar alertas de Windows Defender/SmartScreen. | Media | Alto | Publicación self-contained falla, certificado ausente/expirado, instalación/actualización bloqueada o alerta de reputación. | Definir estrategia de empaquetado y firma en ADR; pruebas de instalación limpia; versionado reproducible; separar secretos de firma del repositorio. | No publicar el artefacto; distribuir únicamente builds internas aprobadas y corregir el pipeline/certificado antes de beta pública. | Responsable release | Abierto |
| R-012 | IA futura | Proveedores, prompts y modelos futuros pueden filtrar contenido, aceptar instrucciones maliciosas desde capturas/texto, generar errores o crear costos impredecibles. | Media | Crítico | Habilitación de proveedor, envío de contenido, respuesta inesperada, prompt injection o presupuesto excedido. | Mantener IA fuera del MVP; ADR de privacidad; proveedor abstracto y fake; envío mínimo con consentimiento, secretos en Credential Manager/DPAPI, diff revisable y límites de costo/retención. | Deshabilitar proveedor, revocar credenciales, no aplicar resultados automáticamente y conservar el proyecto local intacto. | Responsable de producto y privacidad | Abierto |

## Decisiones pendientes que requieren ADR o aprobación

| ID | Decisión | Relación | Responsable | Fecha objetivo | Estado |
| --- | --- | --- | --- | --- | --- |
| D-001 | Definir el formato físico de `.aegidocs`: directorio administrado o contenedor ZIP, con estrategia de recovery y límites de entrada. | R-005, R-006 | Responsable Storage; aprobación de arquitectura | Antes de E03-P01-T02 | Pendiente |
| D-002 | Aprobar dependencias y licencias de los motores de PDF y HTML antes de incluir paquetes NuGet. | R-007 | Orquestador; revisión Sol | Antes de E10 | Pendiente |
| D-003 | Aprobar contrato nativo de hooks, hotkeys, threading y liberación de recursos. | R-002 | Responsable Windows; revisión Sol | Antes de E05-P01-T02 | Pendiente |
| D-004 | Fijar el alcance de monitores múltiples para el MVP y las condiciones de degradación segura. | R-003 | Producto y responsable Windows | Antes de E06 | Pendiente |
| D-005 | Elegir empaquetado, certificados, custodia de secretos de firma y política de publicación. | R-011 | Responsable release; aprobación humana | Antes de E12 | Pendiente |
| D-006 | Definir modelo de privacidad y consentimiento para cualquier proveedor de IA futuro. | R-004, R-012 | Producto, privacidad y revisión Sol | Antes de E13-P02 | Pendiente |

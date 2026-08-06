# Plan maestro de implementación: AegiDocs.Recorder

## 1. Objetivo y forma de usar este documento

Este documento convierte la visión de AegiDocs.Recorder en un backlog ejecutable por agentes. Cada tarea tiene un identificador estable, un resultado verificable y una recomendación de modelo. El [playbook de agentes](Agent-Execution-Playbook.md) define cómo repartir, revisar e integrar el trabajo.

El objetivo del MVP es permitir que una persona:

1. Cree o abra un proyecto local.
2. Inicie una grabación con consentimiento visible.
3. Genere pasos a partir de clics y capturas confiables.
4. Revise, reordene, edite y anote esos pasos.
5. Oculte datos sensibles antes de publicar.
6. Exporte el mismo tutorial a Markdown y PDF.

El MVP no depende de OCR, IA, nube, Word, PowerPoint, GIF ni colaboración. Esas capacidades se incorporan después de estabilizar la fuente única de verdad, la captura y la edición.

## 2. Convenciones del backlog

- Identificador: `E##-P##-T##` significa Epic, Part y Task.
- Cada tarea debe caber en un único turno de agente o entregarse dividida antes de comenzar.
- `Terra` significa `gpt-5.6-terra`, el modelo predeterminado de ejecución.
- `Sol` significa `gpt-5.6-sol`, reservado para decisiones o revisiones de mayor riesgo.
- Las tareas marcadas `Terra + revisión Sol` se implementan con Terra y solo el resultado se revisa con Sol.
- Una tarea no está terminada por compilar: debe cumplir su validación y entregar evidencia.
- Los estados permitidos son `pending`, `in_progress`, `blocked` y `completed`.

## 3. Definiciones globales de calidad

### Definition of Ready

Una tarea está lista cuando tiene alcance, archivos o área propietaria, dependencias resueltas, contrato de entrada/salida, criterio de aceptación y comando de validación. Si requiere una decisión nueva de producto, seguridad, licencia o arquitectura, primero se crea un ADR.

### Definition of Done

Una tarea está terminada cuando:

- Compila con nullable, analyzers y warnings como errores.
- Incluye pruebas proporcionales al riesgo y todas pasan.
- No deja secretos, telemetría ni tráfico de red implícito.
- Mantiene separación entre Domain, Application, infraestructura, exportación y presentación.
- Las operaciones asíncronas aceptan `CancellationToken` cuando corresponda.
- Los recursos nativos se liberan también al cancelar o fallar.
- Toda UI cumple MeridianUI, estados de interacción y accesibilidad aplicables.
- Actualiza documentación o ADR si cambió un contrato o una decisión.
- El handoff enumera archivos, comandos ejecutados, resultados y riesgos restantes.

### Gates obligatorios por integración

```powershell
dotnet format AegiDocs.slnx --verify-no-changes
dotnet build AegiDocs.slnx --configuration Release
dotnet test AegiDocs.slnx --configuration Release --no-build
```

Si todavía no existe una suite de pruebas o `dotnet format` no está configurado, el agente debe declararlo; no debe afirmar que el gate pasó.

## 4. Arquitectura objetivo del MVP

La solución evolucionará de forma explícita a estos límites:

| Proyecto | Responsabilidad | No debe conocer |
| --- | --- | --- |
| `AegiDocs.Domain` | Entidades, value objects, invariantes y eventos de dominio | WPF, Win32, archivos, exportadores, proveedores IA |
| `AegiDocs.Application` | Casos de uso, puertos e interfaces | Implementaciones Windows/WPF |
| `AegiDocs.Infrastructure.Storage` | Persistencia, assets, migraciones y recuperación | UI |
| `AegiDocs.Infrastructure.Windows` | Hooks, captura, DPI, ventanas y recursos nativos | View models y exportadores |
| `AegiDocs.Export` | Representación publicable, Markdown y PDF | WPF, hooks |
| `AegiDocs.Recorder` | Composición, WPF, navegación y MVVM | Detalles nativos directos |
| `*.Tests` | Unitarias, integración, contratos, snapshots y fixtures | Secretos y estado global no aislado |

La extracción se realiza en E01. Hasta completarla, ningún agente debe aumentar el acoplamiento del proyecto WPF actual.

## 5. Secuencia de entrega

| Ola | Epics | Paralelismo seguro | Gate de salida |
| --- | --- | --- | --- |
| 0 | E00–E01 | Documentación, estructura y pruebas base | Solución modular compilable |
| 1 | E02–E03 | Dominio y almacenamiento después de fijar contratos | Proyecto local round-trip |
| 2 | E04–E06 | Coordinador, input y captura en áreas separadas | Grabación técnica estable |
| 3 | E07 | UI de recorder sobre servicios falsos y después reales | Flujo grabar/detener usable |
| 4 | E08–E09 | Editor y anotaciones con contratos ya congelados | Tutorial editable y redactable |
| 5 | E10 | Exportadores paralelos sobre representación común | Markdown y PDF consistentes |
| 6 | E11–E12 | Hardening, matriz Windows y distribución | Beta instalable |
| Posterior | E13 | Capacidades opcionales | Releases incrementales |

No se debe empezar una ola si su gate de entrada está rojo. Dentro de una ola, el orquestador puede asignar hasta tres paquetes sin archivos compartidos.

---

## E00 — Fundación y gobierno del repositorio

**Propósito:** convertir el esqueleto actual en una base reproducible y gobernada.

**Estado:** parcialmente completado. La solución, proyecto WPF, `global.json`, reglas, MeridianUI y build Release ya existen.

### P01 — Baseline reproducible

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E00-P01-T01 | Registrar el estado inicial y decisiones existentes | README, visión, arquitectura y plan enlazados; `git diff --check` limpio | Terra |
| E00-P01-T02 | Añadir `.editorconfig` para C#, XAML y Markdown | Formato consistente; `dotnet format --verify-no-changes` ejecutable | Terra |
| E00-P01-T03 | Configurar administración central de paquetes | `Directory.Packages.props`, versiones explícitas y comentario de licencia/uso | Terra + revisión Sol |
| E00-P01-T04 | Crear plantilla de ADR | `docs/adr/0000-template.md` con contexto, decisión, alternativas y consecuencias | Terra |
| E00-P01-T05 | Crear registro de riesgos y decisiones pendientes | `docs/Risk-Register.md` con propietario, probabilidad, impacto y mitigación | Terra |

### P02 — Automatización de calidad

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E00-P02-T01 | Crear script de verificación local | `eng/verify.ps1` ejecuta restore, format, build y test; propaga códigos de error | Terra |
| E00-P02-T02 | Añadir CI Windows | Pipeline en Windows que usa `global.json`, caché de NuGet y artifacts de test | Terra |
| E00-P02-T03 | Añadir inventario de dependencias y licencias | Documento con paquete, propósito, licencia y decisión de distribución | Terra |
| E00-P02-T04 | Definir política de fixtures | Datos sintéticos, sin PII ni capturas reales; validación en revisión | Terra |

**Gate E00:** clon limpio, restore/build/test reproducibles y documentación navegable.

---

## E01 — Límites de arquitectura y composición

**Propósito:** crear límites que permitan a agentes trabajar en paralelo sin acoplar WPF, Win32 y dominio.

### P01 — Decisiones y grafo de proyectos

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E01-P01-T01 | Redactar ADR de arquitectura modular | Dependencias permitidas, composición y razones documentadas | Sol |
| E01-P01-T02 | Crear proyectos de producción | Domain, Application, Storage, Windows y Export agregados a la solución | Terra |
| E01-P01-T03 | Crear proyectos de pruebas | Unitarias por capa y una suite de integración Windows separada | Terra |
| E01-P01-T04 | Configurar referencias de proyecto | Grafo sin ciclos; Domain no referencia paquetes de UI/SO | Terra |
| E01-P01-T05 | Mover recursos/código actual al límite correcto | Recorder conserva `App`, views y recursos MeridianUI; build sin cambios funcionales | Terra |

### P02 — Contratos transversales

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E01-P02-T01 | Definir reloj e identificadores inyectables | `IClock`, generador de IDs y fakes deterministas | Terra |
| E01-P02-T02 | Definir resultado y errores de aplicación | Errores tipados para validación, I/O, captura, permisos y cancelación | Terra + revisión Sol |
| E01-P02-T03 | Definir logging local mínimo | Interfaz sin PII, niveles, correlación por sesión y no-op para tests | Terra |
| E01-P02-T04 | Crear pruebas de arquitectura | Verifican referencias prohibidas y ausencia de WPF/Win32 en Domain | Terra |

**Gate E01:** grafo de dependencias aprobado, proyectos compilables y tests de arquitectura verdes.

---

## E02 — Dominio de documentos y tutoriales

**Propósito:** establecer la fuente única de verdad independiente de UI, almacenamiento y formato de exportación.

### P01 — Identidad, metadatos e invariantes

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E02-P01-T01 | Definir IDs tipados | `ProjectId`, `TutorialId`, `StepId`, `AssetId`; igualdad y JSON probados | Terra |
| E02-P01-T02 | Definir metadatos de proyecto | Nombre, idioma, fecha de creación/modificación, versión de esquema y preferencias | Terra |
| E02-P01-T03 | Definir `DocumentProject` | Colección ordenada de tutoriales, alta/baja y reglas de unicidad | Terra |
| E02-P01-T04 | Definir `Tutorial` | Título, descripción, audiencia, orden y ciclo de edición | Terra |
| E02-P01-T05 | Definir `Step` | Orden, título, descripción, captura, interacción y anotaciones | Terra |

**Orden de ejecución dentro de E02:** tras `E02-P01-T01` y `E02-P01-T02`, ejecutar `E02-P02-T01` a `E02-P02-T04` (geometría, captura, interacción y anotaciones), después `E02-P01-T05` (Step), `E02-P01-T04` (Tutorial) y `E02-P01-T03` (DocumentProject). Finalmente ejecutar `E02-P02-T05`, `E02-P02-T06` y P03. Los IDs y el alcance de las tareas no cambian; este orden evita placeholders artificiales y ciclos de modelo.

### P02 — Capturas, geometría y anotaciones

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E02-P02-T01 | Definir coordenadas normalizadas | Value objects restringidos a `[0,1]`, conversiones y casos límite | Terra |
| E02-P02-T02 | Definir metadatos de captura | Asset, dimensiones, DPI, monitor, ventana opcional y timestamp `DateTimeOffset` | Terra |
| E02-P02-T03 | Definir interacción | Tipo de clic, botón, posición y datos técnicos permitidos | Terra |
| E02-P02-T04 | Definir jerarquía de anotaciones | Marcador, rectángulo, flecha, texto y redacción; orden Z y geometría | Terra + revisión Sol |
| E02-P02-T05 | Definir operaciones de ordenamiento | Insertar, mover, duplicar y borrar sin índices inválidos | Terra |
| E02-P02-T06 | Cubrir invariantes con pruebas | Casos felices, límites, IDs inválidos, colecciones vacías y reordenamientos aleatorios | Terra |

### P03 — Esquema público interno

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E02-P03-T01 | Asignar versión inicial de esquema | Constante documentada y estrategia de compatibilidad hacia adelante | Terra |
| E02-P03-T02 | Crear fixtures canónicos | Proyecto vacío, tutorial simple, anotaciones completas y proyecto grande | Terra |
| E02-P03-T03 | Revisar el modelo contra todos los consumidores | Matriz Domain → recorder/editor/export; no faltan datos esenciales | Sol |

**Gate E02:** modelo determinista, serializable por DTO y cubierto en invariantes, sin referencias de infraestructura.

---

## E03 — Formato `.aegidocs` y almacenamiento local

**Propósito:** guardar, abrir, migrar y recuperar proyectos sin pérdida de información.

### P01 — Decisión del formato

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E03-P01-T01 | Evaluar carpeta administrada frente a contenedor ZIP | ADR analiza atomicidad, tamaño, edición, recuperación y seguridad | Terra; decisión Sol |
| E03-P01-T02 | Definir layout físico | Manifiesto, assets, thumbnails, recovery y archivos temporales documentados | Terra |
| E03-P01-T03 | Definir DTOs persistidos | DTOs separados de Domain, nombres JSON estables y nullable explícito | Terra |
| E03-P01-T04 | Definir migrador de esquema | Cadena incremental, idempotencia y rechazo de versión futura | Terra + revisión Sol |

### P02 — Repositorio y escritura segura

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E03-P02-T01 | Implementar serializer | Opciones centralizadas, enums estables, fechas ISO y pruebas golden | Terra |
| E03-P02-T02 | Implementar asset store | Hash opcional, rutas relativas, deduplicación básica y validación de extensión | Terra |
| E03-P02-T03 | Implementar guardado atómico | Escribir temporal, flush, reemplazar y conservar recuperación; pruebas de fallo inyectado | Terra + revisión Sol |
| E03-P02-T04 | Implementar `IProjectRepository` | Create/open/save/save-as con cancelación y errores tipados | Terra |
| E03-P02-T05 | Implementar autosave | Debounce, un escritor por proyecto, cancelación y señal de estado | Terra |
| E03-P02-T06 | Implementar recuperación | Detecta autosave más reciente, permite restaurar/descartar sin sobrescribir silenciosamente | Terra |

### P03 — Seguridad y compatibilidad

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E03-P03-T01 | Bloquear path traversal | Rechazar rutas absolutas, `..`, links y assets fuera del root | Terra |
| E03-P03-T02 | Limitar recursos al abrir | Límites de manifiesto, número/tamaño de assets y compresión si aplica | Terra + revisión Sol |
| E03-P03-T03 | Crear suite round-trip | Fixtures de todas las variantes conservan semántica tras save/open | Terra |
| E03-P03-T04 | Probar fallos de I/O | Disco lleno simulado, acceso denegado, archivo corrupto y cancelación | Terra |

**Gate E03:** proyecto canónico hace round-trip, guardado interrumpido no destruye la última versión y entradas maliciosas son rechazadas.

---

## E04 — Coordinador y ciclo de vida de grabación

**Propósito:** controlar la grabación con una máquina de estados testeable antes de conectar APIs nativas.

### P01 — Contratos de grabación

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E04-P01-T01 | Definir estados y transiciones | Idle, Starting, Countdown, Recording, Paused, Stopping y Faulted; tabla de transición | Terra + revisión Sol |
| E04-P01-T02 | Definir `IRecordingSession` | Start/pause/resume/stop/cancel, estado observable y cancelación | Terra |
| E04-P01-T03 | Definir eventos de input/captura | Records inmutables, orden temporal y correlación | Terra |
| E04-P01-T04 | Definir opciones | Delay de captura, debounce, calidad, cursor, monitor y hotkey con validación | Terra |

### P02 — Pipeline con servicios falsos

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E04-P02-T01 | Implementar máquina de estados | Transiciones serializadas y errores explícitos por comando inválido | Terra |
| E04-P02-T02 | Implementar cola de eventos | `Channel<T>` acotado, backpressure definido y procesamiento fuera del hook | Terra + revisión Sol |
| E04-P02-T03 | Implementar correlación clic-captura | Un evento aceptado produce como máximo un paso y conserva orden | Terra |
| E04-P02-T04 | Implementar debounce configurable | Evita dobles clics accidentales sin eliminar clics intencionales | Terra |
| E04-P02-T05 | Implementar cierre confiable | Stop drena lo aceptado; cancel descarta según contrato; ambos liberan servicios | Terra |
| E04-P02-T06 | Crear pruebas de carrera | Start/stop rápido, doble stop, cancel durante captura, error y cola llena | Terra |

**Gate E04:** 20 eventos falsos producen 20 pasos ordenados; todos los estados y carreras están cubiertos sin usar Win32.

---

## E05 — Captura de input global y filtrado de ventanas

**Propósito:** observar clics autorizados con el mínimo trabajo posible dentro del hook y sin dejar recursos activos.

### P01 — Interoperabilidad Win32

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E05-P01-T01 | Documentar contrato nativo | APIs, structs, arquitectura x64, errores y estrategia de liberación en ADR | Sol |
| E05-P01-T02 | Implementar wrappers P/Invoke | Firmas con tipos correctos, `SetLastError`, SafeHandle donde aplique y analyzers | Terra + revisión Sol |
| E05-P01-T03 | Crear hilo de hook | Message pump dedicado, inicio confirmado, cierre determinista y sin bloqueo UI | Terra |
| E05-P01-T04 | Normalizar mouse events | Mouse down/up, botón, coordenadas físicas y timestamp monotónico | Terra |
| E05-P01-T05 | Implementar hotkey de parada | Registro configurable, conflicto informado y liberación probada | Terra |

### P02 — Contexto y exclusiones

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E05-P02-T01 | Resolver ventana bajo el clic | HWND, PID, proceso y título sanitizado opcional | Terra |
| E05-P02-T02 | Excluir procesos/ventanas | AegiDocs siempre excluido; lista configurable y comparación robusta | Terra |
| E05-P02-T03 | Resolver monitor y DPI | Monitor, bounds físicos, escala y punto normalizado | Terra + revisión Sol |
| E05-P02-T04 | Mantener callback mínimo | Callback solo copia datos y encola; benchmark/documentación | Terra |
| E05-P02-T05 | Probar liberación | Cierre normal, excepción, cancelación y finalización de proceso no dejan hook | Terra |
| E05-P02-T06 | Ejecutar matriz manual | Mouse principal/secundario, doble clic, UAC, varias apps y DPI 100/125/150/200% | Terra QA |

**Gate E05:** input global estable durante 30 minutos, exclusión propia efectiva y cero hooks después de detener.

---

## E06 — Motor de captura de pantalla

**Propósito:** obtener imágenes correctas y eficientes con Windows Graphics Capture y errores comprensibles.

### P01 — Spike y decisión técnica

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E06-P01-T01 | Crear spike aislado de WGC | Captura una ventana y un monitor en Windows 10/11; resultados documentados | Terra |
| E06-P01-T02 | Revisar interop y threading | Decisión sobre dispatcher, D3D device, frame pool y apartment state | Sol |
| E06-P01-T03 | Definir fallback explícito | Qué pasa con ventanas protegidas, minimizadas o no capturables; sin fallback silencioso | Sol |

### P02 — Implementación productiva

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E06-P02-T01 | Implementar `IScreenCaptureService` | Captura cancelable con resultado tipado y ownership claro de buffers | Terra + revisión Sol |
| E06-P02-T02 | Gestionar device/frame pool | Reutilización segura, resize y `Dispose` determinista | Terra |
| E06-P02-T03 | Convertir y codificar imagen | PNG inicial, dimensiones preservadas, metadata mínima y streams liberados | Terra |
| E06-P02-T04 | Aplicar delay posclic | Delay configurable permite que la UI destino se estabilice sin bloquear input | Terra |
| E06-P02-T05 | Excluir AegiDocs | Ventana principal, indicador y diálogos no aparecen en la captura | Terra + revisión Sol |
| E06-P02-T06 | Gestionar cambios de monitor/DPI | Bounds y coordenadas se corresponden con la imagen final | Terra |

### P03 — Rendimiento y fallos

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E06-P03-T01 | Benchmark de captura | Tiempo p50/p95, memoria por paso y tamaño de PNG registrados | Terra QA |
| E06-P03-T02 | Probar sesiones largas | 500 capturas sin crecimiento de handles/memoria fuera del umbral acordado | Terra QA |
| E06-P03-T03 | Mapear errores al usuario | Acceso negado, contenido protegido, dispositivo perdido y cancelación | Terra |
| E06-P03-T04 | Añadir integración opt-in | Tests Windows etiquetados, fuera de unitarias rápidas | Terra |

**Gate E06:** capturas coinciden con clics en DPI objetivo, sesiones largas no filtran recursos y fallos no crean pasos incompletos.

---

## E07 — Experiencia de grabación WPF con MeridianUI

**Propósito:** hacer visible, controlable y segura la grabación sin exponer detalles de infraestructura.

**Lectura obligatoria para cada agente UI:** skill `meridianui-design`, `C:\Users\kevin\.MeridianUI\README.md`, `tokens-wpf.xaml`, `references/platform-wpf.md` y specimens relevantes de titlebar, sidebar, buttons, inputs y dialogs.

### P01 — Estados y navegación MVVM

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E07-P01-T01 | Diseñar matriz de estados UI | Idle, countdown, recording, paused, stopping, completed y faulted con acciones válidas | Terra UX + revisión Sol |
| E07-P01-T02 | Crear shell y navegación MVVM | Views sin lógica de negocio en code-behind; DI en composición | Terra |
| E07-P01-T03 | Crear `RecorderViewModel` | Comandos async, cancelación, busy/disabled y errores observables | Terra |
| E07-P01-T04 | Conectar primero a fakes | Todos los estados demostrables sin hooks ni WGC | Terra |
| E07-P01-T05 | Conectar servicios reales | Sustitución por DI sin cambios en la vista | Terra |

### P02 — Superficies del flujo

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E07-P02-T01 | Vista de preparación | Nombre del tutorial, fuente de captura, delay, hotkey y explicación de privacidad | Terra UI |
| E07-P02-T02 | Countdown accesible | Cuenta visible, cancelable, sin animaciones mayores de 200 ms | Terra UI |
| E07-P02-T03 | Indicador de grabación | Siempre visible, excluido de captura, estado y acciones pause/stop | Terra UI + revisión Sol |
| E07-P02-T04 | Resumen al detener | Número de pasos, fallos parciales y acciones revisar/descartar | Terra UI |
| E07-P02-T05 | Estados vacíos y de error | Copy es-MX, acción correctiva, detalles técnicos separables | Terra UI |
| E07-P02-T06 | Preferencias de grabación | Validación inline, defaults seguros y persistencia local | Terra UI |

### P03 — Calidad visual y accesibilidad

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E07-P03-T01 | Auditar tokens MeridianUI | Cero colores, radios, sombras, espaciado o tipografía ad hoc | Terra reviewer |
| E07-P03-T02 | Completar estados de controles | Hover, focus, pressed, disabled, selected, busy, success y error | Terra UI |
| E07-P03-T03 | Navegación por teclado | Tab order, shortcuts, Escape seguro y foco visible | Terra QA |
| E07-P03-T04 | Automatización accesible | Names, roles, labels y anuncios de estado principales | Terra |
| E07-P03-T05 | Visual QA multi-DPI | Capturas aprobadas a 100%, 125%, 150% y 200%; sin clipping | Terra QA + revisión Sol |

**Gate E07:** una persona puede iniciar, pausar, detener y entender errores usando mouse o teclado; UI aprobada contra MeridianUI.

---

## E08 — Workspace y editor de pasos

**Propósito:** convertir la grabación cruda en un tutorial organizado sin editar archivos manualmente.

### P01 — Workspace de proyecto

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E08-P01-T01 | Definir navegación del workspace | Proyecto/tutorial/paso seleccionado y rutas de retorno | Terra UX + revisión Sol |
| E08-P01-T02 | Implementar lista virtualizada | Miniaturas, número, título, estado dirty y selección; 500 pasos fluidos | Terra |
| E08-P01-T03 | Implementar panel de propiedades | Título, descripción, alt text y metadatos útiles con validación | Terra |
| E08-P01-T04 | Implementar alta y captura manual | Añadir paso vacío o importar captura con asset administrado | Terra |
| E08-P01-T05 | Implementar reordenamiento | Botones accesibles y drag/drop; orden persistido | Terra |

### P02 — Comandos y consistencia

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E08-P02-T01 | Editar y eliminar paso | Confirmación contextual, selección posterior determinista | Terra |
| E08-P02-T02 | Duplicar paso | Nuevo ID, asset compartido o copiado según ADR, anotaciones clonadas | Terra |
| E08-P02-T03 | Crear undo/redo | Command history acotada, nombres de acción y limpieza al abrir proyecto | Terra + revisión Sol |
| E08-P02-T04 | Integrar autosave | Indicador Guardando/Guardado/Error, sin escrituras simultáneas | Terra |
| E08-P02-T05 | Manejar cambios pendientes | Cerrar/abrir/salir ofrece guardar, descartar o cancelar | Terra |
| E08-P02-T06 | Añadir shortcuts | Nuevo, guardar, undo, redo, borrar y navegación sin colisiones | Terra |

### P03 — Pruebas del editor

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E08-P03-T01 | Unit tests de view models | Comandos, selección, validación, busy y error con servicios falsos | Terra QA |
| E08-P03-T02 | Pruebas de undo/redo | Secuencias largas y límites restauran estado exacto | Terra QA |
| E08-P03-T03 | Perfil de proyecto grande | 500 pasos, thumbnails lazy y memoria documentada | Terra QA |
| E08-P03-T04 | Auditoría MeridianUI | Reutiliza table/list/card/input/dialog; documenta extensiones | Terra reviewer |

**Gate E08:** grabación de 100 pasos se puede reorganizar y editar con autosave, undo/redo y rendimiento aceptable.

---

## E09 — Canvas de anotaciones y privacidad visual

**Propósito:** anotar capturas de forma no destructiva y producir salidas donde las redacciones sí sean irreversibles.

### P01 — Geometría y renderer

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E09-P01-T01 | Definir sistema de coordenadas | Imagen, viewport, zoom, pan, DPI y normalización documentados | Sol |
| E09-P01-T02 | Crear transformaciones puras | Image↔viewport con pruebas property-based y casos de resize | Terra |
| E09-P01-T03 | Crear renderer común | Composición determinista usada por preview y exportación | Terra + revisión Sol |
| E09-P01-T04 | Definir selección y handles | Hit testing, resize, rotate si aplica y límites de imagen | Terra |

### P02 — Herramientas

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E09-P02-T01 | Herramienta marcador de clic | Numeración automática, posición editable y estilo MeridianUI | Terra |
| E09-P02-T02 | Rectángulo y flecha | Crear, mover, redimensionar, color semántico limitado y undo | Terra |
| E09-P02-T03 | Texto | Edición inline, tamaño por tokens, contraste y bounds | Terra |
| E09-P02-T04 | Redacción sólida/pixelada | Preview claro; export flatten impide recuperar píxeles cubiertos | Terra + revisión Sol |
| E09-P02-T05 | Zoom y pan | Fit, 100%, rueda/teclado y foco preservado | Terra |
| E09-P02-T06 | Orden Z y borrado | Acciones deterministas, accesibles y cubiertas por undo/redo | Terra |

### P03 — Verificación de privacidad

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E09-P03-T01 | Separar original de salida | El proyecto conserva original; exportación usa copia aplanada explícita | Terra |
| E09-P03-T02 | Avisar sobre originales | UI explica que redactar una exportación no elimina el original del proyecto | Terra UX |
| E09-P03-T03 | Probar no reversibilidad | Pixel inspection demuestra que el output no contiene el área original | Terra QA + revisión Sol |
| E09-P03-T04 | Visual QA de herramientas | Alineación, contraste, handles y cursores a DPI objetivo | Terra QA |

**Gate E09:** todas las anotaciones sobreviven save/open; preview y export coinciden; la redacción exportada no revela píxeles originales.

---

## E10 — Pipeline de publicación, Markdown y PDF

**Propósito:** generar varios formatos a partir de una representación común y estable.

### P01 — Representación publicable

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E10-P01-T01 | Definir `PublicationDocument` | Portada, metadata, secciones, pasos, imágenes, warnings y tema | Sol |
| E10-P01-T02 | Crear builder Domain→Publication | Transformación pura, orden estable y validaciones pre-export | Terra |
| E10-P01-T03 | Crear asset resolver | Nombres deterministas, copia segura, imágenes aplanadas y cancelación | Terra |
| E10-P01-T04 | Definir reporte de exportación | Éxitos, warnings, errores, archivos y duración | Terra |

### P02 — Markdown

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E10-P02-T01 | Definir dialecto Markdown | CommonMark, encoding UTF-8, estructura de assets y escaping | Terra |
| E10-P02-T02 | Implementar renderer | Portada textual, índice, pasos numerados, alt text e imágenes relativas | Terra |
| E10-P02-T03 | Añadir opciones | Incluir/excluir metadata, numeración y tema sin cambiar Domain | Terra |
| E10-P02-T04 | Golden tests | Unicode, caracteres especiales, rutas y proyecto completo | Terra QA |

### P03 — PDF

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E10-P03-T01 | Resolver licencia/dependencia | ADR de QuestPDF u opción elegida, licencia compatible y versión centralizada | Sol |
| E10-P03-T02 | Diseñar tema MeridianUI de documento | Tipografía, colores, espaciado, portada y jerarquía derivados de tokens | Terra UI + revisión Sol |
| E10-P03-T03 | Implementar paginación | Portada, índice, headers/footers, pasos, imágenes y cortes controlados | Terra |
| E10-P03-T04 | Manejar imágenes grandes | Fit, orientación, calidad y límites de memoria | Terra |
| E10-P03-T05 | Smoke tests PDF | Archivo abre, páginas > 0, texto esencial extraíble y assets visibles | Terra QA |
| E10-P03-T06 | Visual regression | Render de páginas canónicas a PNG y comparación/revisión registrada | Terra QA + revisión Sol |

### P04 — UI de publicación

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E10-P04-T01 | Crear diálogo de exportación | Formato, destino, opciones, validación y resumen bajo MeridianUI | Terra UI |
| E10-P04-T02 | Mostrar progreso y cancelación | Estados busy/disabled, cancelación segura y reporte final | Terra |
| E10-P04-T03 | Abrir ubicación opcionalmente | Solo tras acción explícita y con errores controlados | Terra |

**Gate E10:** un fixture canónico genera Markdown y PDF consistentes, accesibles y visualmente aprobados; cancelar no deja archivos parciales.

---

## E11 — Hardening, seguridad, accesibilidad y rendimiento

**Propósito:** convertir una demo funcional en una beta confiable.

### P01 — Resiliencia y diagnósticos

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E11-P01-T01 | Manejar excepciones globales | Captura segura, mensaje útil, recovery y cierre controlado | Terra + revisión Sol |
| E11-P01-T02 | Implementar logs locales | Rotación, tamaño limitado, redacción de rutas/títulos sensibles | Terra |
| E11-P01-T03 | Crear paquete de diagnóstico | Exportación opt-in con preview del contenido | Terra |
| E11-P01-T04 | Recuperar sesión interrumpida | Detecta proyecto y autosave sin asumir consentimiento | Terra |

### P02 — Seguridad y privacidad

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E11-P02-T01 | Threat model | Assets, archivos manipulados, PII, hooks, clipboard, plugins futuros | Sol |
| E11-P02-T02 | Auditar dependencias | Vulnerabilidades, licencias, mantenimiento y superficie nativa | Terra + revisión Sol |
| E11-P02-T03 | Auditar persistencia | Traversal, archivos gigantes, symlinks, corrupción y permisos | Sol reviewer |
| E11-P02-T04 | Auditar captura | Consentimiento, indicador, exclusiones, cierre y contenido protegido | Sol reviewer |
| E11-P02-T05 | Publicar política de privacidad | Explica local-first, originales, exportación y futura IA opt-in | Terra |

### P03 — Rendimiento y compatibilidad

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E11-P03-T01 | Fijar presupuestos | Startup, memoria idle, p95 captura, editor 500 pasos y exportación | Sol |
| E11-P03-T02 | Perf tests repetibles | Scripts y resultados base; regresión visible en CI/manual | Terra QA |
| E11-P03-T03 | Matriz Windows | Windows 10/11, x64, DPI 100–200%, varios monitores y sesiones RDP | Terra QA |
| E11-P03-T04 | Stress de recorder | 30 min y 500 pasos; handles, memoria, orden y cierre | Terra QA |

### P04 — Accesibilidad y UX final

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E11-P04-T01 | Auditoría solo teclado | Todos los flujos MVP completables sin mouse salvo dibujo libre justificado | Terra QA |
| E11-P04-T02 | Auditoría lector de pantalla | Nombres, estados, errores, orden y cambios anunciados | Terra QA |
| E11-P04-T03 | Contraste y escalado | MeridianUI + Windows text scaling; sin clipping crítico | Terra QA |
| E11-P04-T04 | Revisión de copy | Español es-MX, tono operativo, consistencia y acciones claras | Terra reviewer |

**Gate E11:** cero hallazgos críticos/altos abiertos; presupuestos cumplidos o excepciones aprobadas en ADR.

---

## E12 — Empaquetado, beta y operación de releases

**Propósito:** distribuir una beta instalable, diagnosticable y reversible.

### P01 — Packaging

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E12-P01-T01 | Elegir MSIX o instalador alterno | ADR considera firma, updates, permisos, WGC y experiencia de desinstalación | Sol |
| E12-P01-T02 | Configurar identidad y versión | SemVer, assembly/file/package version desde una fuente | Terra |
| E12-P01-T03 | Crear paquete x64 | Release reproducible, iconos/marca oficiales y archivos mínimos | Terra |
| E12-P01-T04 | Validar instalación limpia | Instalar, ejecutar, grabar, exportar y desinstalar en VM limpia | Terra QA |
| E12-P01-T05 | Definir actualización | Manual para beta o updater explícito; nunca descarga silenciosa | Sol |

### P02 — Release readiness

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E12-P02-T01 | Crear checklist de release | Gates, versiones, licencias, privacidad, artifacts y rollback | Terra |
| E12-P02-T02 | Crear manual corto | Inicio rápido, grabación, edición, redacción, exportación y recuperación | Terra |
| E12-P02-T03 | Preparar ejemplos sintéticos | Proyecto demo sin datos reales, PDF y Markdown esperados | Terra |
| E12-P02-T04 | Ejecutar release candidate | Evidencia completa de build/tests/matriz/instalación | Orquestador + Sol reviewer |
| E12-P02-T05 | Etiquetar beta | Solo después de aprobación humana; changelog y hashes de artifacts | Orquestador |

**Gate E12:** paquete beta probado en equipo limpio, con documentación, privacidad, licencias y rollback.

---

## E13 — Roadmap posterior al MVP

Estas capacidades no deben colarse en epics anteriores salvo contratos de extensión pequeños y justificados.

### P01 — OCR y comprensión local

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E13-P01-T01 | Evaluar Windows OCR/Tesseract | Precisión, idiomas, tamaño, licencia, offline y rendimiento | Terra research + decisión Sol |
| E13-P01-T02 | Implementar OCR opt-in | Texto local asociado al paso, editable y descartable | Terra |
| E13-P01-T03 | Detectar controles | UI Automation primero; confianza visible y revisión humana | Terra + revisión Sol |

### P02 — Asistencia con IA

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E13-P02-T01 | Threat/privacy model de IA | Minimización, consentimiento, retención, proveedor y prompt injection | Sol |
| E13-P02-T02 | Definir `IAssistantProvider` | Contrato independiente, streaming/cancelación y proveedor fake | Terra |
| E13-P02-T03 | Gestionar secretos | Windows Credential Manager/DPAPI; nunca repo o JSON del proyecto | Terra + revisión Sol |
| E13-P02-T04 | Generar títulos/descripciones | Solo contenido seleccionado, diff visible, aceptar/rechazar por campo | Terra |
| E13-P02-T05 | Traducir y adaptar audiencia | Resultado revisable, idioma explícito y sin sobrescritura silenciosa | Terra |
| E13-P02-T06 | Medir calidad | Dataset sintético, rúbrica, costos, latencia y tasa de aceptación | Terra QA + Sol review |

### P03 — Más publicaciones e integración

| ID | Tarea | Resultado y validación | Ejecutor |
| --- | --- | --- | --- |
| E13-P03-T01 | HTML interactivo | Export offline, accesible, assets locales y tema MeridianUI | Terra |
| E13-P03-T02 | Word | Open XML, estilos estables, imágenes y prueba de apertura | Terra |
| E13-P03-T03 | PowerPoint | Layout por paso, notas y prueba de apertura | Terra |
| E13-P03-T04 | GIF/video | Timeline configurable, calidad/tamaño y datos redactados | Terra |
| E13-P03-T05 | Runtime de ayuda | Visor embebible y deep links desde aplicaciones | Terra + revisión Sol |

## 6. Cola inicial recomendada

El primer ciclo de ejecución debe seguir exactamente este orden:

1. E00-P01-T02 — `.editorconfig`.
2. E00-P01-T03 — administración central de paquetes.
3. E00-P01-T04 — plantilla ADR.
4. E00-P02-T01 — script local de verificación.
5. E01-P01-T01 — ADR de arquitectura modular.
6. E01-P01-T02 y E01-P01-T03 — proyectos de producción y pruebas.
7. E01-P01-T04 — referencias y grafo.
8. E01-P02-T01 y E01-P02-T02 — contratos transversales.
9. E01-P02-T04 — tests de arquitectura.
10. Ejecutar Gate E01 antes de comenzar E02.

El orquestador puede paralelizar 1–4 y, tras aprobar el ADR, 6–8 cuando no compartan `AegiDocs.slnx`, `Directory.*` o los mismos `.csproj`.

## 7. Métricas de calidad del MVP

- Build Release: 0 errores y 0 warnings.
- Unit tests: todos verdes; cobertura usada como señal, no como sustituto de casos de riesgo.
- Grabación: 20/20 eventos en flujo normal; 500 pasos en stress sin pérdida silenciosa.
- Recursos: ningún hook, hotkey, frame pool, stream o handle permanece después de stop/cancel.
- Persistencia: round-trip semántico y recuperación tras fallo de escritura.
- Rendimiento: presupuestos fijados en E11 antes de declarar beta.
- UI: todos los estados relevantes, teclado, DPI 100–200% y auditoría MeridianUI.
- Privacidad: cero red por defecto; redacciones irreversibles en outputs; originales claramente comunicados.
- Publicación: Markdown y PDF abren, conservan orden, texto, imágenes y alt text.

## 8. Condiciones de bloqueo reales

Un agente debe detener su tarea y devolverla al orquestador cuando encuentre:

- Una decisión de producto con dos comportamientos incompatibles.
- Necesidad de un paquete nuevo sin licencia o versión aprobada.
- Una API Windows cuya semántica de seguridad/threading no esté confirmada.
- Cambios simultáneos de otro agente en los mismos archivos.
- Datos o fixtures con posible información personal.
- Una prueba crítica imposible de automatizar sin equipo, permiso o sesión externa.

El bloqueo no autoriza ampliar el alcance ni sustituir Windows Graphics Capture, MeridianUI o la política local-first de forma silenciosa.

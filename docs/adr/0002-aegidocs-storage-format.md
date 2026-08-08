# ADR-0002: Formato de almacenamiento local para proyectos `.aegidocs`

- **Estado:** Aceptado — revisión Sol aprobada el 2026-08-06.
- **Fecha:** 2026-08-06
- **Decisores:** Orquestador de AegiDocs; responsable Storage; revisión Sol
- **Tarea(s) relacionada(s):** `E03-P01-T01`, `E03-P01-T02`, `E03-P01-T03`, `E03-P01-T04`, `E03-P02-T03`, `E03-P03-T01`, `E03-P03-T02`

## Contexto y problema

Un proyecto AegiDocs contiene un manifiesto versionado, capturas potencialmente sensibles, thumbnails y futuros datos de recovery. El formato debe conservar un proyecto local-first que sea recuperable ante cierre, disco lleno, bloqueo de archivos o fallo de serialización. También debe resistir entradas no confiables sin permitir path traversal, enlaces fuera del root, descompresión descontrolada o acceso a rutas ajenas.

El contrato de Domain ya fija `ProjectSchema.InitialVersion = 1`, el nombre persistido `schemaVersion` y los discriminadores de anotación. Storage será responsable de DTOs y JSON; no puede serializar directamente los records polimórficos de Domain. Aún falta decidir si `.aegidocs` será una carpeta administrada o un contenedor ZIP único.

Los riesgos principales son R-004 (privacidad), R-005 (persistencia) y R-006 (seguridad de archivos). La [política de fixtures](../Fixture-Policy.md) exige datos sintéticos, rutas relativas, límites de tamaño y pruebas adversariales deterministas.

## Drivers de decisión

- Preservar la última versión válida cuando un guardado es interrumpido.
- Mantener capturas y datos sensibles locales, sin red, telemetría ni servicios externos.
- Permitir actualizaciones incrementales de manifiesto y assets sin reescribir todas las capturas.
- Hacer recovery y diagnóstico local comprensibles, sin exponer contenido sensible en logs.
- Validar entradas no confiables antes de tocar rutas o consumir recursos.
- Mantener el MVP implementable con BCL y contratos Storage explícitos.

## Decisión

> Decisión propuesta: en el MVP, un proyecto `.aegidocs` será una **carpeta administrada** cuyo directorio raíz termina en `.aegidocs`. El manifiesto y cada asset se escriben como archivos independientes, con rutas relativas validadas. Los assets publicados son inmutables y versionados; una edición que cambie una captura produce un asset nuevo, nunca reemplaza bytes de un asset ya confirmado. No se utilizará ZIP como formato de trabajo ni de recovery del MVP.

La carpeta no se actualiza como una unidad atómica. El commit es el reemplazo atómico del manifiesto: un asset temporal se crea en el mismo directorio y volumen que su destino, se escribe, ejecuta `Flush(true)`, se cierra y se renombra a su nombre versionado. Después, el candidato de manifiesto se crea en el mismo directorio y volumen, se escribe, ejecuta `Flush(true)`, se cierra y se publica con `File.Replace` atómico y backup cuando exista manifiesto anterior, o con `File.Move` atómico cuando no exista. El reemplazo o movimiento exitoso del manifiesto es el único commit de la revisión.

Recovery elige entre manifiesto principal, backup y recovery por una revisión o generación explícita y validada, nunca por hora de modificación. El cleanup ocurre después del commit y solo puede retirar temporales o assets que no sean requeridos por ninguna revisión referenciada por manifiesto principal, backup o recovery válido. No puede eliminar assets potencialmente necesarios para recuperar la última versión confirmada.

`Create` y `SaveAs` construyen el nuevo root en un directorio temporal hermano del destino final y lo renombran al final. Un destino existente no se reemplaza implícitamente; requiere una operación de reemplazo explícita posterior y su propio contrato de recovery.

El layout exacto, extensiones internas, nombre de manifiesto, política de cleanup y límites numéricos se definen en `E03-P01-T02`. Este ADR solo decide la forma física y los límites de responsabilidad.

## Alcance

- Directorio raíz local con sufijo `.aegidocs` como unidad de proyecto.
- Manifiesto DTO versionado, assets, thumbnails y recovery dentro de un único root administrado.
- Guardado atómico por archivo y publicación final del manifiesto.
- Assets inmutables/versionados, backup del manifiesto y recovery por revisión/generación validada.
- Bloqueo cross-process con handle exclusivo durante una escritura; no se usan lock files persistentes.
- Validación canónica de root, rutas relativas, extensiones, reparse points, conteos y tamaños antes de abrir o guardar.
- Pruebas de round-trip, recuperación, bloqueo/antivirus, path traversal y límites de recursos.

## No alcance

- ZIP como formato de trabajo, backup, intercambio o distribución del MVP.
- Sincronización cloud, colaboración multiusuario, deduplicación entre proyectos o cifrado en reposo.
- Compresión, firma, cifrado, integración con Explorer o asociaciones de archivo.
- Un contrato público de importación/exportación de proyectos.

## Alternativas consideradas y trade-offs

| Alternativa | Ventajas | Desventajas y riesgos | Motivo para aceptar o descartar |
| --- | --- | --- | --- |
| Carpeta administrada `.aegidocs` | Assets actualizables de forma incremental; manifest atómico sin reescribir capturas; recovery y diagnóstico local directos; no hay extracción ZIP; límites se aplican por archivo; más sencilla para pruebas de fallo. | No es un único archivo para copiar; queda expuesta a locks de antivirus/indexador en archivos individuales; requiere cleanup de temporales y disciplina de root. | **Propuesta aceptada para MVP.** Prioriza recuperación, edición y validación sobre portabilidad. |
| ZIP `.aegidocs` único | Fácil de mover/adjuntar; potencial reducción de tamaño en contenido compresible; una sola ruta visible para la persona autora. | Cada edición suele requerir reescribir o reemplazar el contenedor completo; recovery más complejo; una interrupción puede afectar todos los assets; exige defensa adicional ante zip slip, entradas duplicadas, zip bombs y ratios de compresión; antivirus puede bloquear el archivo completo. | Rechazada para formato de trabajo del MVP. Puede reconsiderarse como exportación o empaquetado posterior con ADR y límites explícitos. |
| Carpeta temporal convertida a ZIP al guardar | Combina edición local y un archivo final. | Duplica I/O y espacio temporal; complica atomicidad, rollback y estados parciales; hereda riesgos ZIP y no mejora el recovery del formato de trabajo. | Descartada; añade complejidad sin resolver los riesgos prioritarios. |

## Consecuencias

### Positivas

- Una captura nueva no obliga a recomprimir ni reescribir el proyecto completo.
- El manifiesto puede actuar como commit point tras validar DTOs y assets ya escritos.
- Los fallos pueden aislarse a un temporal, un asset o el manifiesto de recovery sin destruir la última versión publicada.
- La validación de rutas no requiere extraer contenido arbitrario antes de comprobarlo.
- El formato apoya la inspección y reparación local con herramientas de desarrollo sin enviar datos fuera del equipo.

### Negativas y deuda aceptada

- Copiar o compartir un proyecto requiere preservar el árbol completo; el producto debe hacerlo explícito cuando llegue la UX correspondiente.
- File locks de antivirus, indexadores, backup o Explorer pueden retrasar un reemplazo atómico. Storage obtiene un handle exclusivo por proyecto; solo reintenta sharing violations con política acotada y cancelable, devuelve cancelación/error tipado y no usa un lock file persistente. No debe forzar ni borrar un archivo bloqueado.
- Assets huérfanos y temporales requieren una estrategia de limpieza conservadora que no borre contenido que podría permitir recovery.
- ZIP sigue siendo una posible necesidad de portabilidad futura, pero necesita un formato de intercambio separado y pruebas de seguridad específicas.

## Seguridad y privacidad

Las capturas, el manifiesto y los assets nunca salen del root local del proyecto por esta decisión. El almacenamiento no imprime contenido de capturas, rutas completas, texto de anotaciones, títulos de ventana ni otros datos potencialmente sensibles en logs.

- ¿Introduce tráfico de red, telemetría, secretos o servicios externos? **No.**
- ¿Puede incluir PII, credenciales, texto sensible o capturas? **Sí.** Todos los archivos se mantienen locales; las redacciones siguen siendo semántica de Domain y los exportadores deben aplanarlas. No se copian proyectos reales a fixtures, CI, documentación ni prompts.
- ¿Requiere validación de entradas no confiables, permisos Windows o aislamiento adicional? **Sí.** Toda ruta debe ser relativa y combinarse bajo un root canónico. Se rechaza cualquier reparse point descendiente; no se sigue. Tras abrir un handle, se verifica que su ruta final permanezca bajo el root canónico. Si la BCL no puede garantizar esa comprobación para una operación, la API nativa necesaria se aísla en Infrastructure.Windows/Storage, nunca se filtra a Domain. También se rechazan rutas absolutas, `..`, versiones futuras, extensiones no permitidas, duplicados lógicos y tamaños/conteos fuera de límite.

Antes de asignar memoria, abrir assets o decodificar imágenes, Storage valida presupuestos. `E03-P01-T02` fija y documenta límites para bytes de manifiesto, profundidad JSON, longitud de strings, número de tutoriales/pasos/anotaciones, conteo de assets, bytes por asset y total, píxeles/dimensiones, profundidad/longitud de rutas y cantidad/tamaño de recovery/temporales. `E03-P01-T03` los valida antes de allocation, parsing profundo o decoding.

## Impacto en MeridianUI y accesibilidad

No aplica directamente: esta decisión no crea UI. Los futuros estados de abrir, guardar, recovery, bloqueo y espacio insuficiente deberán llegar a Recorder como errores tipados, con copy es-MX, navegación de teclado y recursos MeridianUI, sin exponer rutas o contenido sensible innecesariamente.

## Plan de implementación o migración

1. `E03-P01-T02`: definir el layout del root, manifiesto, assets inmutables/versionados, thumbnails, temporales, backup y recovery; fijar extensiones, revisión/generación y todos los presupuestos de apertura.
2. `E03-P01-T03` y `T04`: crear DTOs Storage separados de Domain, validar los presupuestos antes de allocation/decoding y crear una cadena de migración idempotente que rechace versiones futuras antes de leer assets.
3. `E03-P02-T01` a `T03`: implementar serializer, asset store, bloqueo exclusivo cross-process y publicación: temporal en el mismo volumen, `Flush(true)`, cierre, rename de asset y `File.Replace`/`File.Move` atómico del manifiesto con backup.
4. `E03-P02-T04` a `T06`: implementar repositorio, autosave y recuperación que conserven la última versión válida.
5. `E03-P03-T01` a `T04`: añadir pruebas adversariales de root, enlaces, rutas, archivos corruptos, tamaños, disco lleno simulado, acceso denegado, cancelación y locks reproducibles.

No existen proyectos previos que migrar. Si posteriormente se adopta un paquete ZIP para intercambio, deberá ser otro formato/versionado o un exportador explícito; no se convertirá silenciosamente un proyecto de trabajo existente.

## Validación

- Guardar 500 pasos sintéticos debe actualizar solo los assets/manifiestos necesarios y mantener un manifiesto abierto consistente.
- Una interrupción antes de publicar el manifiesto no debe hacer que el proyecto abra un estado parcialmente escrito.
- Una interrupción o lock durante el reemplazo del manifiesto debe conservar la última versión válida o recovery de revisión/generación validada, sin eliminar datos confirmados.
- Abrir rutas absolutas, `..`, cualquier reparse point descendiente, rutas finales de handle fuera de root, manifests corruptos, versiones futuras, conteos excesivos o assets fuera de límite debe fallar antes de acceder al destino externo, asignar memoria no acotada o decodificar contenido.
- Pruebas de commit deben verificar `Flush(true)` y cierre antes de rename, backup del manifiesto, `File.Replace`/`File.Move` como commit, cleanup que preserve referencias de main/backup/recovery y `Create`/`SaveAs` mediante root hermano temporal sin reemplazo implícito de destino existente.
- Pruebas cross-process deben comprobar handle exclusivo, retry solo ante sharing violation, cancelación durante retry y ausencia de lock file persistente.
- La suite debe usar exclusivamente fixtures sintéticos y rutas relativas, conforme a `Fixture-Policy.md`.
- Validación manual en Windows: proyecto abierto en Explorer, antivirus/indexador activo, archivo temporal bloqueado y espacio reducido simulado; comprobar diagnóstico local mínimo y recuperación.
- Gate: `dotnet format AegiDocs.slnx --verify-no-changes`, `dotnet build AegiDocs.slnx --configuration Release`, pruebas Storage aplicables y `git diff --check`.

## Rollback

1. Si la implementación demuestra que no puede publicar manifiestos de forma confiable bajo locks habituales, detener el uso de la nueva ruta de guardado, liberar el handle exclusivo y conservar root, assets, backup y recovery sin limpieza destructiva.
2. Abrir únicamente el manifiesto de mayor revisión/generación validada; dejar los temporales intactos para diagnóstico/recovery y devolver un error tipado a la aplicación.
3. Corregir la publicación atómica o reemplazar esta decisión mediante ADR antes de habilitar ZIP. Verificar que no se perdió el último proyecto válido y comunicar únicamente el estado, no datos de sus capturas.

## Revisión Sol solicitada

La revisión Sol ya confirmó los requisitos de protocolo que se incorporaron arriba. Antes de aceptar este ADR, el orquestador debe confirmar que `E03-P01-T02` y `T03` los mantienen explícitos: publicación/flush/reemplazo en NTFS, locks por antivirus/indexadores, rechazo de reparse points y verificación de handles, presupuestos previos a allocation/decoding, y un formato de intercambio futuro mediante decisión independiente.

## Enlaces relacionados

- [Plantilla ADR](0000-template.md)
- [Arquitectura modular](0001-modular-architecture.md)
- [Plan maestro](../Implementation-Plan.md)
- [Registro de riesgos](../Risk-Register.md)
- [Política de fixtures](../Fixture-Policy.md)
- [Contrato de esquema Domain](../../src/AegiDocs.Domain/Serialization/ProjectSchema.cs)

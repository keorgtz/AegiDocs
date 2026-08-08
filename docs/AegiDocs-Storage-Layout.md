# Layout físico de proyectos `.aegidocs`

## Propósito y alcance

Este documento concreta el formato de trabajo decidido en [ADR-0002](adr/0002-aegidocs-storage-format.md): un proyecto es una carpeta administrada cuyo nombre termina en `.aegidocs`. Es un contrato de Storage del MVP, no un formato de intercambio, ZIP ni una API pública.

El contenido puede contener capturas y texto sensible. Todo se mantiene en el root local seleccionado; Storage no usa red, telemetría ni logs que incluyan píxeles, texto de anotaciones, títulos de ventana, rutas completas o datos de interacción.

## Árbol canónico

```text
manual-de-prueba.aegidocs/
├── manifest.json
├── manifest.backup.json
├── assets/
│   └── 6b9d…e4a1/
│       └── v0000000000000042.png
├── thumbnails/
│   └── 6b9d…e4a1/
│       └── v0000000000000042.png
├── recovery/
│   └── manifest-g0000000000000041.json
└── tmp/
    └── asset-6b9d…e4a1-v0000000000000042-<operation-id>.tmp
```

Los caracteres elididos en el ejemplo son GUIDs canónicos en minúsculas. Los nombres reales no usan `…`.

| Ubicación | Propietario | Contrato |
| --- | --- | --- |
| `manifest.json` | Storage writer | Manifiesto DTO actual. Es el único punto de commit de una revisión publicada. |
| `manifest.backup.json` | Storage writer | Copia de la revisión publicada inmediatamente anterior, creada por `File.Replace`. No se edita en sitio. |
| `assets/<asset-guid>/v<revision>.png` | Asset store | Captura publicada, inmutable y versionada. Una modificación crea un GUID o revisión nuevos; nunca sobrescribe bytes confirmados. |
| `thumbnails/<asset-guid>/v<revision>.png` | Thumbnail store | Derivado local de un asset publicado. También puede contener información de captura y recibe los mismos controles de privacidad. Se puede regenerar; no es fuente de verdad. |
| `recovery/manifest-g<generation>.json` | Recovery manager | Candidato de recovery autocontenido y validado, asociado a una generación del manifiesto. No se elige por fecha de modificación. |
| `tmp/` | Operación activa | Temporales por archivo dentro del mismo volumen. No son parte de una revisión ni se abren como datos de proyecto. |

No existen otros archivos o directorios permitidos dentro de un root v1. Un proyecto con una entrada desconocida, un nombre no canónico o una extensión no permitida se rechaza en modo seguro; una versión futura debe declarar su layout mediante una migración explícita, no ser tolerada silenciosamente.

## Nombres, rutas y extensiones

- El root debe ser un directorio existente o creable, con nombre final exacto `.aegidocs` sin espacios posteriores. `Create` y `SaveAs` sólo aceptan un destino que no exista.
- Los nombres internos son ASCII minúsculas. IDs: GUID `D` minúsculo (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`).
- `revision` y `generation` son enteros positivos, decimales, rellenos a 16 dígitos: `v0000000000000042`, `g0000000000000042`.
- Los únicos nombres fijos son `manifest.json`, `manifest.backup.json`, `assets`, `thumbnails`, `recovery` y `tmp`.
- La extensión de captura y thumbnail v1 es exclusivamente `.png`; el manifiesto no acepta una ruta aportada por la persona usuaria. Storage deriva la ruta a partir de `assetGuid` y `revision` ya validados.
- Toda ruta persistida es relativa al root, usa `/` al serializar, no tiene raíz, unidad, UNC, `.` o `..`, separadores repetidos, segmentos vacíos, dos puntos, nombres reservados de Windows ni caracteres de control.
- Profundidad máxima: 4 segmentos persistidos desde el root (`assets/<id>/<file>`). Longitud máxima de ruta relativa: 180 caracteres; segmento: 80 caracteres. El root canónico completo más ruta relativa no puede exceder 240 caracteres en v1.

Storage compara nombres con ordinal case-sensitive tras validar que son minúsculas. Esto evita ambigüedad en volúmenes Windows case-insensitive y hace estable el manifiesto.

## Generaciones, publicación y recuperación

Cada manifiesto incluye una `generation` positiva y un `schemaVersion`. La generación aumenta exactamente una vez por publicación correcta. Los assets referenciados por el manifiesto se escriben y confirman antes de publicar ese manifiesto.

1. El writer obtiene un handle exclusivo del root para la duración de la escritura. Es un handle de sistema; **no se crea un lock file persistente**.
2. Escribe un asset nuevo en `tmp/` en el mismo volumen, hace `Flush(true)`, cierra el handle y hace rename a su ruta final versionada en `assets/`.
3. Genera y valida el manifiesto candidato en `tmp/`, hace `Flush(true)`, cierra el handle y lo publica como `manifest.json` mediante `File.Replace` con `manifest.backup.json`. Para el primer guardado usa `File.Move` atómico.
4. Puede conservar un manifiesto de recovery validado en `recovery/` antes del commit. No lo promueve por timestamp: sólo si su generación, esquema, referencias y presupuestos son válidos y su generación es mayor que la del manifiesto principal o backup válido.
5. Sólo después de un commit exitoso puede limpiar temporales y assets no referenciados por ningún manifiesto principal, backup o recovery válido.

Al abrir, el orden de preferencia es: manifiesto principal válido de mayor generación, backup válido, y después recovery válido de mayor generación. Un manifiesto corrupto o futuro no autoriza eliminar los otros candidatos. La implementación conserva los candidatos para diagnóstico local mínimo y devuelve un error tipado sin imprimir contenido.

`Create` y `SaveAs` construyen todo el árbol en un directorio temporal **hermano** de destino, por ejemplo `.manual-de-prueba.aegidocs-create-<operation-id>.tmp`, y renombran el root al final. Esos directorios hermanos no se consideran proyectos y no se escanean al abrir otro proyecto. Un destino existente nunca se reemplaza de forma implícita.

## Presupuestos obligatorios v1

Los límites se validan antes de asignar buffers grandes, recorrer árboles no confiables o decodificar imágenes. Son límites de apertura y también de escritura; guardar no puede crear un proyecto que abrir rechazaría.

| Recurso | Límite | Justificación |
| --- | ---: | --- |
| Manifiesto principal, backup o recovery individual | 8 MiB | Suficiente para miles de referencias y texto breve; acota parsing y recovery. |
| Recovery total | 3 manifiestos / 24 MiB | Conserva alternativas útiles sin convertir recovery en un almacén ilimitado. |
| Profundidad JSON | 64 | Muy por encima del DTO previsto, pero evita anidamiento hostil. |
| Longitud de string JSON | 16,384 UTF-16 code units | Cubre títulos/descripciones extensos sin permitir blobs de texto en manifiesto. |
| Propiedades por objeto JSON | 128 | Protege DTOs planos y detecta formas inesperadas. |
| Elementos por arreglo JSON | 25,000 | Cubre la colección más grande permitida sin crecimiento no acotado. |
| Tutoriales por proyecto | 500 | Supera el escenario de validación de 500 pasos y mantiene edición manejable. |
| Pasos por tutorial | 2,000 | Permite procedimientos largos; evita un único agregado desproporcionado. |
| Pasos totales por proyecto | 20,000 | Límite explícito para memoria, exportación y tiempo de apertura. |
| Anotaciones por paso | 100 | Suficiente para edición densa; una captura con más suele requerir dividir el paso. |
| Anotaciones totales | 200,000 | Derivado de los límites anteriores, sin permitir desbordamiento de conteo. |
| Assets publicados | 25,000 | Incluye versiones que siguen referenciadas por main/backup/recovery. |
| Bytes por asset PNG | 32 MiB | Cubre capturas de escritorio normales sin permitir archivos arbitrarios gigantes. |
| Bytes de assets publicados totales | 8 GiB | Presupuesto de proyecto local explícito y verificable antes de copiar/escribir. |
| Thumbnails | 25,000 archivos; 512 KiB cada uno; 512 MiB total | Derivados pequeños, suficientes para grids sin competir con assets. |
| Dimensiones de asset decodificado | 7,680 × 4,320 px | Cubre 4K amplio; evita dimensiones de imagen adversariales. |
| Píxeles decodificados por asset | 33,177,600 | Equivale al máximo 7,680 × 4,320 y permite calcular memoria antes de decode. |
| Bytes de imagen decodificada por asset | 128 MiB RGBA | Presupuesto para un buffer de cuatro canales; no se decodifica si excede. |
| `tmp/` dentro del root | 32 archivos / 1 GiB total | Permite una operación y reintentos acotados sin limpieza riesgosa. |
| Directorio temporal hermano | 1 root activo por destino / 10 GiB total | Soporta `Create`/`SaveAs`; se abandona de forma conservadora al fallar. |
| Antigüedad para elegibilidad de cleanup de temp | 24 horas | Evita borrar prematuramente tras crash; el cleanup aún exige nombre, root y ausencia de referencia válidos. |

Los límites de conteos se comprueban sin multiplicaciones inseguras. Cualquier suma de bytes, píxeles o elementos usa comprobación de overflow y rechaza el proyecto antes de continuar.

## Orden de validación para abrir

La apertura trata el root y todo su contenido como no confiable. Este orden es obligatorio:

1. Validar sintácticamente el argumento del root: directorio, sufijo `.aegidocs`, longitud y ausencia de segmentos relativos o raíz inesperada. Canonicalizarlo una vez.
2. Abrir el directorio root sin seguir enlaces y verificar sus atributos. Rechazar de inmediato todo reparse point, incluidos symlinks, junctions, mount points y placeholders de proveedor cloud.
3. Enumerar únicamente el primer nivel con conteo acotado; validar nombres fijos y atributos antes de descender. Cada directorio descendiente se abre/enumera con el mismo control de reparse point y límite de profundidad.
4. Consultar atributos y longitudes de `manifest.json`, backup y recovery candidatos antes de abrirlos; rechazar archivos no regulares, tamaño excesivo, extensiones incorrectas o cantidad excesiva de recovery/temp.
5. Leer como máximo 8 MiB del manifiesto candidato mediante parsing UTF-8 streaming. Aplicar profundidad, propiedades, strings y elementos máximos durante parsing; validar `schemaVersion` y `generation` antes de construir DTOs completos.
6. Validar IDs, unicidad, conteos de tutoriales/pasos/anotaciones, rutas derivadas, extensiones, referencias y presupuestos acumulados. Todavía no abrir ni decodificar assets.
7. Para cada asset referenciado, verificar atributos, archivo regular, tamaño y ruta canónica derivada. Abrir un handle y comprobar que su ruta final permanezca bajo el root canónico; cerrar el handle si falla.
8. Leer sólo la cabecera PNG necesaria para comprobar firma, dimensiones, píxeles y presupuesto RGBA. Decodificar sólo después de esas comprobaciones y con un buffer acotado.
9. Elegir main, backup o recovery exclusivamente por generación válida. Entregar objetos DTO/domain sólo cuando todas sus referencias requeridas han pasado validación.

La falta de un thumbnail no invalida un proyecto: se marca para regeneración local tras abrir el asset válido. Una captura requerida faltante, corrupta o fuera de presupuesto invalida ese manifiesto candidato.

## Política explícita de reparse points y TOCTOU

V1 adopta una política de denegación total: **se rechaza cualquier reparse point en el root, cualquier descendiente, archivo, directorio temporal interno o directorio temporal hermano administrado**. Storage no sigue ni intenta resolver enlaces simbólicos, junctions, mount points, puntos de análisis de cloud ni tipos desconocidos.

Las comprobaciones por ruta no bastan frente a cambios entre enumeración y apertura. Después de abrir cualquier archivo o directorio relevante, la implementación debe verificar que el handle final siga bajo el root canónico y que no sea reparse point. Si una API BCL no ofrece esa garantía, el acceso se encapsula detrás de una abstracción testeable de Storage/Windows; no se degrada a seguir el enlace. Un fallo de esa comprobación aborta la operación sin tocar rutas externas.

La consecuencia aceptada es que un proyecto situado dentro de una carpeta sincronizada o redirigida mediante reparse point puede ser rechazado por v1. Es preferible a que una operación de save, recovery o cleanup salga silenciosamente del root. Cualquier excepción futura requiere ADR, pruebas adversariales y revisión de seguridad.

## Ownership, bloqueo y limpieza

- El writer tiene ownership exclusivo mediante handle durante una operación de escritura; readers no cambian el árbol y nunca eliminan contenido.
- Sólo Storage crea o borra `tmp/`, temporales hermanos, recovery y thumbnails. La UI solicita operaciones; no construye rutas ni llama a `File.*`.
- Un retry está permitido sólo para sharing violations, con backoff acotado y `CancellationToken`; no se fuerza la eliminación de un archivo bloqueado por antivirus, indexador o Explorer.
- Cleanup valida el nombre canónico, que el archivo esté bajo el root y no sea reparse point, y que no esté referenciado por main, backup ni recovery válidos. Si no puede probarlo, conserva el archivo.
- No se eliminan manifests principales/backups, assets confirmados, roots existentes de `SaveAs` ni temporales recientes como parte de una apertura fallida.
- Los diagnósticos registran únicamente categoría, código de error, operación e identificador opaco de proyecto/asset cuando sea necesario; nunca contenido sensible ni rutas completas.

## Implicaciones para tareas posteriores

- `E03-P01-T03` implementa DTOs y parser streaming que hagan cumplir estos límites antes de allocation o decode.
- `E03-P02-T02` implementa la derivación de rutas, publicación inmutable de assets y validación de headers PNG.
- `E03-P02-T03` implementa flush, cierre, replace/move, backup y handle exclusivo conforme al protocolo.
- `E03-P02-T06` implementa selección por generación y cleanup conservador.
- `E03-P03-T01` y `T02` añaden pruebas de traversal, reparse, conteos, bytes, píxeles, manifests futuros y recovery.

## Decisiones explícitas

1. PNG es el único formato de asset/thumbnail v1 para reducir superficie de decoder y ambigüedad de extensiones.
2. Los nombres derivan de identificadores y revisión, nunca de nombre de ventana, aplicación, persona o archivo de origen.
3. El manifiesto, no el reloj del sistema ni el thumbnail, determina qué revisión está publicada.
4. Ante duda de ownership, enlace, referencia o límite, Storage conserva datos y rechaza la operación; no intenta una reparación destructiva.

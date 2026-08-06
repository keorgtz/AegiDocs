# Política de fixtures y assets de prueba

## Propósito y alcance

Esta política define cómo crear, almacenar, revisar y retirar fixtures, archivos de ejemplo y assets usados por pruebas de AegiDocs. Aplica a todos los proyectos de producción, pruebas, scripts de `eng/`, documentación técnica y artefactos de CI.

Un fixture es un dato, proyecto, archivo, imagen o conjunto de eventos usado para probar un contrato. No es material de demostración de usuarios ni una copia de una captura tomada durante la grabación de un flujo real.

La política materializa los principios de **privacidad local por defecto**, **minimización de datos** y **control editorial de la persona autora**. Los fixtures no deben crear una vía indirecta para introducir material sensible en el repositorio, los binarios, los logs, los exports de prueba o servicios externos.

## Regla no negociable: contenido sintético

Todo fixture y asset de prueba debe ser sintético: creado expresamente para la prueba, generado de forma determinista o derivado exclusivamente de otros datos sintéticos aprobados.

Está prohibido incorporar, incluso si se pretende anonimizar posteriormente:

- PII o datos personales: nombres reales, correos, teléfonos, direcciones, identificadores gubernamentales, rostros, voces, firmas, ubicaciones o conversaciones privadas.
- Secretos: contraseñas, tokens, claves API, certificados, llaves privadas, cookies, cadenas de conexión, credenciales de prueba reutilizables o valores con formato de secreto.
- Capturas, grabaciones, títulos de ventana, logs, OCR, exportaciones o proyectos obtenidos de una persona usuaria, cliente, prospecto, proveedor, empleado o sistema de producción.
- Datos internos no públicos, información contractual, financiera, sanitaria, educativa, de seguridad o propiedad intelectual de terceros.
- Contenido descargado de servicios externos si no existe una licencia y una necesidad de producto explícitas; para pruebas se debe recrear sintéticamente la forma mínima necesaria.

No se permite "sanear después" un asset real. Si su origen no puede demostrarse como sintético, se considera sensible y no puede entrar al repositorio ni a CI.

## Relación con captura local-first y redacción de exportaciones

- La aplicación captura localmente únicamente después de consentimiento visible; los fixtures no reducen ni simulan esa exigencia con datos reales.
- Las capturas reales de una sesión son datos de proyecto local y no fixtures. No se copian a `tests`, `docs`, issues, pull requests, artefactos de CI ni prompts de IA.
- Los fixtures de redacción deben demostrar que una redacción sustituye u oculta áreas sintéticas antes de producir una exportación. Una exportación de prueba nunca debe conservar píxeles, texto, metadatos o rutas del asset sintético original que se marcó como redactado.
- Las pruebas de exportación solo publican resultados en directorios temporales o artefactos de CI de acceso controlado. Los artefactos no se usan como muestras públicas y se eliminan según la retención del pipeline.
- Ninguna prueba, generador o validador de fixtures realiza red, telemetría o carga de contenido. Las dependencias deben ser locales y deterministas.

## Clasificación y ubicación

| Clase | Propósito | Ubicación preferida | Reglas adicionales |
| --- | --- | --- | --- |
| Unitaria | Validar una regla aislada, serialización pequeña o conversión. | Datos inline o `tests/<Area>.Tests/Fixtures/Unit/`. | Preferir builders/factories en código; no incluir binarios si un record sintético basta. |
| Integración | Ejercitar persistencia, contratos entre capas o round-trip. | `tests/<Area>.IntegrationTests/Fixtures/Integration/`. | Incluir manifiestos y assets mínimos sintéticos; cada caso declara esquema y resultado esperado. |
| Visual | Comparar render, DPI, anotaciones y redacción. | `tests/<Area>.Tests/Fixtures/Visual/` y baselines aprobados. | PNG/WebP sintético de tamaño mínimo; no incrustar metadatos EXIF/XMP ni texto realista que parezca PII. |
| Adversarial | Probar límites, corrupción, path traversal, tamaños y entradas maliciosas. | `tests/<Area>.Tests/Fixtures/Adversarial/`. | Debe ser inerte, pequeño y claramente etiquetado; no incluir malware, secretos reales ni archivos que ejecuten código. |

Los assets compartidos entre suites deben residir en `tests/Shared.Fixtures/` solo después de demostrar que su contrato es estable. Evitar una carpeta común sin propietario. Los resultados generados se escriben fuera del árbol versionado, por ejemplo en `artifacts/` o en un directorio temporal, y nunca se promueven a fixture sin revisión.

## Nombres, metadatos y almacenamiento

- Usar nombres en inglés, minúsculas y `kebab-case`: `empty-project-v1.json`, `redacted-click-step.png`, `oversized-manifest-v1.json`.
- El sufijo `-v<schema>` identifica el esquema o contrato; actualizarlo al cambiar deliberadamente el formato. No sobrescribir un baseline para ocultar una regresión.
- Todo fixture no trivial debe tener junto a él una nota `README.md` o comentario de test con: clase, origen sintético, generador o autor, contrato ejercitado y condiciones de actualización.
- JSON, XML y texto se guardan en UTF-8 sin BOM, con saltos de línea LF y formato estable. Fechas, IDs y orden de colecciones deben ser fijos.
- Imágenes visuales se guardan con dimensiones explícitas y sin metadatos no necesarios. Se prefiere una paleta y texto ficticio como `Usuario de prueba`, `example.invalid` o IDs no asociables a personas reales.
- Las rutas dentro de fixtures son relativas al fixture root. Nunca usar rutas absolutas, perfiles locales, nombres de equipo, unidades de red ni enlaces fuera de la carpeta de prueba.
- No incluir archivos mayores que el mínimo requerido por la prueba. Cualquier fixture que exceda el límite documentado de la suite requiere justificación, prueba de límite y revisión de seguridad.

## Generación determinista

Los generadores de fixtures son código de prueba y deben ser reproducibles en una máquina limpia.

- Usar reloj, generador de IDs, semilla aleatoria y zona horaria inyectables/fijos. Persistir `DateTimeOffset` con offset explícito.
- Fijar el orden de elementos, nombres de archivo, compresión, cultura y codificación. No depender del usuario actual, el reloj del SO, DPI activo, ventana visible, red o estado global.
- Las capturas visuales se generan desde canvas/datos sintéticos o un renderer determinista; no mediante una captura de escritorio.
- Un generador debe ofrecer un comando documentado, producir un diff estable al ejecutarse dos veces y validar que su salida cumple esta política antes de reemplazar un baseline.
- Si un resultado depende inevitablemente de plataforma o versión de render, separar baselines por condición explícita y documentar la razón; no aceptar variación silenciosa.

## Revisión, CI y cambios de baseline

Cada cambio que añada o modifique fixtures debe revisarse junto con el código que consume el fixture.

- La persona revisora verifica origen sintético, clasificación, nombre, tamaño, ausencia de PII/secretos y necesidad del asset.
- Los cambios visuales muestran el antes/después, la resolución/DPI y el caso de redacción aplicable. Un baseline cambia solo cuando el comportamiento esperado está documentado.
- Las pruebas de integración y adversariales deben cubrir tanto la aceptación de entradas válidas como el rechazo seguro de las inválidas.
- CI ejecuta los generadores/verificadores aplicables, pruebas que consumen los fixtures y detectores de secretos sobre el diff y el árbol de pruebas. Un hallazgo bloquea la integración.
- Los logs de CI no imprimen el contenido completo de assets, manifests o exports que podrían ser sensibles; registran identificadores de fixture, hash y diagnóstico mínimo.
- No se descargan fixtures en CI ni se suben a servicios de terceros. Los artefactos de pruebas se limitan a la retención y controles del pipeline.

## Detección, reporte y remoción de material sensible

Si se sospecha que un fixture, asset, log o artefacto contiene material real o sensible:

1. Detener su uso y la publicación de artefactos relacionados. No copiarlo a tickets, chats, prompts o documentación.
2. Informar de inmediato al orquestador y a la persona responsable de producto y privacidad, indicando únicamente la ruta, commit/ejecución afectada y tipo de sospecha; no reproducir el contenido sensible.
3. Eliminar el material del árbol de trabajo y de los artefactos accesibles conforme a los procedimientos del repositorio. Si ya se versionó o publicó, escalar para limpieza de historial, revocación de secretos y evaluación de incidente antes de continuar.
4. Reemplazarlo por un fixture sintético mínimo, añadir una prueba o regla que evite la recurrencia y registrar/actualizar el riesgo de privacidad cuando corresponda.
5. Documentar en el handoff la remoción, la evidencia de que CI dejó de distribuir el material y cualquier autoridad externa necesaria. No cerrar el incidente por una simple redacción visual si el original sigue accesible.

La prioridad es contener la exposición; preservar una copia para depuración no está autorizado sin aprobación explícita de privacidad y seguridad.

## Checklist obligatorio de PR y handoff

Quien implemente una tarea que toque fixtures debe confirmar explícitamente:

- [ ] Cada fixture/asset es sintético y su origen puede explicarse sin referir datos reales.
- [ ] No se añadieron PII, secretos, capturas reales, contenido de cliente, logs de producción ni metadatos de máquina/persona.
- [ ] La clasificación, ubicación, nombre, esquema y tamaño cumplen esta política.
- [ ] El contenido es determinista; relojes, IDs, orden, cultura y semillas están controlados.
- [ ] Las rutas son relativas, las entradas adversariales son inertes y los límites se prueban.
- [ ] Las pruebas de redacción verifican que el output/export no revela la región ni los metadatos marcados como redactados.
- [ ] No existe red, telemetría, descarga ni publicación externa implícita durante generación o ejecución.
- [ ] Se ejecutaron los comandos de prueba aplicables y se informa su resultado exacto.
- [ ] Se revisó el diff por material sensible y los verificadores/CI aplicables pasan.
- [ ] El handoff incluye archivos modificados, contrato validado, riesgos residuales y, si aplica, el procedimiento de remoción seguido.

Un checklist incompleto bloquea la aceptación de la tarea. Cuando haya duda sobre el origen o la sensibilidad, el fixture se rechaza y se crea uno sintético nuevo.

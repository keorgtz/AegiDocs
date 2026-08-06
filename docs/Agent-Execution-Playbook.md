# Playbook de ejecución con agentes

## 1. Objetivo

Este playbook permite ejecutar el [plan maestro](Implementation-Plan.md) principalmente con `gpt-5.6-terra`, conservando calidad mediante contratos pequeños, separación de responsabilidades, evidencia automática y revisiones selectivas con `gpt-5.6-sol`.

La unidad de trabajo no es un epic completo: es una tarea `E##-P##-T##` o una subdivisión todavía menor. Terra funciona mejor cuando recibe una frontera clara, archivos propietarios, comportamiento observable y pruebas concretas.

## 2. Topología recomendada

Con cuatro slots disponibles:

| Slot | Rol | Modelo habitual | Responsabilidad |
| --- | --- | --- | --- |
| 1 | Orquestador raíz | Sol o el modelo principal disponible | Mantener plan, resolver dependencias, asignar archivos, integrar y ejecutar gates |
| 2 | Implementador | Terra | Cambios de producción de una sola tarea |
| 3 | QA/Test | Terra | Pruebas, fixtures, repros, benchmarks o validación visual sin reescribir la solución |
| 4 | Explorador o reviewer | Terra; Sol en riesgos altos | Investigación read-only, revisión de contratos, seguridad o interop |

No es obligatorio ocupar todos los slots. Para tareas pequeñas es preferible un implementador y un reviewer. La concurrencia solo es útil cuando los archivos propietarios no se cruzan.

## 3. Política de modelos

### Usar `gpt-5.6-terra` por defecto para

- Scaffolding definido por un ADR aprobado.
- Entidades, DTOs, mappers y validaciones con contratos claros.
- Repositorios, serializers y migraciones ya especificados.
- View models, views y recursos WPF basados en patrones MeridianUI existentes.
- Exportadores con una representación intermedia congelada.
- Unit tests, fixtures, golden files, documentación y scripts.
- Reproducción de bugs, benchmarks y matrices manuales guiadas.
- Refactors mecánicos dentro de un módulo.

### Escalar a `gpt-5.6-sol` para

- ADRs que cambian límites o contratos públicos.
- Diseño inicial de P/Invoke, WGC, threading, DPI y ownership nativo.
- Race conditions persistentes o memory/handle leaks difíciles.
- Threat modeling, privacidad, secretos, parsing de archivos no confiables y release gates.
- Revisión final de epics E01, E03, E04, E05, E06, E09, E10, E11 y E12.
- Conflictos entre MeridianUI, accesibilidad y una necesidad funcional nueva.

### Regla de ahorro

No pedir a Sol que implemente una tarea mecánica completa. Pedirle que produzca o revise el contrato/ADR, entregar ese contrato a Terra y regresar a Sol solo con el diff, evidencia y preguntas puntuales. Una revisión Sol por gate suele ser más valiosa que usar Sol en cada archivo.

## 4. Roles de agentes

### Orquestador

- Lee `AGENTS.md`, el plan y este playbook antes de asignar trabajo.
- Mantiene una sola tarea `in_progress` por agente.
- Define archivos exclusivos y dependencias.
- Es el único que modifica simultáneamente archivos globales: `AegiDocs.slnx`, `Directory.*`, `global.json`, `App.xaml` y documentos maestros, salvo delegación explícita.
- Integra resultados, resuelve conflictos y ejecuta el gate global.
- No acepta “debería funcionar”; exige salida de comandos o explicación de por qué no fue posible.

### Explorador

- Trabaja read-only.
- Localiza contratos, patrones, APIs, riesgos y pruebas existentes.
- Devuelve rutas y líneas relevantes, no una reexplicación genérica.
- No propone paquetes o arquitectura como hechos: enumera opciones y evidencia.

### Implementador

- Solo edita archivos asignados.
- Implementa la mínima solución que satisface los criterios.
- Incluye pruebas en el mismo paquete cuando los archivos no estén asignados al agente QA.
- No cambia contratos vecinos para facilitar su código; solicita ajuste al orquestador.

### QA/Test

- Parte de criterios de aceptación y busca falsarlos.
- Puede añadir pruebas/fixtures dentro de su área asignada.
- No “arregla” producción si su rol es reviewer; entrega un repro preciso.
- En UI registra resolución, DPI, estado y capturas comparables.

### Reviewer

- Revisa correctness, cancelación, ownership, seguridad, rendimiento, arquitectura y reglas MeridianUI.
- Prioriza hallazgos reproducibles y los clasifica por severidad.
- No exige cambios cosméticos sin relación con el task packet.
- Aprueba únicamente con evidencia de gates aplicables.

## 5. Contrato de un work packet

Toda asignación debe contener:

```text
Task ID: E##-P##-T##
Objetivo: una oración verificable.
Contexto obligatorio: archivos y documentos que debe leer.
Archivos propietarios: rutas que puede editar.
Fuera de alcance: cambios explícitamente prohibidos.
Contrato de entrada: interfaces, DTOs, fixtures o servicios disponibles.
Contrato de salida: tipos, métodos, archivos o comportamiento esperado.
Criterios de aceptación: lista binaria y medible.
Pruebas requeridas: comandos y casos mínimos.
Modelo: gpt-5.6-terra o gpt-5.6-sol.
Handoff: formato obligatorio del resultado.
```

Si el paquete no tiene suficiente detalle, el explorador Terra puede completarlo read-only antes de asignar un implementador.

## 6. Plantillas de prompts

### Prompt para implementador Terra

```text
Eres el implementador de {TASK_ID} en AegiDocs.Recorder.
Lee AGENTS.md, docs/Architecture.md, docs/Implementation-Plan.md y los archivos indicados.
Objetivo: {OBJETIVO}.
Puedes editar exclusivamente: {ARCHIVOS_O_DIRECTORIOS}.
No modifiques: {FUERA_DE_ALCANCE}.
Respeta los contratos: {CONTRATOS}.
Criterios de aceptación: {CRITERIOS}.
Pruebas requeridas: {COMANDOS_Y_CASOS}.
Preserva cambios ajenos. Si una decisión no está definida, detente y devuelve opciones; no inventes arquitectura.
Al terminar entrega resumen, archivos modificados, pruebas con resultado y riesgos restantes.
```

### Prompt para agente QA Terra

```text
Valida {TASK_ID} sin asumir que la implementación es correcta.
Lee AGENTS.md y los criterios de aceptación.
Busca casos límite, cancelación, errores, recursos sin liberar y regresiones.
Puedes editar únicamente: {TEST_FILES_OR_FIXTURES}.
Ejecuta: {COMMANDS}.
Entrega resultados reproducibles, severidad, archivo/línea cuando aplique y evidencia. No edites producción.
```

### Prompt para reviewer Sol

```text
Revisa el gate {EPIC_OR_TASK} de AegiDocs.Recorder.
Alcance: {DIFF_OR_FILES}. Contratos: {ADRS_AND_INTERFACES}.
Evalúa arquitectura, correctness, threading/cancelación, seguridad/privacidad, recursos nativos, rendimiento y pruebas.
Para UI evalúa MeridianUI y accesibilidad.
No implementes un rediseño. Devuelve hallazgos priorizados, evidencia, cambio mínimo recomendado y veredicto: approve / changes required.
```

### Prompt para UI Terra con MeridianUI

```text
Ejecuta {TASK_ID} en WPF con MVVM.
Antes de editar, lee la skill meridianui-design, C:\Users\kevin\.MeridianUI\README.md,
C:\Users\kevin\.MeridianUI\tokens-wpf.xaml, references\platform-wpf.md y los specimens indicados.
Usa Resources/MeridianTokens.xaml; no inventes colores, radios, sombras, spacing, tipografía o iconos.
Implementa estados loading, empty, error, success, disabled, selected y active cuando apliquen.
Copy es-MX, sin emojis ni marketing voice; animaciones <= 200 ms.
Entrega lista de archivos MeridianUI inspeccionados, tokens/patrones reutilizados y evidencia visual por DPI.
```

## 7. Protocolo de ejecución por tarea

1. El orquestador verifica el gate de entrada y marca la tarea `in_progress`.
2. Si hay incertidumbre, asigna una exploración read-only Terra.
3. Congela o referencia el contrato que consumirá el implementador.
4. Asigna archivos propietarios; ningún otro writer puede tocarlos.
5. El implementador Terra crea código y pruebas del paquete.
6. El agente QA Terra prueba desde los criterios, incluyendo fallos.
7. Para riesgo alto, Sol revisa diff, ADR y evidencia; no repite toda la implementación.
8. El orquestador integra, ejecuta gates globales y comprueba `git diff`.
9. Solo entonces marca `completed` y desbloquea dependientes.
10. Registra riesgos nuevos y decisiones en ADR/Risk Register.

## 8. Paralelismo seguro

### Puede ejecutarse en paralelo

- Domain entities y fixtures de test, si los contratos de nombres ya están congelados.
- Implementación de storage y pruebas adversariales, usando archivos de producción/test distintos.
- Global input y WGC después de aprobar sus interfaces.
- Markdown y PDF después de congelar `PublicationDocument`.
- UI sobre fakes mientras infraestructura implementa los mismos puertos.
- Documentación/manual y hardening técnico cuando no comparten archivos.

### Debe ejecutarse en serie

- ADR antes del scaffolding que depende de él.
- Interfaces antes de dos implementaciones paralelas.
- Migración de esquema después del DTO anterior.
- Renderer común antes de PDF/preview que lo consumen.
- Cambio de tokens MeridianUI antes de views que usan esas keys.
- Edición de `AegiDocs.slnx`, `Directory.*`, `App.xaml` o archivos de versión.
- Integración final y release tagging.

### Mapa de propiedad sugerido

| Área | Writer permitido durante una ola |
| --- | --- |
| `src/AegiDocs.Domain/**` | Agente Domain |
| `src/AegiDocs.Application/**` | Agente Application/orquestador de contratos |
| `src/AegiDocs.Infrastructure.Storage/**` | Agente Storage |
| `src/AegiDocs.Infrastructure.Windows/**` | Agente Windows |
| `src/AegiDocs.Export/**` | Agente Export |
| `src/AegiDocs.Recorder/Views/**`, `ViewModels/**` | Agente UI |
| `src/AegiDocs.Recorder/Resources/**` | Un solo agente MeridianUI |
| `tests/<Area>/**` | Agente QA del área |
| Archivos raíz/solución | Solo orquestador |

## 9. Handoff obligatorio

Cada agente devuelve exactamente estas secciones:

```text
Estado: completed | blocked
Task ID:
Resultado:
Archivos modificados:
Pruebas ejecutadas:
- comando
- resultado exacto
Criterios no verificados:
Riesgos o deuda:
Siguiente dependencia desbloqueada:
```

Un handoff `completed` sin pruebas o explicación explícita se trata como incompleto.

## 10. Revisiones por epic

| Epic | Implementación primaria | QA independiente | Revisión final |
| --- | --- | --- | --- |
| E00 | Terra engineering | Terra CI/docs | Sol solo para dependencias/licencias |
| E01 | Terra scaffolding | Terra architecture tests | Sol arquitectura |
| E02 | Terra Domain | Terra property/fixture tests | Sol modelo final |
| E03 | Terra Storage | Terra adversarial I/O | Sol seguridad/atomicidad |
| E04 | Terra Application | Terra race tests | Sol concurrencia |
| E05 | Terra Windows | Terra integration/manual | Sol P/Invoke/cleanup |
| E06 | Terra Windows | Terra perf/stress | Sol WGC/threading |
| E07 | Terra UI | Terra visual/accessibility | Sol UX gate |
| E08 | Terra UI/Application | Terra performance/undo | Terra reviewer; Sol si cambia contratos |
| E09 | Terra Canvas | Terra pixel/privacy tests | Sol geometría/redacción |
| E10 | Terra Export/UI | Terra golden/visual tests | Sol licencia/PDF gate |
| E11 | Terra hardening | Terra matrices | Sol security/release readiness |
| E12 | Terra packaging/docs | Terra clean-machine QA | Sol release gate + aprobación humana |
| E13 | Terra por feature | Terra evaluation | Sol privacidad/arquitectura IA |

## 11. Estrategia de corrección

- Un hallazgo de test vuelve al mismo implementador si conserva contexto y scope.
- Después de dos intentos fallidos, un explorador/reviewer independiente produce un repro y una hipótesis.
- Si el tercer intento sigue bloqueado por interop, concurrencia, seguridad o arquitectura, escalar a Sol con diff, logs y repro mínimo.
- No reescribir un módulo entero para arreglar un bug localizado.
- Toda corrección añade una prueba de regresión cuando el comportamiento sea automatizable.

## 12. Reglas específicas de calidad para agentes baratos

- No entregar tareas de más de un sub-epic a Terra en un solo prompt.
- Proporcionar nombres exactos de interfaces y criterios, no pedir “implementa la fase”.
- Separar producción y QA entre agentes cuando el riesgo sea medio/alto.
- Pedir salida de comandos; no aceptar estimaciones verbales.
- Preferir fakes deterministas antes de integrar Windows.
- Mantener diffs pequeños: idealmente 3–8 archivos por task packet; dividir si crece.
- Prohibir cambios oportunistas fuera del scope.
- Usar Sol para cerrar ambigüedad antes de que varios Terra implementen interpretaciones distintas.
- Ejecutar gate global después de cada lote paralelo, no solo al final del epic.

## 13. Checklist del orquestador antes de cerrar un epic

- Todas las tareas y dependencias están en estado correcto.
- No hay dos implementaciones del mismo contrato.
- Build/test/format globales pasaron.
- Tests negativos y de cancelación existen donde aplican.
- No hay handles, hooks, streams o archivos temporales sin ownership.
- Los errores llegan a UI como tipos comprensibles, no excepciones crudas.
- Las views no contienen lógica de negocio y usan MeridianUI.
- No se añadieron paquetes sin ADR/inventario de licencia.
- No se introdujo red, telemetría o secretos.
- Documentación, ADRs y riesgos reflejan el resultado real.
- Reviewer emitió `approve` en gates que requieren Sol.

# ADR-0000: [Título breve de la decisión]

- **Estado:** Propuesto | Aceptado | Reemplazado | Rechazado | Retirado
- **Fecha:** YYYY-MM-DD
- **Decisores:** [nombres, roles o equipo responsable]
- **Tarea(s) relacionada(s):** `E##-P##-T##`

## Contexto y problema

Describa el contexto técnico, de producto y operativo. Explique el problema concreto que requiere una decisión y qué ocurriría si se conserva el estado actual.

Incluya los hechos verificables, restricciones y supuestos relevantes. Para AegiDocs, indique expresamente cualquier relación con captura Windows, datos locales, persistencia, exportación, accesibilidad o dependencias externas.

## Drivers de decisión

- [Driver 1: por ejemplo, privacidad local-first.]
- [Driver 2: por ejemplo, confiabilidad y recuperación ante fallos.]
- [Driver 3: por ejemplo, mantenibilidad y pruebas deterministas.]

## Decisión

Declare con precisión la alternativa elegida, los límites de responsabilidad y las reglas que deben respetar las implementaciones futuras.

> Decisión: [texto de la decisión].

## Alcance

- [Componente, flujo o contrato incluido.]
- [Componente, flujo o contrato incluido.]

## No alcance

- [Trabajo explícitamente excluido.]
- [Trabajo explícitamente excluido.]

## Alternativas consideradas y trade-offs

| Alternativa | Ventajas | Desventajas y riesgos | Motivo para aceptar o descartar |
| --- | --- | --- | --- |
| [Alternativa elegida] | [ventajas] | [costos/riesgos] | [razón] |
| [Alternativa 2] | [ventajas] | [costos/riesgos] | [razón] |
| [Alternativa 3] | [ventajas] | [costos/riesgos] | [razón] |

## Consecuencias

### Positivas

- [Beneficio verificable.]
- [Beneficio verificable.]

### Negativas y deuda aceptada

- [Costo, limitación o mantenimiento adicional.]
- [Riesgo residual y responsable de revisarlo.]

## Seguridad y privacidad

Describa los datos que procesa la decisión, dónde residen, quién puede acceder a ellos y cuándo se eliminan. Confirme cómo se respeta el principio local-first.

- ¿Introduce tráfico de red, telemetría, secretos o servicios externos? [Sí/No; detalle y consentimiento explícito si aplica.]
- ¿Puede incluir PII, credenciales, texto sensible o capturas? [Sí/No; controles de minimización, redacción y retención.]
- ¿Requiere validación de entradas no confiables, permisos Windows o aislamiento adicional? [Sí/No; detalle.]

## Impacto en MeridianUI y accesibilidad

Complete esta sección si la decisión afecta UI o UX. Si no aplica, escriba `No aplica` y justifique brevemente.

- Patrones, tokens o componentes MeridianUI afectados: [referencias concretas].
- Estados de interfaz y copy es-MX requeridos: [loading, vacío, error, éxito, disabled, selected, active, según aplique].
- Impacto de accesibilidad: [teclado, lector de pantalla, contraste, DPI y escalado de texto].

## Plan de implementación o migración

1. [Paso implementable, responsable y dependencia.]
2. [Migración de datos, compatibilidad o despliegue, si aplica.]
3. [Actualización de pruebas, documentación y riesgos.]

Indique cómo se tratarán los proyectos, archivos, configuraciones o contratos existentes durante la transición. Especifique ventanas de compatibilidad y versión de esquema cuando corresponda.

## Validación

- Criterios de aceptación: [resultados observables].
- Pruebas automatizadas: [unitarias, integración, regresión, seguridad o rendimiento].
- Validación manual: [entorno Windows, DPI, permisos u otros escenarios].
- Gate de calidad: `dotnet format AegiDocs.slnx --verify-no-changes`, `dotnet build AegiDocs.slnx --configuration Release` y pruebas aplicables.

## Rollback

Describa la condición que activaría un rollback, el procedimiento reversible, los datos que deben preservarse y el responsable de aprobarlo. Si no existe rollback seguro, explique la mitigación y el plan de recuperación.

1. [Disparador y detección.]
2. [Acción de reversión o recuperación.]
3. [Verificación posterior y comunicación.]

## Enlaces relacionados

- [Tarea del plan](../Implementation-Plan.md)
- [Playbook de agentes](../Agent-Execution-Playbook.md)
- [Documento, issue, PR, ADR que reemplaza o evidencia técnica]

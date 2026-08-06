# Visión del producto

## Problema

Documentar un flujo de una aplicación implica repetir el trabajo: ejecutar el flujo, tomar capturas, redactar pasos, ocultar datos sensibles y rehacer distintos formatos cuando cambia la UI.

## Propuesta

AegiDocs Recorder crea un proyecto local con una secuencia de pasos obtenida de clics autorizados y capturas. La persona editora controla el contenido, las anotaciones y las exportaciones. Las sugerencias de IA son opcionales y nunca se ejecutan sin aprobación y configuración explícita.

## MVP

1. Iniciar/detener grabación desde una interfaz clara.
2. Capturar una imagen y metadatos mínimos por clic, excluyendo AegiDocs.
3. Presentar, reordenar, editar y eliminar pasos.
4. Añadir título, descripción, indicador de clic y redacción manual de áreas sensibles.
5. Guardar/abrir un proyecto local versionado (`.aegidocs`).
6. Exportar Markdown y PDF estático.

## Fuera del MVP

OCR, detección semántica de controles, GIF, Word/PowerPoint, múltiples monitores, colaboración, sincronización en nube y generación IA. Se diseñarán como extensiones, no como condiciones para el primer lanzamiento.

## Principios

- Privacidad local por defecto.
- Un proyecto como fuente única de verdad; formatos son derivados.
- La automatización propone; la persona autora decide.
- Degradación segura ante errores de Windows.

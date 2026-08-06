# AegiDocs: instrucciones para asistentes

## Propósito

AegiDocs convierte flujos realizados en aplicaciones Windows en tutoriales editables y exportables. Es **local-first**: una captura no sale del equipo sin una acción explícita de la persona usuaria.

## Reglas de implementación

- El objetivo actual es `AegiDocs.Recorder`, WPF sobre .NET 9 para Windows.
- Mantener separadas captura, modelo de documento, editor, exportación e IA.
- Usar MVVM en toda UI nueva; code-behind solo para inicialización visual o delegación de comandos.
- Preferir `Windows.Graphics.Capture`; encapsular P/Invoke en infraestructura testeable.
- No guardar secretos ni enviar imágenes, OCR o texto a servicios externos por defecto.
- No añadir paquetes NuGet sin justificarlo en documentación o PR.
- La captura pide consentimiento visible, se detiene de forma confiable y excluye AegiDocs.
- Añadir pruebas a la lógica determinista y aislar el SO detrás de interfaces.

## UI y UX: MeridianUI obligatorio

- **Toda UI y UX de AegiDocs debe usar MeridianUI como base obligatoria.** Es la fuente de verdad para colores, tipografía, espaciado, radios, sombras, iconografía, densidad, estados y copy.
- Antes de crear o modificar UI, leer `C:\Users\kevin\.MeridianUI\README.md`, `tokens-wpf.xaml` y `references\platform-wpf.md`; consultar los specimens o referencias que correspondan al patrón a construir.
- En WPF, reutilizar `src/AegiDocs.Recorder/Resources/MeridianTokens.xaml` mediante `StaticResource` o `DynamicResource`. No introducir valores hex, tamaños, espacios, radios, sombras o iconos ad hoc.
- Respetar el tono enterprise-soft: interfaz clara y densa, español es-MX, sin emojis, gradientes, glassmorphism, fondos decorativos ni animaciones de más de 200 ms.
- Una mejora visual es válida únicamente si extiende MeridianUI sin contradecir sus tokens, principios o patrones. Si falta un componente, documentar el patrón elegido y construirlo con esos tokens.
- Al entregar una modificación visual, indicar qué archivos, tokens y patrones de MeridianUI se reutilizaron.

## Convenciones

- Código en inglés; documentación y UI inicial en español.
- Usar `CancellationToken` en operaciones asíncronas y `DateTimeOffset` persistido.
- Serializar proyectos con `System.Text.Json` y esquemas versionados.
- Ejecutar el trabajo conforme a `docs/Implementation-Plan.md` y `docs/Agent-Execution-Playbook.md`; una tarea de agente debe tener ID, alcance, archivos propietarios, criterios de aceptación y handoff con evidencia.
- Usar `gpt-5.6-terra` por defecto para tareas acotadas. Reservar modelos más costosos para decisiones de arquitectura, seguridad, interoperabilidad Windows compleja y gates finales.
- No permitir que dos agentes escritores modifiquen simultáneamente los mismos archivos. Los archivos raíz, solución, configuración compartida y composición pertenecen al orquestador salvo delegación expresa.
- Antes de finalizar: `dotnet build AegiDocs.slnx --configuration Release` y pruebas aplicables.

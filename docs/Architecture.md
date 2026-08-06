# Arquitectura objetivo

`AegiDocs.Recorder` inicia como una aplicación WPF modular. Cuando el MVP tenga comportamiento estable, las áreas se extraerán a bibliotecas sin cambiar los contratos.

| Área | Responsabilidad | Dependencias permitidas |
| --- | --- | --- |
| Presentation | WPF, view models, navegación y comandos | Application |
| Application | Casos de uso y contratos | Domain |
| Domain | Tutorial, paso, anotación y reglas | Ninguna |
| Infrastructure.Windows | Windows Graphics Capture, hooks y almacenamiento | Application, Domain |
| Export | Renderizadores de formatos | Application, Domain |
| AI (opcional) | Adaptadores de proveedores y revisión humana | Application, Domain |

## Modelo inicial

- `DocumentProject`: metadatos, configuración y tutoriales.
- `Tutorial`: título, audiencia y pasos ordenados.
- `Step`: captura, punto de interacción, título, descripción y anotaciones.
- `Annotation`: rectángulo, flecha, texto, numeración o redacción.

Las imágenes viven junto al archivo de proyecto dentro de un directorio administrado; el manifiesto JSON mantiene rutas relativas y versión de esquema.

## Stack decidido

- .NET 9 y C#; WPF en Windows 10/11.
- MeridianUI es la base obligatoria de UI/UX. Los tokens versionados se incluyen en `Resources/MeridianTokens.xaml`; la fuente canónica y sus patrones permanecen en `C:\Users\kevin\.MeridianUI`.
- MVVM con CommunityToolkit.Mvvm al introducir view models.
- Windows.Graphics.Capture; Win32 low-level hook encapsulado.
- `System.Text.Json` para proyecto y configuración.
- QuestPDF para PDF y Razor/Scriban para HTML, tras validar licencias y distribución.
- Open XML SDK, ImageSharp y OCR quedan diferidos hasta que una fase los requiera.

## Privacidad

En el MVP no hay red, telemetría ni IA integrada. Todo proveedor futuro recibirá únicamente contenido seleccionado por el usuario y requerirá una clave fuera del repositorio.

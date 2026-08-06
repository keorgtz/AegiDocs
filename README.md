# AegiDocs

Suite local-first para crear documentación de software a partir de flujos reales. `AegiDocs.Recorder` será la primera aplicación: registra interacciones autorizadas, genera pasos visuales editables y prepara tutoriales para PDF, HTML y Markdown.

## Estado

Se creó el esqueleto WPF de .NET 9 y la especificación del MVP. La captura de pantalla, hooks, editor y exportadores forman parte de las fases posteriores descritas en [el plan](docs/Implementation-Plan.md).

## Inicio rápido

```powershell
dotnet build AegiDocs.slnx --configuration Release
dotnet run --project src/AegiDocs.Recorder
```

## Documentación

- [Visión y alcance](docs/Product-Vision.md)
- [Arquitectura objetivo](docs/Architecture.md)
- [Plan de implementación](docs/Implementation-Plan.md)
- [Playbook de ejecución con agentes](docs/Agent-Execution-Playbook.md)
- [Registro de riesgos](docs/Risk-Register.md)
- [Inventario de dependencias](docs/Dependency-Inventory.md)
- [Política de fixtures](docs/Fixture-Policy.md)
- [Plantilla ADR](docs/adr/0000-template.md)

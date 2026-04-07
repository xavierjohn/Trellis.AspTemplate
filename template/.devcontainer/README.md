# Dev Container

This folder configures a ready-to-code environment for GitHub Codespaces and VS Code Dev Containers.

## What it includes

- .NET 10 SDK
- Docker-in-Docker
- GitHub CLI
- Recommended VS Code extensions for C#, Docker, REST, and EditorConfig
- Auto-restore on container creation

## How to use it

1. Open the template in Codespaces or **Reopen in Container** in VS Code.
2. Wait for the post-create restore to finish.
3. Run the service:

```powershell
dotnet run --project Api\src
```

## Included ports

- `5122` - API (HTTP)
- `7011` - API (HTTPS)
- `9090` - Prometheus
- `9411` - Zipkin

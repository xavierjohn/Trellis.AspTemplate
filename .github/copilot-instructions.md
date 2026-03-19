# Copilot Instructions — Trellis Template Repository

This repository contains the **Trellis ASP.NET template** (`dotnet new`). There are two sets of copilot instructions:

1. **`.github/copilot-instructions.md`** (this file) — Instructions for working on the template repository itself.
2. **`template/.github/copilot-instructions.md`** — Instructions shipped with the template. When a user installs the template, this file guides AI in building their service. Edit this file when updating Trellis conventions, patterns, or architectural guidance.

---

## Repository Structure

```
TrellisAspTemplate/
├── templatepack.csproj            ← NuGet template pack project
├── version.json                   ← Nerdbank.GitVersioning
├── template/                      ← The actual template content (installed by `dotnet new`)
│   ├── .github/
│   │   ├── copilot-instructions.md   ← AI instructions for template users
│   │   └── trellis-api-reference.md  ← Trellis API surface reference
│   ├── Directory.Build.props
│   ├── Directory.Packages.props      ← Trellis + dependency versions
│   ├── Domain/
│   ├── Application/
│   ├── Acl/
│   ├── Api/
│   └── build/
└── .github/
    └── copilot-instructions.md       ← THIS FILE (template repo instructions)
```

## Key Files

- **`template/.github/copilot-instructions.md`** — The most important file for Trellis conventions. This is what AI agents see when building services from the template. Keep it focused on architectural rules and conventions; defer API details to the template API reference.
- **`template/.github/trellis-api-reference.md`** — Trellis API surface shipped with the template for downstream AI use.
- **`template/Directory.Packages.props`** — Central package version management. The `TrellisVersion` property controls all Trellis package versions.

## Working on the Template

- Template content lives entirely under `template/`. Files added there are included when a user runs `dotnet new`.
- Do NOT modify `Directory.Build.props`, `global.json`, or `build/test.props` — these are pre-configured for template users.
- Add new NuGet packages to `template/Directory.Packages.props` (version) and the relevant `.csproj` (reference without version).
- The template uses `BestWeatherForecast` as a placeholder service name.

## Building & Testing the Template Pack

```powershell
# Build the template NuGet package
dotnet pack templatepack.csproj

# Install locally for testing
dotnet new install ./nupkg/Trellis.AspTemplate.*.nupkg

# Create a new project from the template
dotnet new trellis-asp -n MyService

# Uninstall
dotnet new uninstall Trellis.AspTemplate
```

## Updating Trellis Conventions

When updating how AI should build services with Trellis:

1. Edit `template/.github/copilot-instructions.md` for architectural rules and conventions.
2. Edit `template/.github/trellis-api-reference.md` for API surface changes that should ship with the template.
3. Keep instructions DRY — the copilot instructions should reference the template API reference by section number (e.g., "See §12") rather than duplicating API details.

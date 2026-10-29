# Copilot instructions for IncrementalResearch

ONI (Oxygen Not Included) mod. Structure and build setup intentionally mirror the sibling `CritterCensus` solution.

- `IncrementalResearch/IncrementalResearch.csproj`: `netstandard2.1`, LangVersion 10.0, references game DLLs directly from `$(GameManagedPath)`.
- `IncrementalResearch/Local.Build.props` (git-ignored; template: `Local.Build.props.example`) defines `GameManagedPath` and `ModInstallPath`. Env fallbacks: `ONI_GAME_MANAGED_PATH`, `ONI_MOD_INSTALL_PATH`.
- The csproj `PostBuild` target copies the DLL, pdb, `mod.yaml` and `mod_info.yaml` to `$(ModInstallPath)`.
- No `Directory.Build.*` files; build config lives in the csproj.

## Build

```bash
dotnet build
```

Requires a local ONI install (real game assemblies; there is no stub mode). No tests or linters are configured.

## Code layout

- `IncrementalResearchMod.cs`: `UserMod2` entry point; applies all Harmony patches in the assembly.
- File-scoped namespaces, no implicit usings, nullable disabled.

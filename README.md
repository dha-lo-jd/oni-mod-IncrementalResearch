# IncrementalResearch ONI Mod

Oxygen Not Included mod (work in progress).

## Local build setup

Build settings follow the `CritterCensus` layout.

1. Copy `IncrementalResearch/Local.Build.props.example` to `IncrementalResearch/Local.Build.props` (git-ignored).
2. Set `GameManagedPath` (`OxygenNotIncluded_Data\Managed`) and `ModInstallPath` (e.g. `...\mods\local\IncrementalResearch`).
   Alternatively set the environment variables `ONI_GAME_MANAGED_PATH` / `ONI_MOD_INSTALL_PATH`.
3. `dotnet build` — the DLL, pdb, `mod.yaml` and `mod_info.yaml` are copied to `ModInstallPath` after each build.

# AGENTS.md

- **Build C# scripts**: run `dotnet build` in the `godot/` directory. The pre‑launch task in VS Code uses this.
- **Run the game**: open `godot/project.godot` in the Godot editor and click **Play**. Alternatively, from the terminal run `godot --verbose` (path to the Godot executable is hard‑coded in `launch.json`).
- **VS Code debugging**: the launch configuration (`.vscode/launch.json`) assumes the Godot executable is at
  `C:\Program Files\Godot\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64.exe`. Update the `program` field if your installation differs.
- **Namespace**: all C# scripts are compiled under `murph9.RallyGame2.godot`.
- **JSON assets**: `Cars\Init\Data\*.json` are marked as `Content` in the `.csproj` and are copied to the output directory during build.
- **.uid files**: the `.cs` files with a `.uid` suffix are Godot’s compiled script identifiers; they should not be edited directly.
- **.editorconfig**: the repository enforces formatting rules; run `dotnet format` or use your editor’s formatting support.
- **No automated tests**: the project does not contain unit tests or CI workflows; all verification is manual through the Godot editor.
- **Environment**: requires .NET 8.0 SDK and Godot 4.6.1 Mono. Verify the SDK is installed with `dotnet --list-sdks`.
- **Project layout**: everything lives under the `godot/` folder – resources, scripts, scenes, shaders, and the `.csproj`.
- **Common pitfalls**:
  - Forgetting to rebuild after editing C# scripts; the build task must run before launching.
  - Hard‑coded Godot path in `launch.json`; adjust if the engine is in a different location.
  - Mixing C# and GDScript: the project currently uses only C#.

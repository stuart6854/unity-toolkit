# Toolkit (`com.stumillman.toolkit`)

Personal toolkit for **Unity 6+**. It starts with a **Project Setup** window for new projects.

## Install

In Unity, open **Window > Package Manager**, click **+ > Install package from git URL...**, then enter:

```
https://github.com/stuart6854/unity-toolkit.git#v0.11.0
```

You can also add it to `Packages/manifest.json`:

```json
"com.stumillman.toolkit": "https://github.com/stuart6854/unity-toolkit.git#v0.11.0"
```

To get the latest commit on `main`, leave off `#v0.11.0`. Pinning a tag is recommended.

## Project Setup

Open it from **Tools > Toolkit > Project Setup**. Tick the sections you want, then click **Run Selected**. You can also run each section with its own button.

| Section | What it does |
|---|---|
| Folders | Creates `Assets/_Project/{Art/{Materials,Models,Textures,Animations},Audio,Prefabs,Scenes,Scripts/{Runtime,Editor},ScriptableObjects,Settings,UI}`. Can add `.gitkeep` files. |
| Git Files | Writes a Unity `.gitignore` and `.gitattributes` to the project root (LFS rules included but commented out). Existing files are kept unless "Overwrite" is ticked. |
| Default Materials | Creates simple coloured URP Lit materials in `Assets/_Project/Art/Materials/Defaults`. Existing materials are never overwritten. |
| Utility Textures | Creates PNGs in `Assets/_Project/Art/Textures/Common`: 2x2 solids (white, black, grey, flat normal), noise (white, RGB, tileable Perlin), prototype (1m grid, checker, UV test) and gradients (horizontal, vertical, radial), each with suitable import settings. |
| Scenes | Creates `Bootstrap`, `MainMenu` and `Game` scenes in `Assets/_Project/Scenes` and adds them to Build Settings. Each can include a camera and light, organiser objects, a URP Global Volume and a ground plane with cubes. Opens `Bootstrap` and deletes the template's `SampleScene`. Existing scenes are never overwritten. |
| Project Settings | Sets company/product name and bundle ID, Linear color space, fast Enter Play Mode and Input System-only input handling (asks to restart the editor). |
| Assembly Definitions | Creates `<Product>.Runtime` and `<Product>.Editor` asmdefs in `_Project/Scripts/Runtime` and `Editor`, referencing common packages. |
| Packages | Installs `com.unity.inputsystem`, `com.unity.cinemachine` and `com.unity.ai.navigation`. Removes unneeded template packages (Version Control, Visual Scripting, Multiplayer Center) and unused built-in modules (2D, XR/VR, analytics, video, asset bundles, terrain and more; see the list in the window). Packages already in the right state are skipped. Optional packages (Alchemy, ALINE) are installed only when ticked. |

Every list can be edited in the window. Settings are saved per project to `ProjectSettings/StuMillmanToolkit/ProjectSetupSettings.asset`.

## Editor tools

### Bootstrap Play Mode

**Tools > Toolkit > Bootstrap Play Mode** (on by default). Pressing Play starts in the first scene in Build Settings (Bootstrap), then loads the scene you were editing. Mark objects in Bootstrap with `DontDestroyOnLoad` to keep them.

If your Bootstrap code normally loads the main menu, skip that when testing another scene:

```csharp
if (!StuMillman.Toolkit.BootstrapPlayMode.WillLoadEditedScene)
    SceneManager.LoadScene("MainMenu");
```

### Prototype materials

**Tools > Toolkit > Prototype Materials** creates materials using the package's world-space triplanar shader. The grid stays 1 tile = `Tile Size` metres however objects are scaled or rotated. Pick a preset colour (Floor, Wall, Interactive, Water, Vegetation, Danger, Special), a pattern (Grid, Checker, Plain) and click **Create Material**. **Create Default Set** makes one grid material per preset. Lighting is simple: main light with shadows, ambient and fog (no additional lights).

### Hierarchy headers

Objects named like `--- Managers ---` show as coloured bars in the Hierarchy. Each name always gets the same colour. Toggle with **Tools > Toolkit > Hierarchy Headers**. In builds, their children move up a level and the headers are removed, so they cost nothing at runtime.

### Scene switcher

On Unity 6.3+, a dropdown in the main toolbar lists the Build Settings scenes. If it's hidden, right-click the toolbar to show **Toolkit/Scene Switcher**. On earlier versions, use **Tools > Toolkit > Switch Scene...**.

### Auto-save and Play Mode tint

Configure both in **Edit > Preferences > Toolkit** (per user):

- **Auto-save** saves dirty scenes and assets every N minutes, before entering Play Mode and when Unity loses focus. Untitled scenes are skipped.
- **Play Mode tint** sets Unity's built-in Playmode tint colour, so it's obvious when you're playing.

### Screenshots

**Tools > Toolkit > Screenshot**. Press **F12** to capture the Game view, or use the x2/x4 menu items. Files are saved to `<Project>/Screenshots` (git-ignored).

## Build info

Every build is stamped with the version, a build number, the git commit and branch, and the build time:

```csharp
using StuMillman.Toolkit;

var info = BuildInfo.Current;
versionLabel.text = info.ShortString;   // "v0.3.0 (42)"
Debug.Log(info.ToString());             // "v0.3.0 (42) a1b2c3d [main] 2026-10-29 14:03 UTC"
// Also: info.Version, info.BuildNumber, info.Commit, info.Branch, info.IsDirty, info.BuildTimeUtc
```

- The build number goes up by 1 on each build. It's saved in `ProjectSettings/StuMillmanToolkit/BuildNumber.txt` (commit this file), and is also used for the Android version code and iOS/macOS build number.
- In the editor, `Current` returns live values (the last build number, current git state).
- Needs `git` on the PATH for commit/branch. Without it they show as `unknown`.

## Releasing

1. Bump `version` in `package.json`.
2. Add a `## [x.y.z]` section to `CHANGELOG.md`.
3. Push to `main`. The **Release** workflow creates the `vx.y.z` tag and a GitHub release.

## License

MIT. See [LICENSE.md](LICENSE.md).

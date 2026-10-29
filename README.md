# Toolkit (`com.stumillman.toolkit`)

Personal toolkit for **Unity 6+**. It starts with a **Project Setup** window for new projects.

## Install

In Unity, open **Window > Package Manager**, click **+ > Install package from git URL...**, then enter:

```
https://github.com/stuart6854/unity-toolkit.git#v0.6.2
```

You can also add it to `Packages/manifest.json`:

```json
"com.stumillman.toolkit": "https://github.com/stuart6854/unity-toolkit.git#v0.6.2"
```

To get the latest commit on `main`, leave off `#v0.6.2`. Pinning a tag is recommended.

## Project Setup

Open it from **Tools > Toolkit > Project Setup**. Tick the sections you want, then click **Run Selected**. You can also run each section with its own button.

| Section | What it does |
|---|---|
| Folders | Creates `Assets/_Project/{Art/{Materials,Models,Textures,Animations},Audio,Prefabs,Scenes,Scripts/{Runtime,Editor},ScriptableObjects,Settings,UI}`. Can add `.gitkeep` files. |
| Git Files | Writes a Unity `.gitignore` and `.gitattributes` to the project root (LFS rules included but commented out). Existing files are kept unless "Overwrite" is ticked. |
| Default Materials | Creates simple coloured URP Lit materials in `Assets/_Project/Art/Materials/Defaults`. Existing materials are never overwritten. |
| Scenes | Creates `Bootstrap`, `MainMenu` and `Game` scenes in `Assets/_Project/Scenes` and adds them to Build Settings. Each can include a camera and light, organiser objects, a URP Global Volume and a ground plane with cubes. Opens `Bootstrap` and deletes the template's `SampleScene`. Existing scenes are never overwritten. |
| Project Settings | Sets company/product name and bundle ID, Linear color space, fast Enter Play Mode and Input System-only input handling (asks to restart the editor). |
| Assembly Definitions | Creates `<Product>.Runtime` and `<Product>.Editor` asmdefs in `_Project/Scripts/Runtime` and `Editor`, referencing common packages. |
| Packages | Installs `com.unity.inputsystem`, `com.unity.cinemachine` and `com.unity.ai.navigation`. Removes unneeded template packages (Version Control, Visual Scripting, Multiplayer Center) and unused built-in modules (2D, XR/VR, analytics, video, asset bundles, terrain and more; see the list in the window). Packages already in the right state are skipped. |

Every list can be edited in the window. Settings are saved per project to `ProjectSettings/StuMillmanToolkit/ProjectSetupSettings.asset`.

## Editor tools

### Bootstrap Play Mode

**Tools > Toolkit > Bootstrap Play Mode** (on by default). Pressing Play starts in the first scene in Build Settings (Bootstrap), then loads the scene you were editing. Mark objects in Bootstrap with `DontDestroyOnLoad` to keep them.

If your Bootstrap code normally loads the main menu, skip that when testing another scene:

```csharp
if (!StuMillman.Toolkit.BootstrapPlayMode.WillLoadEditedScene)
    SceneManager.LoadScene("MainMenu");
```

### Hierarchy headers

Objects named like `--- Managers ---` show as coloured bars in the Hierarchy. Each name always gets the same colour. Toggle with **Tools > Toolkit > Hierarchy Headers**. In builds, their children move up a level and the headers are removed, so they cost nothing at runtime.

### Scene switcher

On Unity 6.3+, a dropdown in the main toolbar lists the Build Settings scenes. If it's hidden, right-click the toolbar to show **Toolkit/Scene Switcher**. On earlier versions, use **Tools > Toolkit > Switch Scene...**.

### Screenshots

**Tools > Toolkit > Screenshot**. Press **F12** to capture the Game view, or use the x2/x4 menu items. Files are saved to `<Project>/Screenshots` (git-ignored).

## Releasing

1. Bump `version` in `package.json`.
2. Add a `## [x.y.z]` section to `CHANGELOG.md`.
3. Push to `main`. The **Release** workflow creates the `vx.y.z` tag and a GitHub release.

## License

MIT. See [LICENSE.md](LICENSE.md).

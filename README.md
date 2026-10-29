# Toolkit (`com.stumillman.toolkit`)

Personal toolkit for **Unity 6+**. It starts with a **Project Setup** window for new projects.

## Install

In Unity, open **Window > Package Manager**, click **+ > Install package from git URL...**, then enter:

```
https://github.com/stuart6854/unity-toolkit.git#v0.4.0
```

You can also add it to `Packages/manifest.json`:

```json
"com.stumillman.toolkit": "https://github.com/stuart6854/unity-toolkit.git#v0.4.0"
```

To get the latest commit on `main`, leave off `#v0.4.0`. Pinning a tag is recommended.

## Project Setup

Open it from **Tools > Toolkit > Project Setup**. Tick the sections you want, then click **Run Selected**. You can also run each section with its own button.

| Section | What it does |
|---|---|
| Folders | Creates `Assets/_Project/{Art/{Materials,Models,Textures,Animations},Audio,Prefabs,Scenes,Scripts/{Runtime,Editor},ScriptableObjects,Settings,UI}`. Can add `.gitkeep` files. |
| Default Materials | Creates simple coloured URP Lit materials in `Assets/_Project/Art/Materials/Defaults`. Existing materials are never overwritten. |
| Scenes | Creates `Bootstrap`, `MainMenu` and `Game` scenes in `Assets/_Project/Scenes` and adds them to Build Settings. Each can include a camera and light, organiser objects, a URP Global Volume and a ground plane with cubes. Opens `Bootstrap` and deletes the template's `SampleScene`. Existing scenes are never overwritten. |
| Packages | Installs `com.unity.inputsystem`, `com.unity.cinemachine` and `com.unity.ai.navigation`. Removes unneeded template packages (Version Control, Visual Scripting, Multiplayer Center) and unused built-in modules (2D, XR/VR, analytics, video, asset bundles, terrain and more; see the list in the window). Packages already in the right state are skipped. |

Every list can be edited in the window. Settings are saved per project to `ProjectSettings/StuMillmanToolkit/ProjectSetupSettings.asset`.

## Releasing

1. Bump `version` in `package.json`.
2. Add a `## [x.y.z]` section to `CHANGELOG.md`.
3. Push to `main`. The **Release** workflow creates the `vx.y.z` tag and a GitHub release.

## License

MIT. See [LICENSE.md](LICENSE.md).

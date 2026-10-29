# Changelog

All notable changes to this package are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [0.6.4] - 2026-10-29

### Changed
- New Hierarchy window: header bars are drawn behind the row so the expand/collapse arrow stays visible. The name, icon and other columns are hidden on header rows.

## [0.6.3] - 2026-10-29

### Changed
- Hierarchy header bars now span the full row (all columns in the new Hierarchy window).
- Header colours are more muted.

## [0.6.2] - 2026-10-29

### Fixed
- Hierarchy headers didn't show in Unity 6.6's new Hierarchy window. They now hook `HierarchyWindow.BindViewItem` there. The legacy window still uses the IMGUI callback.

## [0.6.1] - 2026-10-29

### Fixed
- Compile errors on newer Unity 6 versions: hierarchy headers now use `hierarchyWindowItemByEntityIdOnGUI` and `EntityIdToObject` on Unity 6.3+.

## [0.6.0] - 2026-10-29

### Added
- **Hierarchy headers**: objects named like `--- Managers ---` are drawn as coloured bars (colour based on the name). Toggle with Tools > Toolkit > Hierarchy Headers. In builds, header children move up a level and the header objects are removed.
- **Scene switcher**: a main toolbar dropdown (Unity 6.3+) listing Build Settings scenes. On older versions use Tools > Toolkit > Switch Scene...

## [0.5.0] - 2026-10-29

### Added
- **Project Settings** section: company/product name and bundle ID, Linear color space, fast Enter Play Mode (no domain/scene reload), Input System as the only input handler (prompts for an editor restart).
- **Git Files** section: writes a Unity `.gitignore` and `.gitattributes` (Smart Merge rules, binary rules, Git LFS rules commented out) to the project root.
- **Assembly Definitions** section: creates `<Product>.Runtime` and `<Product>.Editor` asmdefs in `_Project/Scripts` with common package references.
- **Bootstrap Play Mode** (Tools > Toolkit > Bootstrap Play Mode): Play always starts in the first Build Settings scene, then loads the scene(s) you were editing. Runtime code can check `StuMillman.Toolkit.BootstrapPlayMode.WillLoadEditedScene`.
- **Screenshot tool** (Tools > Toolkit > Screenshot): F12 captures the Game view to `<Project>/Screenshots`, with x2/x4 options.

### Changed
- `com.unity.modules.screencapture` is no longer removed by default (the screenshot tool needs it). It's taken off saved remove lists automatically.

## [0.4.0] - 2026-10-29

### Added
- Default remove list now covers Unity 6.6 template modules: 2D (physics2d, physicscore2d, tilemap, vectorgraphics), platform/services (adaptiveperformance, androidjni, unityanalytics, xr), media/content (video, screencapture, assetbundle, unitywebrequest*) and terrain (terrain, terrainphysics, tetgen).
- Settings saved by older versions get the new defaults added automatically. Your own entries are kept.
- Console now lists packages skipped because they aren't in the manifest.

## [0.3.1] - 2026-10-29

### Fixed
- Package removal did nothing ("Packages already up to date"). Installed packages are now read with `Client.List` instead of `PackageInfo.FindForPackageName`.

## [0.3.0] - 2026-10-29

### Added
- Scene setup: creates Bootstrap, MainMenu and Game scenes in `Assets/_Project/Scenes` and adds them to Build Settings (Bootstrap first).
- Each scene can include a camera and light, organiser objects (`--- Managers ---`, `--- Environment ---`, `--- UI ---`, `--- Dynamic ---`), a URP Global Volume (shared profile with ACES tonemapping and light bloom) and test geometry (ground plane and cubes using the default materials). Each item can be toggled per scene.
- Opens the first scene and optionally deletes the template's `SampleScene`.

## [0.2.0] - 2026-10-29

### Added
- Package removal: removes unneeded default packages (Version Control, Visual Scripting, Multiplayer Center) and rarely used built-in modules (cloth, vehicles, VR, wind, umbra). The list is editable.

### Changed
- Packages are now installed and removed in a single request via **Apply Package Changes**.

## [0.1.0] - 2026-10-29

### Added
- Project Setup window (**Tools > Toolkit > Project Setup**).
- Folder setup with an editable folder list and optional `.gitkeep` files.
- Default URP material creation.
- Package installer (Input System, Cinemachine, AI Navigation by default).

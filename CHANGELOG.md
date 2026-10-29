# Changelog

All notable changes to this package are documented here.
This project follows [Semantic Versioning](https://semver.org/).

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

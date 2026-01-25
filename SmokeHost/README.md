# SmokeHost

A minimal WPF app that hosts `GisEditorWpfMap` (ThinkGeo v14) with a single `LayerOverlay` and an in-memory demo layer.

## What it helps you validate quickly

- MapView renders at all (Skia canvas pipeline + shims)
- `RefreshAsync()` timing vs `OverlaysDrawn` event
- `CurrentExtent` + `CurrentScale` event ordering
- Overlay-only refresh (`RefreshAsync(overlay)`) vs full refresh

## How to use

1. Open `WpfDesktopExtension/WpfDesktopExtension.sln`
2. Add this project (`SmokeHost/SmokeHost.csproj`) to the solution
3. Set `SmokeHost` as startup project and run

Buttons:
- **Init / Reset**: rebuilds overlay/layer and refreshes
- **Move Point**: changes one point feature and refreshes overlay only

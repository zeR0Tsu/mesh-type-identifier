# Mesh Type Identifier

A Unity Editor plugin for automatically identifying **Mesh** vs **SkinnedMesh** in scenes, with real-time visual labels in the Hierarchy panel and intelligent bone selection.

## Features

- **Hierarchy Labels** — Shows `[M]` (green) for MeshRenderer and `[S]` (blue) for SkinnedMeshRenderer directly in the Hierarchy
- **One-Click Bone Selection** — Click the `[S]` label to automatically select the controlling bone (uses BoneWeight analysis for accuracy)
- **Bone Weight Analysis** — Finds the best bone to control each SkinnedMesh based on actual vertex weight data
- **Smart Bone Targeting** — Automatically distinguishes full-body meshes (selects Hips) from specific body parts (selects the specific bone chain)
- **Fully Configurable** — Toggle labels on/off per mesh type, adjust label position from the settings window

## Installation

### Via VCC (VRChat Creator Companion)

1. Add the VPM repository: `https://zer0tsu.github.io/mesh-type-identifier/index.json`
2. Install `Mesh Type Identifier` from the package list

### Via UPM (Unity Package Manager)

Add the Git URL in Package Manager:
```
https://github.com/zeR0Tsu/mesh-type-identifier.git
```

### Manual

Download the latest release ZIP and extract into your Unity project's `Packages/` folder.

## Usage

1. After installation, **Hierarchy labels appear automatically** — no setup needed
2. Open settings: `zeR0Tsu > 网格识别设置`
3. Hover over `[S]` labels to see the controlling bone name
4. Click `[S]` to jump to that bone in the Hierarchy

## How It Works

The plugin reads `Mesh.boneWeights` from each SkinnedMeshRenderer to determine which bone has the most influence on the mesh, then uses a weighted scoring formula (`weight / depth^2`) to select the most appropriate control bone. Full-body meshes (using >10% of skeleton bones) retain the root bone as the selection target.

## Performance

The hierarchy labels are designed to stay cheap even when the scene is expanded to thousands of visible objects:

- **Per-object caching** — each GameObject's detection result is cached, so a repaint does an O(1) dictionary lookup + one button draw instead of re-checking components every frame.
- **Lazy bone analysis** — the expensive `Mesh.boneWeights` scan runs only when you hover a `[S]` label (and the result is cached), not on every repaint.
- **Targeted invalidation** — the cache is cleared per object on component changes and pruned of destroyed objects on hierarchy changes, so newly added objects are scanned individually.

## Package Structure

```
com.zer0tsu.mesh-type-identifier/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE
├── Runtime/
│   ├── MeshType.cs
│   ├── IMeshInfo.cs
│   ├── MeshInfo.cs
│   └── MeshScanner.cs
└── Editor/
    ├── MeshTypeIdentifierConfig.cs
    ├── MeshTypeIdentifierWindow.cs
    └── HierarchyMeshLabel.cs
```

## License

MIT

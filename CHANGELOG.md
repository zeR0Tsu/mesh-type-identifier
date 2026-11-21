# Changelog

## [1.0.1] - 2026-11-21

### Fixed
- **Severe hierarchy lag when many objects are expanded.** `hierarchyWindowItemOnGUI` runs for every visible object on every repaint, and the plugin was rebuilding a bone-weight tooltip by re-scanning the whole mesh every frame. Hierarchy labels are now cached per GameObject and the BoneWeight analysis runs lazily only when hovering the `[S]` label, turning the per-frame cost from a full mesh scan into an O(1) cache lookup.
- **`[S]` button not receiving clicks** (regression from the performance work). The hierarchy draw callback now handles all onGUI events so the button can receive the click and focus the target bone again.
- Cache is invalidated per object on component changes and pruned of destroyed objects on hierarchy changes; newly added objects are scanned individually.

## [1.0.0] - 2026-07-25

### Added
- Initial release
- Hierarchy labels `[M]` / `[S]` for quick mesh type identification
- One-click bone selection via `[S]` label
- BoneWeight-based intelligent bone targeting
- Smart detection: full-body meshes (Hips) vs specific body parts (tail, head, etc.)
- Configurable settings window (label toggles, margin adjustment)
- Debug tool for bone weight distribution analysis
- Unified `MeshInfo` data model for future extensibility

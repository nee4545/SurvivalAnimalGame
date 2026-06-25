SpecialZone_GoldenRing.prefab - fixed material version

This is a visual-only prefab: no scripts, no colliders.

Use:
1. Drag Prefabs/SpecialZone_GoldenRing.prefab as a child under your existing trigger zone.
2. Keep it slightly above the terrain, around Y = 0.03 to 0.06.
3. Scale the root to match your trigger radius.

Fix included:
The material now uses a local custom unlit transparent shader:
Assets/SpecialZoneRing/Shaders/SpecialZone_UnlitTransparent.shader

This avoids pink material issues caused by render-pipeline shader mismatch.

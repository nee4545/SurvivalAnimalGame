Dust Trail Effect for Unity

How to install:
1. Copy Assets/DustTrailEffect into your Unity project's Assets folder.
2. Let Unity compile.
3. Go to Tools > Dust Trail > Create Dust Trail Prefab.
4. Unity will create Assets/DustTrailEffect/Prefabs/FX_DustTrail_Player.prefab.
5. Drag FX_DustTrail_Player into your scene.
6. Assign your player Transform to DustTrailController > Target.
7. Adjust Local Offset so the dust spawns near the feet/back of the player.

Recommended settings:
- For animals: localOffset = (0, 0.08, -0.35)
- For humans: localOffset = (0, 0.05, -0.25)
- For low-end mobile: maxEmission = 20 to 30, maxParticles = 60 to 90
- For mid-range mobile: maxEmission = 35 to 55, maxParticles = 100 to 140

Notes:
- This effect uses no collision and no lights.
- It only emits when the target is moving.
- It uses World simulation space so dust stays behind instead of sticking to the player.

River Escape Foam Strip Package

Setup:
1. Copy the RiverEscapeFoamStrips folder into your Unity Assets folder.
2. In Unity, run: Tools > River Escape > Create Foam Strip Prefab
3. Drag the generated prefab into your River Escape scene.
4. Place one strip near the left river bank and duplicate it for the right bank.
5. Keep it slightly above the water surface to avoid z-fighting.

Generated prefab path:
Assets/AnimalGame/Prefabs/AiGenerated prefabs/RiverEscapeFoam/Prefabs/RiverFoamStrip.prefab

Recommended placement:
- Put the strip on top of the water near each bank.
- Rotate/scale it so it follows the river direction.
- Use several strips on each river tile or parent them under your river tile parent so they recycle with the tile.

Recommended values:
- Tint alpha: 0.45 to 0.75
- Scroll Speed X: 0.2 to 0.6
- Scale length: match your river tile length
- Scale width: 0.8 to 2.0

If the foam scrolls sideways, change Scroll Speed from X to Y or rotate the quad/prefab.

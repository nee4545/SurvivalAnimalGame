River Escape Lasso Catch Ring Package

What this gives you:
- RiverLassoCatchRing.cs: quad-based lasso/catch ring visual.
- RiverLassoRing_Transparent.png: transparent golden ring texture.
- Unity editor menu to create a ready prefab.

Setup:
1. Unzip this package.
2. Copy the RiverEscapeLasso folder directly into your Unity project's Assets folder.
3. In Unity, run:
   Tools > River Escape > Create Lasso Catch Ring Prefab
4. Unity creates:
   Assets/AnimalGame/Prefabs/AiGenerated prefabs/RiverEscapeLasso/Prefabs/RiverLassoCatchRing.prefab
5. Drag that prefab into your River Escape scene.
6. Assign the prefab object to:
   RiverEscapePlayerController > Lasso Catch Ring
7. Clear/ignore Aim Arc Line.

Recommended values:
- RiverEscapePlayerController > Mount Catch Radius: 2.5 to 3.5
- RiverLassoCatchRing > Height Offset From Water: 0.08
- RiverLassoCatchRing > Active Brightness: 1.8 to 2.2
- RiverLassoCatchRing > Pulse Scale Amount: 0.06 to 0.1

If invisible:
- Check the River Camera culling mask can see the prefab layer.
- Raise Height Offset From Water to 0.15 or 0.25.
- In RiverLassoCatchRing, try Flat World Rotation X = 90 instead of -90.
- Check the generated material uses a transparent shader.

Stampede Dust Trail Package
===========================

Purpose
-------
Creates a mobile-friendly dust trail prefab for stampede AI. Attach it to your running AI hazards so dust appears near the ground while they charge toward the player.

Install
-------
1. Unzip this package.
2. Copy the StampedeDustTrailPackage folder into your Unity Assets folder.
3. In Unity, use: Tools > Stampede > Create Dust Trail Prefab
4. Unity creates: Assets/StampedeDustTrail/Prefabs/StampedeDustTrail.prefab

How to attach to AI
-------------------
1. Drag StampedeDustTrail.prefab as a child of your stampede AI prefab.
2. Place it near the feet, slightly behind the body.
3. Usually keep Local Offset around: X 0, Y 0.08, Z -0.65
4. Make sure the AI has ground below it included in the Ground Mask.

Recommended values
------------------
Min Speed To Emit: 0.75
Full Emission Speed: 8
Emission Multiplier: 1
Ground Y Offset: 0.04
Snap To Ground: ON
Align To Movement Direction: ON

Tuning for bigger animals
-------------------------
Elephant/Rhino/Buffalo:
- Emission Multiplier: 1.4 to 2.0
- Local Offset Z: -0.9 to -1.2
- Full Emission Speed: 10 to 14

Tuning for smaller animals
--------------------------
Zebra/Deer:
- Emission Multiplier: 0.75 to 1.2
- Local Offset Z: -0.45 to -0.7

Important
---------
The particles use World simulation space, so dust stays behind on the ground instead of sticking to the AI body.
No Rigidbody is needed.

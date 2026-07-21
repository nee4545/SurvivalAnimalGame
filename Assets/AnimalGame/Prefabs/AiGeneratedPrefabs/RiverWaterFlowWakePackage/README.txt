River Water Flow + Wake Package

Files:
1. MobileStylizedWaterBasic_FoamFlow.shader
   - Replaces the previous MobileStylizedWaterBasic_Foam shader.
   - Keeps depth foam and adds directional river-flow streaks.

2. RiverWakeTrailTransparent.shader
   - Simple transparent shader for TrailRenderer wake materials.

3. RiverRideableWakeTrailController.cs
   - Attach to rideable root or wake parent.
   - Controls TrailRenderer wake emission based on movement speed / mounted / retiring state.

Water material starting values:
- Wave Strength: 0.005 to 0.015
- Flow Streak Strength: 0.2 to 0.35
- Flow Streak Speed: 0.8 to 1.5
- Flow Streak Scale: 8 to 14
- Flow Streak Stretch: 6 to 12
- Flow Streak Sharpness: 0.65 to 0.8
- Flow Use V As Forward: 1. If flow goes sideways, try 0. If flow goes backward, make Flow Streak Speed negative.

Wake setup:
RideableRoot
  Wake_Left  (TrailRenderer)
  Wake_Right (TrailRenderer)
  RiverRideableWakeTrailController

TrailRenderer recommended values:
- Time: controlled by script
- Start Width: 0.25 to 0.45
- End Width: 0.03 to 0.08
- Min Vertex Distance: 0.1 to 0.2
- Alignment: View
- Texture Mode: Stretch
- Material: material using Custom/RiverWakeTrailTransparent

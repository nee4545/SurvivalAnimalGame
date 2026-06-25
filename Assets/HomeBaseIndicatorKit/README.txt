HOME BASE INDICATOR KIT

INSTALL
1. Copy the HomeBaseIndicatorKit folder into your Unity project's Assets folder.
2. Allow Unity to compile.
3. Open: Tools > Game UI > Create Home Base Indicator Prefab.
4. Unity creates:
   Assets/HomeBaseIndicatorKit/Generated/HomeBaseIndicator.prefab
5. Drag that prefab under your Screen Space - Overlay Canvas.

REFERENCES
- Assign Home Base to your base-center Transform.
- Player can be left empty if your player uses the Player tag.
- Target Camera can be left empty to use Camera.main.

SPRITE
- Replace the ArrowImage sprite in the generated prefab whenever needed.
- The included sample sprite is automatically assigned by the builder.

NOTES
- The prefab root stretches across the Canvas.
- The visible arrow sits at the top-center.
- The arrow hides within 8 world units of home by default.
- If the artwork points the wrong direction, adjust Arrow Rotation Offset.

SpecialZone Golden Ring With Particles

Drag this prefab under any existing trigger zone:
Assets/SpecialZoneParticleVFX/Prefabs/SpecialZone_GoldenRing_WithParticles.prefab

What it includes:
- Golden ground ring visual
- Soft vertical golden light rays
- Floating gold spark particles
- Rim motes
- Ring rotation
- Glow pulse from 1 to 3 using _GlowIntensity

No trigger logic and no colliders are included.

Recommended setup:
- Place slightly above terrain: Y = 0.03 to 0.06
- Scale root to match your trigger radius
- In SpecialZoneParticleVFX, adjust Zone Radius to match the visual radius after scaling
- Lower Density to 0.5 for mobile-heavy scenes


FIXED VERSION:
- Corrected ParticleSystem velocity curve modes so X/Y/Z use matching MinMaxCurve modes.

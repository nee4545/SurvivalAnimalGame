River Escape Water Splash Package

SETUP
1. Copy the RiverEscapeWaterSplashes folder into your Unity Assets folder.
2. Wait for scripts to compile.
3. Run: Tools > River Escape > Create Water Splash Prefabs
4. Prefabs will be created at:
   Assets/AnimalGame/Prefabs/AiGenerated prefabs/RiverEscapeWaterSplash/Prefabs/

PREFABS
- RiverContinuousSplash.prefab
  Use this for small constant splashes behind/around rideable animals/logs.

- RiverLandingSplashBurst.prefab
  Use this for a one-shot splash when the player lands on a rideable.

HOW TO USE CONTINUOUS SPLASH
1. Drag RiverContinuousSplash under a rideable animal/log prefab.
2. On RiverSplashLockToWater:
   - Follow Target = rideable root
   - Water Y = your water surface Y
   - Height Offset = 0.04 to 0.08
   - Local Offset = usually (0, 0, -0.45)
3. Duplicate it for left/right side splashes if needed.

HOW TO USE LANDING BURST
1. Drag RiverLandingSplashBurst under a rideable animal/log prefab.
2. Set Follow Target = rideable root.
3. Set Water Y to your water surface Y.
4. Call PlayBurst() on its RiverSplashBurst component when landing/mounting.

OPTIONAL RIDEABLE CONTROLLER
Attach RiverRideableSplashController to your rideable root.
Assign continuous and landing particle references.
Then call PlayLandingBurst() when the player lands on that rideable.

MOBILE TIPS
- Keep Max Particles low: 30-60 per effect.
- Use 1 continuous splash per rideable first.
- Use landing burst only for player landing, not every animal all the time.

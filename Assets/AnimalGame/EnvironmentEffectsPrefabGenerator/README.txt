Unity Environment Effects Prefab Generator

What this gives you:
- FX_Rain_Light.prefab
- FX_Snow_Light.prefab
- FX_Wind_Dust.prefab
- EnvironmentEffects_Rig.prefab with controller + all 3 effects

How to use:
1. Copy the Assets/EnvironmentEffects folder into your Unity project.
2. Let Unity compile.
3. In Unity top menu, click Tools > Environment Effects > Create Weather Effect Prefabs.
4. Prefabs will be created in Assets/EnvironmentEffects/Prefabs.
5. Drag EnvironmentEffects_Rig into your scene.
6. Assign your Player to WeatherFollowTarget.target.
7. Call SetRain(), SetSnow(), SetWind(), SetRainAndWind(), SetSnowAndWind(), or SetClear().

Notes:
- These are mobile-friendly starter effects.
- The generated materials are simple and can be replaced later with custom textures.
- For best results, place EnvironmentEffects_Rig at around y = 0 to 4, then let the particle shape modules emit above the player.

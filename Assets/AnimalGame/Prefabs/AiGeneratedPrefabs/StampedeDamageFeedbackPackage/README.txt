Stampede Damage Screen Feedback Prefab

What this package contains:
- Runtime/StampedeDamageScreenFeedback.cs
- Editor/StampedeDamageFeedbackPrefabCreator.cs
- Textures/StampedeBloodBorder.png
- Textures/StampedeRedVignette.png
- Textures/StampedeCenterFlash.png

Install:
1. Copy the folder StampedeDamageFeedback into your Unity project's Assets folder.
2. In Unity, wait for import to finish.
3. Go to Tools > Stampede > Create Damage Feedback Prefab.
4. Unity will create:
   Assets/StampedeDamageFeedback/Prefabs/StampedeDamageScreenFeedback.prefab
5. Drag that prefab into your main scene.

Trigger it when player is hurt:
In StampedeMiniGameController, inside TryConsumeStampedeLife(), after the life count/UI updates, add:

StampedeDamageScreenFeedback.Instance?.PlayHitFeedback();

Recommended placement:
- Call it for both AI hits and rock hits by putting it inside TryConsumeStampedeLife(), not inside the separate hit reaction methods.

Tuning:
Open the prefab and adjust:
- Vignette Peak Alpha
- Blood Peak Alpha
- Pop In Duration
- Hold Duration
- Fade Out Duration
- Pulse Scale Amount

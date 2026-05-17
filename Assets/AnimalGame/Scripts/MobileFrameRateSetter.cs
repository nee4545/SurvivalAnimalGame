using UnityEngine;

public class MobileFrameRateSetter : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetFrameRate()
    {
        // vSyncCount is ignored on Android/iOS, but set this anyway for Editor/standalone consistency.
        QualitySettings.vSyncCount = 0;

        // Try 60 first.
        Application.targetFrameRate = 60;

        // Keep physics stable for 60 FPS.
        Time.fixedDeltaTime = 1f / 60f;

        Debug.Log($"[FrameRate] targetFrameRate set to {Application.targetFrameRate}");
    }
}
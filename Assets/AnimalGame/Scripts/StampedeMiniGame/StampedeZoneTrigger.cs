using UnityEngine;

public class StampedeZoneTrigger : MonoBehaviour
{
    public bool oneTimeUse = false;
    public string playerTag = "Player";

    [Header("Re-trigger Safety")]
    public bool mustExitBeforeRestart = true;

    private bool used;
    private bool waitingForPlayerExit;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (used && oneTimeUse)
            return;

        if (mustExitBeforeRestart && waitingForPlayerExit)
            return;

        StampedeMiniGameController controller = StampedeMiniGameController.Instance;

        if (controller == null)
        {
            Debug.LogWarning("[StampedeZoneTrigger] No StampedeMiniGameController found.");
            return;
        }

        if (controller.IsRunning)
            return;

        used = true;
        waitingForPlayerExit = true;

        controller.StartStampedeMiniGame();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        StampedeMiniGameController controller = StampedeMiniGameController.Instance;

        // If player exits because he was teleported into stampede mode,
        // do NOT re-arm the trigger yet.
        if (controller != null && controller.IsRunning)
            return;

        waitingForPlayerExit = false;

        if (!oneTimeUse)
            used = false;
    }
}
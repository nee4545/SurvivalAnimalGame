using UnityEngine;

public class RiverEscapeZoneTrigger : MonoBehaviour
{
    public RiverEscapeMiniGameController controller;
    public string playerTag = "Player";
    public bool triggerOnlyOnce = false;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (controller == null)
            return;

        hasTriggered = true;
        controller.StartRiverEscapeMiniGame();
    }
}
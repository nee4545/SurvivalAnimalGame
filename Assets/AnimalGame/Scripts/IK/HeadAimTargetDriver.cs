using UnityEngine;

public class HeadAimTargetDriver : MonoBehaviour
{
    public CuteAnimalAI ai;
    public Transform headAimTarget;
    public float heightOffset = 1.2f;     // aim point above target's origin
    public float followLerp = 6f;         // smoothing
    public float maxLead = 0.6f;          // small lead on moving players

    void Reset() { ai = GetComponent<CuteAnimalAI>(); }

    void Update()
    {
        if (!ai || !headAimTarget) return;

        // Pick something interesting to look at
        //Transform t = ai.GetClosestThreatForCombat() ?? ai.player;
        //if (!t) return;

        //Vector3 targetPos = t.position + Vector3.up * heightOffset;

        //// tiny lead if target is moving (useful for the player)
        //if (t.TryGetComponent<Rigidbody>(out var rb))
        //    targetPos += Vector3.ClampMagnitude(rb.velocity * 0.1f, maxLead);

        //headAimTarget.position = Vector3.Lerp(headAimTarget.position, targetPos, followLerp * Time.deltaTime);
    }
}

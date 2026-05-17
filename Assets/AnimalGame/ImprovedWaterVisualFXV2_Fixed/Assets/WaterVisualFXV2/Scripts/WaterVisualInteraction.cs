using UnityEngine;

public class WaterVisualInteraction : MonoBehaviour
{
    [Header("Detection")]
    public string playerTag = "Player";

    [Header("Water FX")]
    public GameObject enterSplashPrefab;
    public GameObject ripplePrefab;

    [Header("Spawn Settings")]
    public Transform waterSurfaceReference;
    public float surfaceOffset = 0.03f;

    [Header("Ripple Settings")]
    public float rippleInterval = 0.35f;
    public float minMoveDistance = 0.03f;

    private Transform player;
    private Vector3 lastPlayerPosition;
    private float nextRippleTime;
    private bool playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        player = other.transform;
        playerInside = true;
        lastPlayerPosition = player.position;
        nextRippleTime = Time.time + rippleInterval * 0.5f;

        SpawnFX(enterSplashPrefab, player.position);
        SpawnFX(ripplePrefab, player.position);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!playerInside || player == null) return;
        if (!other.CompareTag(playerTag)) return;

        float movedDistance = Vector3.Distance(player.position, lastPlayerPosition);

        if (movedDistance >= minMoveDistance && Time.time >= nextRippleTime)
        {
            SpawnFX(ripplePrefab, player.position);
            lastPlayerPosition = player.position;
            nextRippleTime = Time.time + rippleInterval;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        player = null;
    }

    private void SpawnFX(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        Vector3 spawnPos = position;
        spawnPos.y = waterSurfaceReference != null
            ? waterSurfaceReference.position.y + surfaceOffset
            : transform.position.y + surfaceOffset;

        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}
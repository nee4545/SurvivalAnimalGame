using System.Collections;
using UnityEngine;

public class StampedeWorldScroller : MonoBehaviour
{
    [Header("References")]
    public StampedeLaneController laneController;
    public Transform player;

    [Header("Ground Tiles")]
    public Transform[] groundTiles;

    [Header("Movement")]
    public float scrollSpeed = 15f;

    [Tooltip("Actual world-space length of one tile along stampede forward direction.")]
    public float tileLength = 20f;

    [Tooltip("Fallback: how far past the player before a tile is recycled. Use positive value.")]
    public float recycleDistance = 25f;

    [Tooltip("Fallback: extra offset from player when resetting tiles. Use positive value.")]
    public float startOffsetFromPlayer = 0f;

    [Header("Direction")]
    public bool invertScrollDirection = false;

    [Header("Direction Specific Scroll Values")]
    [Tooltip("When ON, the scroller uses separate recycle/start offset values for normal and inverted mode.")]
    public bool useDirectionSpecificValues = true;

    [Tooltip("Used when Invert Scroll Direction is OFF.")]
    public float normalRecycleDistance = 25f;

    [Tooltip("Used when Invert Scroll Direction is OFF.")]
    public float normalStartOffsetFromPlayer = 0f;

    [Tooltip("Used when Invert Scroll Direction is ON.")]
    public float invertedRecycleDistance = 25f;

    [Tooltip("Used when Invert Scroll Direction is ON.")]
    public float invertedStartOffsetFromPlayer = 0f;

    [Header("Runtime Speed Slow")]
    [SerializeField] private float runtimeSpeedMultiplier = 1f;

    private Coroutine speedSlowRoutine;

    [Header("Debug")]
    public bool debugLogs;

    private bool isScrolling;

    public void Begin()
    {
        ResetTilesAroundPlayer();
        isScrolling = true;

        if (debugLogs)
            Debug.Log("[StampedeWorldScroller] Started.");
    }

    public void Stop()
    {
        isScrolling = false;

        ResetRuntimeSpeed();

        if (debugLogs)
            Debug.Log("[StampedeWorldScroller] Stopped.");
    }

    private void Update()
    {
        if (!isScrolling)
            return;

        if (laneController == null || player == null)
            return;

        if (groundTiles == null || groundTiles.Length == 0)
            return;

        Vector3 pathForward = GetPathForward();
        Vector3 moveDirection = GetMoveDirection(pathForward);

        float speed = GetCurrentScrollSpeed();

        for (int i = 0; i < groundTiles.Length; i++)
        {
            Transform tile = groundTiles[i];

            if (tile == null)
                continue;

            tile.position += moveDirection * speed * Time.deltaTime;
        }

        RecycleTiles(pathForward, moveDirection);
    }

    private void ResetTilesAroundPlayer()
    {
        if (laneController == null || player == null)
            return;

        if (groundTiles == null || groundTiles.Length == 0)
            return;

        Vector3 pathForward = GetPathForward();
        Vector3 moveDirection = GetMoveDirection(pathForward);

        bool movingAlongForward = Vector3.Dot(moveDirection, pathForward) > 0f;

        float length = Mathf.Abs(tileLength);
        float offset = Mathf.Abs(GetActiveStartOffsetFromPlayer());

        // If tiles move forward, place them behind player.
        // If tiles move backward, place them in front of player.
        Vector3 spawnDirection = movingAlongForward ? -pathForward : pathForward;

        Vector3 startPosition =
            player.position +
            spawnDirection * offset;

        for (int i = 0; i < groundTiles.Length; i++)
        {
            Transform tile = groundTiles[i];

            if (tile == null)
                continue;

            Vector3 pos = startPosition + spawnDirection * length * i;
            pos.y = tile.position.y;

            tile.position = pos;
        }

        if (debugLogs)
        {
            Debug.Log("[StampedeWorldScroller] Tiles reset.");
            Debug.Log("[StampedeWorldScroller] Path Forward: " + pathForward);
            Debug.Log("[StampedeWorldScroller] Move Direction: " + moveDirection);
            Debug.Log("[StampedeWorldScroller] Moving Along Forward: " + movingAlongForward);
            Debug.Log("[StampedeWorldScroller] Active Recycle Distance: " + GetActiveRecycleDistance());
            Debug.Log("[StampedeWorldScroller] Active Start Offset: " + GetActiveStartOffsetFromPlayer());
        }
    }

    private float GetActiveRecycleDistance()
    {
        if (!useDirectionSpecificValues)
            return recycleDistance;

        return invertScrollDirection
            ? invertedRecycleDistance
            : normalRecycleDistance;
    }

    private float GetActiveStartOffsetFromPlayer()
    {
        if (!useDirectionSpecificValues)
            return startOffsetFromPlayer;

        return invertScrollDirection
            ? invertedStartOffsetFromPlayer
            : normalStartOffsetFromPlayer;
    }

    private void RecycleTiles(Vector3 pathForward, Vector3 moveDirection)
    {
        bool movingAlongForward = Vector3.Dot(moveDirection, pathForward) > 0f;

        float recycle = Mathf.Abs(GetActiveRecycleDistance());
        float length = Mathf.Abs(tileLength);

        float frontMost = GetFrontMostDistance(pathForward);
        float backMost = GetBackMostDistance(pathForward);

        for (int i = 0; i < groundTiles.Length; i++)
        {
            Transform tile = groundTiles[i];

            if (tile == null)
                continue;

            float distance =
                Vector3.Dot(tile.position - player.position, pathForward);

            if (movingAlongForward)
            {
                // Tile moved too far in front of player, recycle it behind.
                if (distance > recycle)
                {
                    Vector3 newPos =
                        player.position +
                        pathForward * (backMost - length);

                    newPos.y = tile.position.y;
                    tile.position = newPos;

                    backMost -= length;

                    if (debugLogs)
                        Debug.Log("[StampedeWorldScroller] Recycled behind: " + tile.name);
                }
            }
            else
            {
                // Tile moved too far behind player, recycle it in front.
                if (distance < -recycle)
                {
                    Vector3 newPos =
                        player.position +
                        pathForward * (frontMost + length);

                    newPos.y = tile.position.y;
                    tile.position = newPos;

                    frontMost += length;

                    if (debugLogs)
                        Debug.Log("[StampedeWorldScroller] Recycled front: " + tile.name);
                }
            }
        }
    }

    private float GetFrontMostDistance(Vector3 pathForward)
    {
        float frontMost = float.NegativeInfinity;

        for (int i = 0; i < groundTiles.Length; i++)
        {
            Transform tile = groundTiles[i];

            if (tile == null)
                continue;

            float distance =
                Vector3.Dot(tile.position - player.position, pathForward);

            if (distance > frontMost)
                frontMost = distance;
        }

        return frontMost == float.NegativeInfinity ? 0f : frontMost;
    }

    private float GetBackMostDistance(Vector3 pathForward)
    {
        float backMost = float.PositiveInfinity;

        for (int i = 0; i < groundTiles.Length; i++)
        {
            Transform tile = groundTiles[i];

            if (tile == null)
                continue;

            float distance =
                Vector3.Dot(tile.position - player.position, pathForward);

            if (distance < backMost)
                backMost = distance;
        }

        return backMost == float.PositiveInfinity ? 0f : backMost;
    }

    private Vector3 GetPathForward()
    {
        Vector3 forward = laneController.GetForwardDirection();
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private Vector3 GetMoveDirection(Vector3 pathForward)
    {
        return invertScrollDirection ? pathForward : -pathForward;
    }

    public Vector3 GetCurrentScrollMoveDirection()
    {
        if (laneController == null)
            return Vector3.back;

        Vector3 pathForward = laneController.GetForwardDirection();
        pathForward.y = 0f;

        if (pathForward.sqrMagnitude < 0.001f)
            pathForward = Vector3.forward;

        pathForward.Normalize();

        return invertScrollDirection ? pathForward : -pathForward;
    }

    public float GetCurrentScrollSpeed()
    {
        return Mathf.Abs(scrollSpeed) * runtimeSpeedMultiplier;
    }

    public void PlayTemporarySpeedSlow(
        float slowMultiplier,
        float slowInDuration,
        float holdDuration,
        float recoverDuration
    )
    {
        if (speedSlowRoutine != null)
            StopCoroutine(speedSlowRoutine);

        speedSlowRoutine = StartCoroutine(
            SpeedSlowRoutine(
                slowMultiplier,
                slowInDuration,
                holdDuration,
                recoverDuration
            )
        );
    }

    private IEnumerator SpeedSlowRoutine(
        float slowMultiplier,
        float slowInDuration,
        float holdDuration,
        float recoverDuration
    )
    {
        slowMultiplier = Mathf.Clamp01(slowMultiplier);

        float startMultiplier = runtimeSpeedMultiplier;

        float timer = 0f;

        while (timer < slowInDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / slowInDuration);
            runtimeSpeedMultiplier = Mathf.Lerp(startMultiplier, slowMultiplier, t);
            yield return null;
        }

        runtimeSpeedMultiplier = slowMultiplier;

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        timer = 0f;

        while (timer < recoverDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / recoverDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            runtimeSpeedMultiplier = Mathf.Lerp(slowMultiplier, 1f, t);
            yield return null;
        }

        runtimeSpeedMultiplier = 1f;
        speedSlowRoutine = null;
    }

    public void ResetRuntimeSpeed()
    {
        if (speedSlowRoutine != null)
        {
            StopCoroutine(speedSlowRoutine);
            speedSlowRoutine = null;
        }

        runtimeSpeedMultiplier = 1f;
    }
}
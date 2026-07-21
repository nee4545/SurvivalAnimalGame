using UnityEngine;

public class RiverWorldScroller : MonoBehaviour
{
    [Header("References")]
    public Transform riverDirectionReference;
    public Transform recycleReference;

    [Header("Tiles")]
    public Transform[] riverTiles;
    public bool autoCalculateTileLength = true;
    public float tileLength = 40f;

    [Header("Scroll")]
    public bool scrollOnStart = false;
    public float scrollSpeed = 8f;

    [Tooltip("Usually ON. Player jumps forward, while river tiles move backward.")]
    public bool moveOppositeRiverForward = true;

    [Header("Recycle")]
    public float recycleDistanceBehindReference = 45f;

    [Header("Runtime Speed")]
    [SerializeField] private float runtimeSpeedMultiplier = 1f;

    [Header("Debug")]
    public bool debugLogs;
    public bool drawDebugGizmos;

    private bool isScrolling;

    private void Start()
    {
        if (autoCalculateTileLength)
            RecalculateTileLength();

        if (scrollOnStart)
            StartScrolling();
    }

    private void Update()
    {
        if (!isScrolling)
            return;

        MoveTiles();
        RecycleTiles();
    }

    public void StartScrolling()
    {
        if (autoCalculateTileLength)
            RecalculateTileLength();

        isScrolling = true;
        runtimeSpeedMultiplier = 1f;

        if (debugLogs)
            Debug.Log("[RiverWorldScroller] Started.");
    }

    public void StopScrolling()
    {
        isScrolling = false;
        runtimeSpeedMultiplier = 1f;

        if (debugLogs)
            Debug.Log("[RiverWorldScroller] Stopped.");
    }

    public Vector3 GetRiverForward()
    {
        Vector3 forward =
            riverDirectionReference != null
                ? riverDirectionReference.forward
                : transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    public Vector3 GetCurrentScrollMoveDirection()
    {
        Vector3 riverForward = GetRiverForward();

        return moveOppositeRiverForward
            ? -riverForward
            : riverForward;
    }

    public float GetCurrentScrollSpeed()
    {
        return Mathf.Abs(scrollSpeed) * runtimeSpeedMultiplier;
    }

    public bool IsScrolling()
    {
        return isScrolling;
    }

    public void SetRuntimeSpeedMultiplier(float multiplier)
    {
        runtimeSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ResetRuntimeSpeed()
    {
        runtimeSpeedMultiplier = 1f;
    }

    private void MoveTiles()
    {
        Vector3 moveDirection = GetCurrentScrollMoveDirection();
        float speed = GetCurrentScrollSpeed();

        Vector3 delta = moveDirection * speed * Time.deltaTime;

        for (int i = 0; i < riverTiles.Length; i++)
        {
            if (riverTiles[i] == null)
                continue;

            riverTiles[i].position += delta;
        }
    }

    private void RecycleTiles()
    {
        if (riverTiles == null || riverTiles.Length <= 1)
            return;

        Vector3 riverForward = GetRiverForward();
        Vector3 scrollDirection = GetCurrentScrollMoveDirection();

        float scrollDot = Vector3.Dot(scrollDirection, riverForward);

        float referenceProjection = GetReferenceProjection(riverForward);

        if (scrollDot < 0f)
        {
            RecycleBackwardMovingTiles(riverForward, referenceProjection);
        }
        else
        {
            RecycleForwardMovingTiles(riverForward, referenceProjection);
        }
    }

    private void RecycleBackwardMovingTiles(
        Vector3 riverForward,
        float referenceProjection
    )
    {
        for (int i = 0; i < riverTiles.Length; i++)
        {
            Transform tile = riverTiles[i];

            if (tile == null)
                continue;

            float tileProjection = Vector3.Dot(tile.position, riverForward);

            if (tileProjection >= referenceProjection - recycleDistanceBehindReference)
                continue;

            Transform furthestAheadTile = GetFurthestTile(riverForward, true);

            if (furthestAheadTile == null)
                continue;

            float furthestProjection =
                Vector3.Dot(furthestAheadTile.position, riverForward);

            float newProjection = furthestProjection + tileLength;
            float deltaProjection = newProjection - tileProjection;

            tile.position += riverForward * deltaProjection;
        }
    }

    private void RecycleForwardMovingTiles(
        Vector3 riverForward,
        float referenceProjection
    )
    {
        for (int i = 0; i < riverTiles.Length; i++)
        {
            Transform tile = riverTiles[i];

            if (tile == null)
                continue;

            float tileProjection = Vector3.Dot(tile.position, riverForward);

            if (tileProjection <= referenceProjection + recycleDistanceBehindReference)
                continue;

            Transform furthestBehindTile = GetFurthestTile(riverForward, false);

            if (furthestBehindTile == null)
                continue;

            float furthestProjection =
                Vector3.Dot(furthestBehindTile.position, riverForward);

            float newProjection = furthestProjection - tileLength;
            float deltaProjection = newProjection - tileProjection;

            tile.position += riverForward * deltaProjection;
        }
    }

    private Transform GetFurthestTile(Vector3 riverForward, bool ahead)
    {
        Transform result = null;
        float bestProjection = ahead ? float.MinValue : float.MaxValue;

        for (int i = 0; i < riverTiles.Length; i++)
        {
            Transform tile = riverTiles[i];

            if (tile == null)
                continue;

            float projection = Vector3.Dot(tile.position, riverForward);

            if (ahead)
            {
                if (projection > bestProjection)
                {
                    bestProjection = projection;
                    result = tile;
                }
            }
            else
            {
                if (projection < bestProjection)
                {
                    bestProjection = projection;
                    result = tile;
                }
            }
        }

        return result;
    }

    private float GetReferenceProjection(Vector3 riverForward)
    {
        Vector3 referencePosition =
            recycleReference != null
                ? recycleReference.position
                : transform.position;

        return Vector3.Dot(referencePosition, riverForward);
    }

    public void RecalculateTileLength()
    {
        float largestLength = 0f;

        for (int i = 0; i < riverTiles.Length; i++)
        {
            Transform tile = riverTiles[i];

            if (tile == null)
                continue;

            float length = GetObjectLengthAlongRiver(tile);

            if (length > largestLength)
                largestLength = length;
        }

        if (largestLength > 0.1f)
        {
            tileLength = largestLength;

            if (debugLogs)
                Debug.Log("[RiverWorldScroller] Auto tile length: " + tileLength);
        }
    }

    private float GetObjectLengthAlongRiver(Transform target)
    {
        Vector3 riverForward = GetRiverForward();

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        Collider[] colliders = target.GetComponentsInChildren<Collider>();

        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(target.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
        }

        if (!hasBounds)
            return tileLength;

        Vector3 extents = combinedBounds.extents;

        float halfLength =
            Mathf.Abs(riverForward.x) * extents.x +
            Mathf.Abs(riverForward.y) * extents.y +
            Mathf.Abs(riverForward.z) * extents.z;

        return halfLength * 2f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
            return;

        Vector3 forward = GetRiverForward();
        Vector3 scrollDirection = GetCurrentScrollMoveDirection();

        Vector3 origin =
            recycleReference != null
                ? recycleReference.position
                : transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + forward * 6f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + scrollDirection * 6f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            origin - forward * recycleDistanceBehindReference,
            1f
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            origin + forward * recycleDistanceBehindReference,
            1f
        );
    }
}
using System.Collections.Generic;
using UnityEngine;

public static class StampedePropSpawnReservation
{
    private class TimedReservation
    {
        public Vector3 position;
        public float expiresAt;
    }

    private class ActiveProp
    {
        public Transform transform;
    }

    private static readonly List<TimedReservation> reservations = new();
    private static readonly List<ActiveProp> activeProps = new();

    public static void Clear()
    {
        reservations.Clear();
        activeProps.Clear();
    }

    public static void RegisterActiveProp(Transform prop)
    {
        if (prop == null)
            return;

        for (int i = 0; i < activeProps.Count; i++)
        {
            if (activeProps[i].transform == prop)
                return;
        }

        activeProps.Add(new ActiveProp
        {
            transform = prop
        });
    }

    public static void UnregisterActiveProp(Transform prop)
    {
        if (prop == null)
            return;

        for (int i = activeProps.Count - 1; i >= 0; i--)
        {
            if (activeProps[i].transform == null || activeProps[i].transform == prop)
                activeProps.RemoveAt(i);
        }
    }

    public static bool TryReserve(
        Vector3 worldPosition,
        Vector3 laneForward,
        Vector3 laneRight,
        float forwardClearance,
        float sideClearance,
        float lifetime
    )
    {
        CleanupExpired();
        CleanupNullActiveProps();

        laneForward.y = 0f;
        laneRight.y = 0f;

        if (laneForward.sqrMagnitude < 0.001f)
            laneForward = Vector3.forward;

        if (laneRight.sqrMagnitude < 0.001f)
            laneRight = Vector3.right;

        laneForward.Normalize();
        laneRight.Normalize();

        if (!IsAreaFree(
            worldPosition,
            laneForward,
            laneRight,
            forwardClearance,
            sideClearance
        ))
        {
            return false;
        }

        reservations.Add(new TimedReservation
        {
            position = worldPosition,
            expiresAt = Time.time + lifetime
        });

        return true;
    }

    private static bool IsAreaFree(
        Vector3 worldPosition,
        Vector3 laneForward,
        Vector3 laneRight,
        float forwardClearance,
        float sideClearance
    )
    {
        for (int i = 0; i < reservations.Count; i++)
        {
            Vector3 delta = reservations[i].position - worldPosition;
            delta.y = 0f;

            float forwardDistance = Mathf.Abs(Vector3.Dot(delta, laneForward));
            float sideDistance = Mathf.Abs(Vector3.Dot(delta, laneRight));

            if (forwardDistance <= forwardClearance &&
                sideDistance <= sideClearance)
            {
                return false;
            }
        }

        for (int i = 0; i < activeProps.Count; i++)
        {
            Transform prop = activeProps[i].transform;

            if (prop == null)
                continue;

            Vector3 delta = prop.position - worldPosition;
            delta.y = 0f;

            float forwardDistance = Mathf.Abs(Vector3.Dot(delta, laneForward));
            float sideDistance = Mathf.Abs(Vector3.Dot(delta, laneRight));

            if (forwardDistance <= forwardClearance &&
                sideDistance <= sideClearance)
            {
                return false;
            }
        }

        return true;
    }

    private static void CleanupExpired()
    {
        for (int i = reservations.Count - 1; i >= 0; i--)
        {
            if (Time.time >= reservations[i].expiresAt)
                reservations.RemoveAt(i);
        }
    }

    private static void CleanupNullActiveProps()
    {
        for (int i = activeProps.Count - 1; i >= 0; i--)
        {
            if (activeProps[i].transform == null)
                activeProps.RemoveAt(i);
        }
    }
}
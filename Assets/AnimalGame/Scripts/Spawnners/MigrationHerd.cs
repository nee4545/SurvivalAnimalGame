using System.Collections.Generic;
using UnityEngine;

public class MigrationHerd : MonoBehaviour
{
    public List<CuteAnimalAI> members = new();

    public CuteAnimalAI Leader
    {
        get
        {
            members.RemoveAll(m => !m || !m.gameObject.activeInHierarchy);
            for (int i = 0; i < members.Count; i++)
                if (members[i].isMigrationLeader) return members[i];
            return members.Count > 0 ? members[0] : null;
        }
    }

    public void Register(CuteAnimalAI ai)
    {
        if (!ai || members.Contains(ai)) return;

        members.Add(ai);

        // First one becomes leader
        if (members.Count == 1)
            ai.isMigrationLeader = true;
    }

    public Vector3 GetFollowerSlot(CuteAnimalAI follower)
    {
        var leader = Leader;
        if (!leader) return follower.transform.position;

        // follower index (skip leader)
        int idx = members.IndexOf(follower);
        if (idx < 0) idx = 1;
        if (leader == follower) idx = 0;

        // Slot config (tweak in inspector via leader fields if you want)
        float backSpacing = Mathf.Max(leader.migrationMinSeparation, 1.2f);
        float sideSpacing = backSpacing * 0.85f;

        // Build a grid behind leader:
        // row 1: 2 followers, row 2: 3, row 3: 4, ...
        // Looks herd-like and reduces same-destination stacking.
        int followerNumber = Mathf.Max(0, idx - 1); // followers start at 0
        int row = 0;
        int capacity = 2;
        int remaining = followerNumber;

        while (remaining >= capacity)
        {
            remaining -= capacity;
            row++;
            capacity++; // next row has +1 slots
        }

        int col = remaining;               // 0..capacity-1
        float rowBack = (row + 1) * backSpacing;

        // Center columns around 0: e.g. for 3 slots -> -1,0,1
        float center = (capacity - 1) * 0.5f;
        float colSide = (col - center) * sideSpacing;

        // Behind leader
        Vector3 backDir = -leader.transform.forward;
        backDir.y = 0f;
        backDir.Normalize();

        // Side is leader’s right
        Vector3 rightDir = leader.transform.right;
        rightDir.y = 0f;
        rightDir.Normalize();

        Vector3 target = leader.transform.position + backDir * rowBack + rightDir * colSide;

        return target;
    }

}

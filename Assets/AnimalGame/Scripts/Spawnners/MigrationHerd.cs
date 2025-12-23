using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MigrationHerd : MonoBehaviour
{
    public List<CuteAnimalAI> Memebers = new();
    public bool IsResting { get; private set; }
    public float RestEndTime { get; private set; }

    public CuteAnimalAI Leader
    {
        get
        {
            Memebers.RemoveAll(m => m == null);

            for (int i = 0; i < Memebers.Count; i++)
                if (Memebers[i].isMigrationLeader) return Memebers[i];
            return Memebers.Count > 0 ? Memebers[0] : null;
        }
    }

    public void Register(CuteAnimalAI ai)
    {
        if (!ai || Memebers.Contains(ai)) return;

        Memebers.Add(ai);

        // First one becomes leader
        if (Memebers.Count == 1)
            ai.isMigrationLeader = true;
    }

    public void BeginRest(float seconds)
    {
        IsResting = seconds > 0.01f;
        RestEndTime = Time.unscaledTime + seconds;
    }

    public void EndRest()
    {
        IsResting = false;
    }

    public float RemainingRestTime =>
        IsResting ? Mathf.Max(0f, RestEndTime - Time.unscaledTime) : 0f;


    public void ResumeMigration()
    {
        foreach (var member in Memebers)
        {
            member.resumeMigrationAfterCombat = false;
            member.migrationInterrupted = false;

            member.StateMachine.ChangeState(
                new AIMigrateState(member, this)
            );
        }
    }

    public Vector3 GetFollowerSlot(CuteAnimalAI follower)
    {
        var leader = Leader;
        if (!leader) return follower.transform.position;

        // follower index (skip leader)
        int idx = Memebers.IndexOf(follower);
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

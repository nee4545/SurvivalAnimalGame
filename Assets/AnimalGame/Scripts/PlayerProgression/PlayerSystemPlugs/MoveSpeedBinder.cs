using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(StatsComponent))]
public class MoveSpeedBinder : MonoBehaviour
{
    public StatId moveSpeedStat;   // interpret as a *multiplier* with base=1
    public float baseSpeed = 5f;

    private StatsComponent _stats;
    private CharacterController _cc; // or your custom controller

    void Awake()
    {
        _stats = GetComponent<StatsComponent>();
        _cc = GetComponent<CharacterController>(); // swap to your controller
    }

    public float GetCurrentSpeed() => baseSpeed * _stats.Get(moveSpeedStat, 1f);
}


using System.Collections.Generic;
using UnityEngine;

public class AnimalAIActivityManager : MonoBehaviour
{
    public static AnimalAIActivityManager Instance;

    [Header("Distances")]
    public float fullActiveRadius = 45f;
    public float idleRadius = 80f;

    [Header("Update Rate")]
    public float updateInterval = 0.4f;

    Transform _player;
    float _nextUpdate;

    float _fullSqr;
    float _idleSqr;

    readonly List<CuteAnimalAI> _allAnimals = new();

    void Awake()
    {
        Instance = this;
        _fullSqr = fullActiveRadius * fullActiveRadius;
        _idleSqr = idleRadius * idleRadius;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) _player = p.transform;
    }

    public void Register(CuteAnimalAI ai)
    {
        if (ai && !_allAnimals.Contains(ai))
            _allAnimals.Add(ai);
    }

    public void Unregister(CuteAnimalAI ai)
    {
        _allAnimals.Remove(ai);
    }

    void Update()
    {
        if (!_player) return;
        if (Time.time < _nextUpdate) return;

        _nextUpdate = Time.time + updateInterval;
        Vector3 p = _player.position;

        for (int i = _allAnimals.Count - 1; i >= 0; i--)
        {
            var ai = _allAnimals[i];
            if (!ai || !ai.gameObject.activeInHierarchy)
            {
                _allAnimals.RemoveAt(i);
                continue;
            }

            float d2 = (ai.transform.position - p).sqrMagnitude;

            if (d2 <= _fullSqr)
                ai.SetActivityMode(AIActivityMode.Full);
            else if (d2 <= _idleSqr)
                ai.SetActivityMode(AIActivityMode.Idle);
            else
                ai.SetActivityMode(AIActivityMode.Dormant);
        }
    }
}

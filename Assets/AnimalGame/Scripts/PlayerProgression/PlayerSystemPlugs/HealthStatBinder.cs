// HealthStatBinder.cs
using UnityEngine;

[RequireComponent(typeof(StatsComponent))]
public class HealthStatBinder : MonoBehaviour
{
    public StatId maxHpStat;
    public bool keepRatioOnChange = true;

    private StatsComponent _stats;
    private Health _health;
    private float _last;

    void Awake()
    {
        _stats = GetComponent<StatsComponent>();
        _health = GetComponent<Health>();
    }

    //void Update()
    //{
    //    float v = _stats.Get(maxHpStat, _health.MaxHealth);
    //    if (Mathf.Approximately(v, _last)) return;

    //    float ratio = keepRatioOnChange ? (_health.CurrentHealth / Mathf.Max(1f, _health.MaxHealth)) : 1f;
    //    _health.MaxHealth = v;
    //    if (keepRatioOnChange)
    //        _health.CurrentHealth = Mathf.Clamp(v * ratio, 0, v);

    //    _last = v;
    //}
}

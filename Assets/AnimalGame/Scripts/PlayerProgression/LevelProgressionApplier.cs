// LevelProgressionApplier.cs
using UnityEngine;

[RequireComponent(typeof(LevelSystem))]
[RequireComponent(typeof(StatsComponent))]
public class LevelProgressionApplier : MonoBehaviour
{
    public LevelProgressionConfig config;
    public string sourceName = "Level";
    public int order = 100; // apply before equipment, buffs, etc.

    private LevelSystem _lvl;
    private StatsComponent _stats;

    void Awake()
    {
        _lvl = GetComponent<LevelSystem>();
        _stats = GetComponent<StatsComponent>();
        _lvl.OnLevelUp += _ => Reapply();
        Reapply();
    }

    public void Reapply()
    {
        if (!_stats || !config) return;
        foreach (var e in config.perLevel)
        {
            if (!e.stat) continue;
            _stats.RemoveModifiersFrom(e.stat, sourceName);
            var val = e.Evaluate(_lvl.level);
            _stats.AddModifier(e.stat, new StatModifier(sourceName, e.modType, val, order));
        }
    }
}

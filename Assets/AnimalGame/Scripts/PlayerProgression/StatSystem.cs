// StatSystem.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatModType { Flat, Percent } // Percent = +% to final

[Serializable]
public struct StatModifier
{
    public string source;          // "Level", "Item:Boots", "Buff:Rage"
    public StatModType type;
    public float value;            // Flat in units; Percent as 0.10 = +10%
    public int order;              // lower first (e.g., Level=100, Equip=200, Buff=300)

    public StatModifier(string source, StatModType type, float value, int order)
    { this.source = source; this.type = type; this.value = value; this.order = order; }
}

[Serializable]
public class StatValue
{
    public float baseValue;
    private readonly List<StatModifier> _mods = new();
    private bool _dirty;
    private float _cached;

    public void SetBase(float v) { baseValue = v; _dirty = true; }
    public IReadOnlyList<StatModifier> Mods => _mods;

    public void Add(StatModifier m) { _mods.Add(m); _dirty = true; }
    public void RemoveBySource(string source)
    {
        _mods.RemoveAll(m => m.source == source);
        _dirty = true;
    }

    public float GetFinal()
    {
        if (!_dirty) return _cached;

        float flat = 0f, percent = 0f;
        _mods.Sort((a, b) => a.order.CompareTo(b.order));
        foreach (var m in _mods)
        {
            if (m.type == StatModType.Flat) flat += m.value;
            else percent += m.value;
        }
        _cached = (baseValue + flat) * (1f + percent);
        _dirty = false;
        return _cached;
    }
}

public class StatsComponent : MonoBehaviour
{
    [Serializable] public class Entry { public StatId id; public float baseValue; }

    [Tooltip("Seed base values here; everything else stacks via modifiers.")]
    public List<Entry> initial = new();

    private readonly Dictionary<StatId, StatValue> _stats = new();

    public event Action<StatId> OnStatChanged;

    void Awake()
    {
        foreach (var e in initial)
        {
            if (!e.id) continue;
            var sv = new StatValue();
            sv.SetBase(e.baseValue);
            _stats[e.id] = sv;
        }
    }

    public float Get(StatId id, float fallback = 0f)
        => id && _stats.TryGetValue(id, out var s) ? s.GetFinal() : fallback;

    public void SetBase(StatId id, float v)
    {
        if (!id) return;
        if (!_stats.TryGetValue(id, out var s)) { s = new StatValue(); _stats[id] = s; }
        s.SetBase(v);
        OnStatChanged?.Invoke(id);
    }

    public void AddModifier(StatId id, StatModifier mod)
    {
        if (!id) return;
        if (!_stats.TryGetValue(id, out var s)) { s = new StatValue(); _stats[id] = s; }
        s.Add(mod);
        OnStatChanged?.Invoke(id);
    }

    public void RemoveModifiersFrom(StatId id, string source)
    {
        if (!id) return;
        if (_stats.TryGetValue(id, out var s))
        {
            s.RemoveBySource(source);
            OnStatChanged?.Invoke(id);
        }
    }
}

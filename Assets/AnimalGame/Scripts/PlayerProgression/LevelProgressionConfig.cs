// LevelProgressionConfig.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Level Progression")]
public class LevelProgressionConfig : ScriptableObject
{
    [System.Serializable]
    public class StatProgress
    {
        public StatId stat;
        public StatModType modType = StatModType.Flat; // Flat or Percent
        public AnimationCurve curve = AnimationCurve.Linear(1, 0, 50, 50);
        // Evaluate(level) -> value to apply as a modifier
        public float Evaluate(int level) => curve.Evaluate(level);
    }

    public List<StatProgress> perLevel = new();
}


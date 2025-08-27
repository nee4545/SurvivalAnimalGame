// LevelCurve.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Level Curve")]
public class LevelCurve : ScriptableObject
{
    [Tooltip("XP required to *reach* each level index (1..N). Index 0 unused.")]
    public List<int> xpToReachLevel = new() { 0, 0, 100, 300, 600, 1000 };
    public int MaxLevel => Mathf.Max(1, xpToReachLevel.Count - 1);
    public int XPForLevel(int lvl) => (lvl < xpToReachLevel.Count) ? xpToReachLevel[lvl] : int.MaxValue;
}

public class LevelSystem : MonoBehaviour
{
    public LevelCurve curve;
    public int level = 1;
    public int currentXP = 0;

    public event System.Action<int> OnLevelUp; // passes new level

    public void AddXP(int amount)
    {
        currentXP += Mathf.Max(0, amount);
        while (level < curve.MaxLevel && currentXP >= curve.XPForLevel(level + 1))
        {
            level++;
            OnLevelUp?.Invoke(level);
        }
    }
}

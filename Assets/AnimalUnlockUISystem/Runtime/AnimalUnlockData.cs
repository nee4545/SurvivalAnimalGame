using UnityEngine;

[CreateAssetMenu(menuName = "Wild Paws/Animal Unlock Data", fileName = "AnimalUnlockData")]
public class AnimalUnlockData : ScriptableObject
{
    public CuteAnimalAI.AnimalType animalType;
    public string displayName;
    public Sprite icon;
    public int unlockLevel = 1;
    public GameObject playerAnimalPrefab;
}

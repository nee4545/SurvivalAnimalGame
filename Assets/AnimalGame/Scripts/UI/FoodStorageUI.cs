using TMPro;
using UnityEngine;

public class FoodStorageUI : MonoBehaviour
{
    [Header("References")]
    public HomeFoodPoint foodPoint;
    public TextMeshProUGUI foodText;

    [Header("Display")]
    public string format = "{0}/{1}";

    private void Update()
    {
        if (foodPoint == null || foodText == null)
            return;

        foodText.text = string.Format(
            format,
            foodPoint.CurrentFoodCount,
            foodPoint.maxFoodCapacity
        );
    }
}
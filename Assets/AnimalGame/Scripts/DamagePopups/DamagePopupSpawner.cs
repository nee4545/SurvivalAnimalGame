using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    public static DamagePopupSpawner Instance { get; private set; }

    [Header("Popup Prefab")]
    public DamagePopup popupPrefab;

    [Header("Spawn Offset")]
    public Vector3 defaultOffset = new Vector3(0f, 1.5f, 0f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowPopup(Vector3 worldPosition, int damage)
    {
        if (popupPrefab == null) return;

        DamagePopup popup = Instantiate(
            popupPrefab,
            worldPosition + defaultOffset,
            Quaternion.identity
        );

        popup.Init(damage);
    }

    public void ShowTextPopup(Vector3 worldPosition, string text)
    {
        if (popupPrefab == null) return;

        DamagePopup popup = Instantiate(
            popupPrefab,
            worldPosition + defaultOffset,
            Quaternion.identity
        );

        popup.InitText(text);
    }
}
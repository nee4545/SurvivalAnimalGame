#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AnimalUnlockUIPrefabBuilder
{
    private const string PrefabFolder = "Assets/AnimalUnlockUISystem/Generated";
    private const string PrefabPath = PrefabFolder + "/AnimalUnlockPanel.prefab";

    [MenuItem("Tools/Wild Paws UI/Create Animal Unlock UI Prefab")]
    public static void CreateAnimalUnlockUIPrefab()
    {
        EnsureFolder("Assets", "AnimalUnlockUISystem");
        EnsureFolder("Assets/AnimalUnlockUISystem", "Generated");

        GameObject root = CreateUIObject("AnimalUnlockPanel", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(900f, 1200f);

        Image rootBg = root.AddComponent<Image>();
        rootBg.color = new Color(0.11f, 0.08f, 0.05f, 0.94f);

        AnimalCollectionUI collection = root.AddComponent<AnimalCollectionUI>();

        GameObject header = CreateUIObject("Header", root.transform);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        StretchTop(headerRect, 0f, 0f, 0f, 150f);

        TextMeshProUGUI title = CreateText("TitleText", header.transform, "Animal Family", 52, TextAlignmentOptions.Left);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0.65f, 1f);
        titleRect.offsetMin = new Vector2(36f, 0f);
        titleRect.offsetMax = new Vector2(0f, 0f);

        TextMeshProUGUI levelText = CreateText("LevelText", header.transform, "Level 1", 34, TextAlignmentOptions.Right);
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.65f, 0f);
        levelRect.anchorMax = new Vector2(1f, 1f);
        levelRect.offsetMin = new Vector2(0f, 0f);
        levelRect.offsetMax = new Vector2(-36f, 0f);
        collection.levelText = levelText;

        GameObject scrollView = CreateUIObject("AnimalScrollView", root.transform);
        RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(32f, 36f);
        scrollRectTransform.offsetMax = new Vector2(-32f, -170f);

        Image scrollRaycastImage = scrollView.AddComponent<Image>();
        scrollRaycastImage.color = new Color(1f, 1f, 1f, 0f);
        scrollRaycastImage.raycastTarget = true;

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 35f;

        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);
        contentRect.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(20, 20, 20, 40);
        grid.cellSize = new Vector2(245f, 310f);
        grid.spacing = new Vector2(20f, 20f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        content.AddComponent<GridScrollContentFitter>();

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        collection.contentParent = content.transform;

        GameObject card = CreateAnimalCard(content.transform);
        card.name = "AnimalCardPrefab_Template";
        AnimalUnlockCardUI cardUI = card.GetComponent<AnimalUnlockCardUI>();
        collection.cardPrefab = cardUI;
        card.SetActive(false);

        for (int i = 0; i < 13; i++)
        {
            GameObject sample = Object.Instantiate(card, content.transform);
            sample.name = "SampleAnimalCard_" + (i + 1).ToString("00");
            sample.SetActive(true);
            AnimalUnlockCardUI sampleUI = sample.GetComponent<AnimalUnlockCardUI>();
            sampleUI.animalNameText.text = "Animal " + (i + 1);
            sampleUI.requiredLevelText.text = "Unlocks at Lv. " + (i + 1);
        }

        Selection.activeGameObject = root;
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animal Unlock UI prefab created at: " + PrefabPath);
    }

    private static GameObject CreateAnimalCard(Transform parent)
    {
        GameObject card = CreateUIObject("AnimalCard", parent);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(245f, 310f);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.24f, 0.16f, 0.09f, 1f);
        bg.raycastTarget = false;

        AnimalUnlockCardUI cardUI = card.AddComponent<AnimalUnlockCardUI>();
        cardUI.background = bg;

        GameObject iconObj = CreateUIObject("AnimalIcon", card.transform);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -24f);
        iconRect.sizeDelta = new Vector2(170f, 150f);
        Image icon = iconObj.AddComponent<Image>();
        icon.color = new Color(1f, 1f, 1f, 1f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        cardUI.animalIcon = icon;

        TextMeshProUGUI nameText = CreateText("AnimalNameText", card.transform, "Animal", 28, TextAlignmentOptions.Center);
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.offsetMin = new Vector2(10f, 84f);
        nameRect.offsetMax = new Vector2(-10f, 132f);
        cardUI.animalNameText = nameText;

        GameObject badge = CreateUIObject("UnlockedBadge", card.transform);
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 0f);
        badgeRect.anchorMax = new Vector2(0.5f, 0f);
        badgeRect.pivot = new Vector2(0.5f, 0f);
        badgeRect.anchoredPosition = new Vector2(0f, 22f);
        badgeRect.sizeDelta = new Vector2(150f, 42f);
        Image badgeImage = badge.AddComponent<Image>();
        badgeImage.color = new Color(0.35f, 0.62f, 0.24f, 1f);
        badgeImage.raycastTarget = false;
        TextMeshProUGUI badgeText = CreateText("Text", badge.transform, "Unlocked", 22, TextAlignmentOptions.Center);
        badgeText.color = Color.white;
        StretchAll(badgeText.GetComponent<RectTransform>(), 0f);
        cardUI.unlockedBadge = badge;

        GameObject lockOverlay = CreateUIObject("LockOverlay", card.transform);
        RectTransform lockRect = lockOverlay.GetComponent<RectTransform>();
        StretchAll(lockRect, 0f);
        Image lockBg = lockOverlay.AddComponent<Image>();
        lockBg.color = new Color(0f, 0f, 0f, 0.52f);
        lockBg.raycastTarget = false;

        TextMeshProUGUI lockText = CreateText("RequiredLevelText", lockOverlay.transform, "Unlocks at Lv. 5", 24, TextAlignmentOptions.Center);
        RectTransform lockTextRect = lockText.GetComponent<RectTransform>();
        lockTextRect.anchorMin = new Vector2(0f, 0.5f);
        lockTextRect.anchorMax = new Vector2(1f, 0.5f);
        lockTextRect.pivot = new Vector2(0.5f, 0.5f);
        lockTextRect.sizeDelta = new Vector2(0f, 70f);
        lockTextRect.anchoredPosition = new Vector2(0f, -20f);
        cardUI.requiredLevelText = lockText;
        cardUI.lockOverlay = lockOverlay;

        GameObject buttonObj = CreateUIObject("SelectButton", card.transform);
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 16f);
        buttonRect.sizeDelta = new Vector2(180f, 50f);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.95f, 0.62f, 0.25f, 1f);
        Button button = buttonObj.AddComponent<Button>();
        cardUI.selectButton = button;
        TextMeshProUGUI buttonText = CreateText("Text", buttonObj.transform, "Select", 22, TextAlignmentOptions.Center);
        buttonText.color = Color.white;
        StretchAll(buttonText.GetComponent<RectTransform>(), 0f);

        return card;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent)
            go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = new Color(1f, 0.92f, 0.78f, 1f);
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void StretchTop(RectTransform rect, float left, float right, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void StretchAll(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif

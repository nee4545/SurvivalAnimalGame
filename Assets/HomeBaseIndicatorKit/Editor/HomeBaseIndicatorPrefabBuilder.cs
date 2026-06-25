#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HomeBaseIndicatorPrefabBuilder
{
    private const string RootFolder = "Assets/HomeBaseIndicatorKit";
    private const string PrefabFolder = RootFolder + "/Generated";
    private const string PrefabPath = PrefabFolder + "/HomeBaseIndicator.prefab";

    [MenuItem("Tools/Game UI/Create Home Base Indicator Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolder("Assets", "HomeBaseIndicatorKit");
        EnsureFolder(RootFolder, "Generated");

        Sprite arrowSprite = FindAndPrepareArrowSprite();

        GameObject root = new GameObject(
            "HomeBaseIndicatorController",
            typeof(RectTransform),
            typeof(HomeBaseIndicator)
        );

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject visual = new GameObject(
            "HomeIndicatorVisual",
            typeof(RectTransform),
            typeof(CanvasGroup)
        );
        visual.transform.SetParent(root.transform, false);

        RectTransform visualRect = visual.GetComponent<RectTransform>();
        visualRect.anchorMin = new Vector2(0.5f, 1f);
        visualRect.anchorMax = new Vector2(0.5f, 1f);
        visualRect.pivot = new Vector2(0.5f, 1f);
        visualRect.anchoredPosition = new Vector2(0f, -55f);
        visualRect.sizeDelta = new Vector2(150f, 175f);

        GameObject arrow = new GameObject(
            "ArrowImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        arrow.transform.SetParent(visual.transform, false);

        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 1f);
        arrowRect.anchorMax = new Vector2(0.5f, 1f);
        arrowRect.pivot = new Vector2(0.5f, 1f);
        arrowRect.anchoredPosition = Vector2.zero;
        arrowRect.sizeDelta = new Vector2(120f, 120f);

        Image arrowImage = arrow.GetComponent<Image>();
        arrowImage.sprite = arrowSprite;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        GameObject distance = new GameObject(
            "DistanceText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        distance.transform.SetParent(visual.transform, false);

        RectTransform distanceRect = distance.GetComponent<RectTransform>();
        distanceRect.anchorMin = new Vector2(0.5f, 0f);
        distanceRect.anchorMax = new Vector2(0.5f, 0f);
        distanceRect.pivot = new Vector2(0.5f, 0f);
        distanceRect.anchoredPosition = new Vector2(0f, 8f);
        distanceRect.sizeDelta = new Vector2(180f, 42f);

        TextMeshProUGUI distanceText = distance.GetComponent<TextMeshProUGUI>();
        distanceText.text = "100m";
        distanceText.alignment = TextAlignmentOptions.Center;
        distanceText.fontSize = 28f;
        distanceText.fontStyle = FontStyles.Bold;
        distanceText.color = Color.white;
        distanceText.raycastTarget = false;
        distanceText.enableAutoSizing = true;
        distanceText.fontSizeMin = 18f;
        distanceText.fontSizeMax = 30f;

        HomeBaseIndicator indicator = root.GetComponent<HomeBaseIndicator>();
        SerializedObject serializedIndicator = new SerializedObject(indicator);
        serializedIndicator.FindProperty("indicatorVisual").objectReferenceValue = visual;
        serializedIndicator.FindProperty("arrowRect").objectReferenceValue = arrowRect;
        serializedIndicator.FindProperty("arrowImage").objectReferenceValue = arrowImage;
        serializedIndicator.FindProperty("distanceText").objectReferenceValue = distanceText;
        serializedIndicator.FindProperty("hideDistance").floatValue = 8f;
        serializedIndicator.ApplyModifiedPropertiesWithoutUndo();

        visual.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"Created Home Base Indicator prefab at: {PrefabPath}");
    }

    private static Sprite FindAndPrepareArrowSprite()
    {
        string[] guids = AssetDatabase.FindAssets("HomeBaseIndicatorArrow t:Texture2D");
        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.alphaIsTransparency == false)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif

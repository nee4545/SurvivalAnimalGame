#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class StampedeDamageFeedbackPrefabCreator
{
    private const string RootFolder = "Assets/StampedeDamageFeedback";
    private const string TextureFolder = RootFolder + "/Textures";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string RuntimeFolder = RootFolder + "/Runtime";

    [MenuItem("Tools/Stampede/Create Damage Feedback Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolders();
        ConfigureTextureAsSprite(TextureFolder + "/StampedeBloodBorder.png");
        ConfigureTextureAsSprite(TextureFolder + "/StampedeRedVignette.png");
        ConfigureTextureAsSprite(TextureFolder + "/StampedeCenterFlash.png");

        Sprite bloodSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TextureFolder + "/StampedeBloodBorder.png");
        Sprite vignetteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TextureFolder + "/StampedeRedVignette.png");
        Sprite flashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TextureFolder + "/StampedeCenterFlash.png");

        GameObject root = new GameObject("StampedeDamageScreenFeedback", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image vignette = CreateFullScreenImage("RedVignette", root.transform, vignetteSprite, new Color(1f, 0f, 0f, 0f));
        Image blood = CreateFullScreenImage("BloodBorderSplatter", root.transform, bloodSprite, new Color(1f, 1f, 1f, 0f));
        Image flash = CreateFullScreenImage("CenterImpactFlash", root.transform, flashSprite, new Color(1f, 0.1f, 0.05f, 0f));

        StampedeDamageScreenFeedback feedback = root.AddComponent<StampedeDamageScreenFeedback>();
        feedback.rootGroup = canvasGroup;
        feedback.redVignetteImage = vignette;
        feedback.bloodBorderImage = blood;
        feedback.centerFlashImage = flash;

        feedback.popInDuration = 0.05f;
        feedback.holdDuration = 0.08f;
        feedback.fadeOutDuration = 0.45f;
        feedback.vignettePeakAlpha = 0.55f;
        feedback.bloodPeakAlpha = 0.9f;
        feedback.centerFlashPeakAlpha = 0.18f;
        feedback.pulseScaleAmount = 0.035f;
        feedback.useUnscaledTime = true;

        string prefabPath = PrefabFolder + "/StampedeDamageScreenFeedback.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Stampede Damage Feedback",
            "Prefab created at:\n" + prefabPath + "\n\nDrag it into your scene and call StampedeDamageScreenFeedback.Instance.PlayHitFeedback() when the player is hurt.",
            "Done"
        );

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "StampedeDamageFeedback");
        CreateFolderIfMissing(RootFolder, "Textures");
        CreateFolderIfMissing(RootFolder, "Prefabs");
        CreateFolderIfMissing(RootFolder, "Runtime");
    }

    private static void CreateFolderIfMissing(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void ConfigureTextureAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Image CreateFullScreenImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = false;

        return image;
    }
}
#endif

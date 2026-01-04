using System.IO;
using UnityEditor;
using UnityEngine;

public class GameViewThumbnailCapture : EditorWindow
{
    private int size = 512;
    private string outputFolder = "Assets/AnimalSprites";
    private string fileName = "AnimalThumbnail";

    [MenuItem("Tools/Capture Game View Thumbnail")]
    static void Open()
    {
        GetWindow<GameViewThumbnailCapture>("Game View Capture");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Game View → Sprite Capture", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Captures EXACTLY what the active Camera sees.\nMake sure the Game View looks correct before clicking Capture.",
            MessageType.Info
        );

        size = EditorGUILayout.IntPopup("Resolution", size,
            new[] { "256", "512", "1024" },
            new[] { 256, 512, 1024 });

        fileName = EditorGUILayout.TextField("File Name", fileName);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Capture From Camera", GUILayout.Height(40)))
        {
            Capture();
        }
    }

    private void Capture()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("No Camera.main found in scene.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        RenderTexture rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        RenderTexture prev = cam.targetTexture;
        RenderTexture.active = rt;

        cam.targetTexture = rt;
        cam.Render();

        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        cam.targetTexture = prev;
        RenderTexture.active = null;

        byte[] png = tex.EncodeToPNG();
        string path = $"{outputFolder}/{fileName}.png";
        File.WriteAllBytes(path, png);

        DestroyImmediate(rt);
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();

        Debug.Log($"Thumbnail captured: {path}");
    }
}

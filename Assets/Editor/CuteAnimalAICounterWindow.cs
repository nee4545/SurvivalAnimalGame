using UnityEditor;
using UnityEngine;

public class CuteAnimalAICounterWindow : EditorWindow
{
    [MenuItem("Tools/Debug/CuteAnimal AI Counter")]
    static void Open()
    {
        GetWindow<CuteAnimalAICounterWindow>("CuteAnimal AI Counter");
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "CuteAnimal AI Debug Tool",
            EditorStyles.boldLabel
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Count CuteAnimal AI in Scene", GUILayout.Height(40)))
        {
            Count();
        }
    }

    void Count()
    {
        // Includes inactive objects
        CuteAnimalAI[] animals = Object.FindObjectsOfType<CuteAnimalAI>(true);

        int total = animals.Length;
        int active = 0;
        int inactive = 0;

        foreach (var a in animals)
        {
            if (a.gameObject.activeInHierarchy)
                active++;
            else
                inactive++;
        }

        Debug.Log(
            $"🐾 CuteAnimalAI Count\n" +
            $"Total: {total}\n" +
            $"Active: {active}\n" +
            $"Inactive: {inactive}"
        );

        Debug.Log("Active Animals = "+ active.ToString() );
    }
}

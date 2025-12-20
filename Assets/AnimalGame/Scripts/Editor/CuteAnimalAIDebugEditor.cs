#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CuteAnimalAI))]
public class CuteAnimalAIDebugEditor : Editor
{
    private GUIStyle headerStyle;
    private GUIStyle valueStyle;

    void OnEnable()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 13;

        valueStyle = new GUIStyle(EditorStyles.label);
        valueStyle.richText = true;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CuteAnimalAI ai = (CuteAnimalAI)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Debug overlay available at runtime.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🐾 AI Debug Overlay", headerStyle);
        EditorGUILayout.Space(5);

        // State
        string stateName = ai.StateMachine?.CurrentState?.GetType().Name ?? "None";
        EditorGUILayout.LabelField("Current State:", $"<b>{stateName}</b>", valueStyle);

        // Type
        EditorGUILayout.LabelField("AI Type:", ai.aiType.ToString(), valueStyle);

        // Threat check
        Transform threat = ai.GetClosestThreatForCombat(ai.detectionRange);
        string threatLabel = threat ? threat.name : "None";
        float threatDist = threat ? Vector3.Distance(ai.transform.position, threat.position) : -1;

        EditorGUILayout.LabelField("Threat Target:", $"<color=orange>{threatLabel}</color>", valueStyle);
        if (threat) EditorGUILayout.LabelField("Threat Distance:", $"{threatDist:F1} units", valueStyle);

        // Flags
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Flags", headerStyle);

        ShowBool("Provoked", ai.wasProvoked);
        ShowBool("Territorial", ai.territorialAggressive);
        ShowBool("Is Migrating", ai.isMigratingMoving);
        ShowBool("Charge Cooldown", ai.isChargeCooldownActive);
        ShowBool("Jump Session Active", ai.jumpSessionActive);
        ShowBool("Perch Ready", Time.time >= ai.nextPerchEngageTime);

        // Cooldown timers
        if (ai.isChargeCooldownActive)
            EditorGUILayout.LabelField("Charge CD Left:", $"{ai.chargeCooldownTimer:F1}s", valueStyle);

        if (ai.jumpSessionActive)
            EditorGUILayout.LabelField("Jump Session Ends In:", $"{ai.jumpSessionDeadline - Time.time:F1}s", valueStyle);

        // NavMesh info
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("NavMesh Status", headerStyle);
        EditorGUILayout.LabelField("Path Pending:", ai.agent.pathPending.ToString(), valueStyle);
        EditorGUILayout.LabelField("Remaining Distance:", $"{ai.agent.remainingDistance:F1}", valueStyle);
    }

    void ShowBool(string label, bool value)
    {
        string val = value ? "<color=green>Yes</color>" : "<color=gray>No</color>";
        EditorGUILayout.LabelField(label + ":", val, valueStyle);
    }
}
#endif

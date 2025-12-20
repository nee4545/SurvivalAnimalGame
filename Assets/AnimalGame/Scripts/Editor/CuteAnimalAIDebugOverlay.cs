#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CuteAnimalAI))]
public class CuteAnimalAIDebugOverlay : Editor
{
    private void OnSceneGUI()
    {
        CuteAnimalAI ai = (CuteAnimalAI)target;
        if (!ai || !ai.agent) return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        Vector3 pos = ai.transform.position + Vector3.up * 2f;
        float labelSpacing = 15f;
        int line = 0;

        void DrawLine(string label, string value, Color c)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = c;
            Handles.Label(pos + Vector3.up * (line * 0.25f), new GUIContent($"{label}: {value}"), style);
            line++;
        }

        // ========== Basic Info ==========
        DrawLine("AI State", ai.StateMachine?.CurrentState?.GetType().Name ?? "None", Color.white);
        DrawLine("AI Type", ai.aiType.ToString(), Color.cyan);

        // ========== Threat Info ==========
        if (ai.player)
        {
            float distToPlayer = Vector3.Distance(ai.transform.position, ai.player.position);
            DrawLine("Player Distance", distToPlayer.ToString("F1") + " units", distToPlayer < ai.detectionRange ? Color.red : Color.gray);
        }

        // ========== Agent Info ==========
        DrawLine("Is On NavMesh", ai.agent.isOnNavMesh.ToString(), ai.agent.isOnNavMesh ? Color.green : Color.red);
        DrawLine("Is Stopped", ai.agent.isStopped.ToString(), ai.agent.isStopped ? Color.yellow : Color.green);
        DrawLine("Path Pending", ai.agent.pathPending.ToString(), ai.agent.pathPending ? Color.yellow : Color.white);
        DrawLine("Remaining Distance", ai.agent.remainingDistance.ToString("F1"), Color.white);
        DrawLine("Stopping Distance", ai.agent.stoppingDistance.ToString("F1"), Color.white);

        // ========== Destination Info ==========
        if (ai.agent.hasPath)
        {
            Handles.color = Color.green;
            Handles.DrawLine(ai.transform.position + Vector3.up * 0.2f, ai.agent.destination);
            Handles.SphereHandleCap(0, ai.agent.destination, Quaternion.identity, 0.3f, EventType.Repaint);
            DrawLine("Destination", ai.agent.destination.ToString(), Color.green);
        }

        // ========== Optional Threat Line ==========
        if (ai.aiType.ToString().ToLower().Contains("aggressive") && ai.player)
        {
            Handles.color = Color.red;
            Handles.DrawDottedLine(ai.transform.position, ai.player.position, 2f);
        }
    }
}
#endif

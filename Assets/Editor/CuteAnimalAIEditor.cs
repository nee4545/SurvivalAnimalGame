#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(CuteAnimalAI))]
[CanEditMultipleObjects]
public class CuteAnimalAIEditor : Editor
{
    // -----------------------------
    // Serialized Properties (tailored to your posted fields)
    // -----------------------------

    // Core
    SerializedProperty aiType, animalType;

    // Home / flocking (core movement block)
    SerializedProperty homeRadius, enableFlocking, repulsionDistance, repulsionStrength, alignmentWeight, cohesionWeight;
    SerializedProperty enableAutoBraking;

    // Movement
    SerializedProperty wanderSpeed, chaseSpeed, fleeSpeed, rotationSpeed;
    SerializedProperty stopDistance, wanderRadius, wanderInterval;

    // Detection / combat
    SerializedProperty detectionRange, fleeRange;
    SerializedProperty attackRange, attackDamage, attackCooldown, attackDuration;
    SerializedProperty facingConeToAttack;

    // Territory
    SerializedProperty territorialAggressive, aggressiveTerritoryRadius;
    SerializedProperty territorial, territoryRadius;

    // Pounce
    SerializedProperty canPounce, pounceCooldown, pounceHeight, pounceForwardDistance;
    SerializedProperty fleePounceTriggerDistance;

    // Flee
    SerializedProperty fleeAttempts, navMeshEdgeThreshold;
    SerializedProperty playerPredictionTime, playerForwardPredictionDistance;
    SerializedProperty fleeBurstMultiplier, fleeBurstDuration;
    SerializedProperty zigzagSpeed, zigzagStrength, zigzagLookahead;
    SerializedProperty preferHomeWhenFleeing, fleeHomeStartRadius, fleeHomeStopRadius;

    // Passive Herd
    SerializedProperty herdPanicRadius, herdJoinRadius;
    SerializedProperty herdCohesionWeightWhileFleeing, herdSeparationWeightWhileFleeing;
    SerializedProperty herdTightFleeSeconds, herdScatterRadius, herdSafeFactor;
    SerializedProperty herdPreferredDistance, herdRegroupSpeed;

    // Charge (Type1)
    SerializedProperty windupDuration, chargeSpeed, chargeDuration;
    SerializedProperty chargeDetectionRange, chargeDamage, chargeDamageRadius;
    SerializedProperty maxChargeAttempts, chargeCooldownDuration;

    // Retaliation (Type2)
    SerializedProperty retaliationDelay;

    // AggressiveType4
    SerializedProperty randomizeAggressiveStats, speedRandomRange;
    SerializedProperty turnRateDeg, turnSlowdownAngle, turnSlowdownMinFactor;
    SerializedProperty leadPredictionSeconds, leadMaxDistance, playerVelSmoothing;
    SerializedProperty t4Surges, t4SurgeEvery, t4SurgeMultiplier, t4SurgeDuration;
    SerializedProperty t4FatigueMultiplier, t4FatigueDuration, t4SpeedSmoothing;

    // Pack (Type3)
    SerializedProperty packCallRadius, coordinationTime, maxPackSize;
    SerializedProperty packChaseRadius, packCollapseSeconds, packMinRadiusFactor;
    SerializedProperty packAttackWindowDeg, packOrbitingEnabled;
    SerializedProperty packOrbitAngularSpeedDeg, packRepathInterval;
    SerializedProperty packRearBoost, packRearAngleWindowDeg;
    SerializedProperty packLungeSpeed, packLungeDuration, packLungeOvershoot, packLungeCooldown;

    // Jumping
    SerializedProperty jumpHeight, jumpDuration, jumpLandingProbeRadius;
    SerializedProperty chaseDuration, jumpSpotTag, jumpSpotApproachRadius;
    SerializedProperty perchCooldown, jumpReturnApproachDistance, jumpReturnStopDistance;
    SerializedProperty jumpHorizontalSpeed, minJumpDuration, maxJumpDuration;

    // Migration
    SerializedProperty migrationPath, migrationOwner;
    SerializedProperty migrationMoveSpeed, migrationFollowerSpeed;
    SerializedProperty migrationCohesionRadius, migrationMinSeparation;
    SerializedProperty migrationImpactRadius, migrationImpactDot;

    // Companion
    SerializedProperty companionFollowDistance, companionCircleRadius, companionCircleSpeed;
    SerializedProperty companionRotationLerp, companionJitterThresholdSq;
    SerializedProperty idleMoveInterval, idleMinRadius, idleMaxRadius;
    SerializedProperty targetTag, companionDetectionRange, maxChaseDistance;
    SerializedProperty limitTargetsToCameraView, screenEdgePadding, companionTargetCamera;
    SerializedProperty flockNeighborRadius, flockSeparationWeight, flockCohesionWeight, flockAlignmentWeight;

    // -----------------------------
    // UI state
    // -----------------------------
    static bool _showDesignerInspector = true;
    static bool _showRuntimeDebug = true;
    static bool _showSceneOverlay = true;

    // Debug refresh throttle (prevents heavy work every repaint)
    double _nextDebugRefreshAt;
    const double DebugRefreshInterval = 0.15; // ~6-7 times/sec

    // Cached debug values (avoid calling heavy methods every repaint)
    string _cachedStateName = "None";
    string _cachedThreatName = "None";
    float _cachedThreatDist = -1f;

    // Styles (cached)
    GUIStyle _headerStyle;
    GUIStyle _richStyle;

    void OnEnable()
    {
        // Core
        aiType = serializedObject.FindProperty("aiType");
        animalType = serializedObject.FindProperty("animalType");

        // Home / flocking
        homeRadius = serializedObject.FindProperty("homeRadius");
        enableFlocking = serializedObject.FindProperty("enableFlocking");
        repulsionDistance = serializedObject.FindProperty("repulsionDistance");
        repulsionStrength = serializedObject.FindProperty("repulsionStrength");
        alignmentWeight = serializedObject.FindProperty("alignmentWeight");
        cohesionWeight = serializedObject.FindProperty("cohesionWeight");
        enableAutoBraking = serializedObject.FindProperty("enableAutoBraking");

        // Movement
        wanderSpeed = serializedObject.FindProperty("wanderSpeed");
        chaseSpeed = serializedObject.FindProperty("chaseSpeed");
        fleeSpeed = serializedObject.FindProperty("fleeSpeed");
        rotationSpeed = serializedObject.FindProperty("rotationSpeed");
        stopDistance = serializedObject.FindProperty("stopDistance");
        wanderRadius = serializedObject.FindProperty("wanderRadius");
        wanderInterval = serializedObject.FindProperty("wanderInterval");

        // Detection / combat
        detectionRange = serializedObject.FindProperty("detectionRange");
        fleeRange = serializedObject.FindProperty("fleeRange");
        attackRange = serializedObject.FindProperty("attackRange");
        attackDamage = serializedObject.FindProperty("attackDamage");
        attackCooldown = serializedObject.FindProperty("attackCooldown");
        attackDuration = serializedObject.FindProperty("attackDuration");
        facingConeToAttack = serializedObject.FindProperty("facingConeToAttack");

        // Territory
        territorialAggressive = serializedObject.FindProperty("territorialAggressive");
        aggressiveTerritoryRadius = serializedObject.FindProperty("aggressiveTerritoryRadius");
        territorial = serializedObject.FindProperty("territorial");
        territoryRadius = serializedObject.FindProperty("territoryRadius");

        // Pounce
        canPounce = serializedObject.FindProperty("canPounce");
        pounceCooldown = serializedObject.FindProperty("pounceCooldown");
        pounceHeight = serializedObject.FindProperty("pounceHeight");
        pounceForwardDistance = serializedObject.FindProperty("pounceForwardDistance");
        fleePounceTriggerDistance = serializedObject.FindProperty("fleePounceTriggerDistance");

        // Flee
        fleeAttempts = serializedObject.FindProperty("fleeAttempts");
        navMeshEdgeThreshold = serializedObject.FindProperty("navMeshEdgeThreshold");
        playerPredictionTime = serializedObject.FindProperty("playerPredictionTime");
        playerForwardPredictionDistance = serializedObject.FindProperty("playerForwardPredictionDistance");
        fleeBurstMultiplier = serializedObject.FindProperty("fleeBurstMultiplier");
        fleeBurstDuration = serializedObject.FindProperty("fleeBurstDuration");
        zigzagSpeed = serializedObject.FindProperty("zigzagSpeed");
        zigzagStrength = serializedObject.FindProperty("zigzagStrength");
        zigzagLookahead = serializedObject.FindProperty("zigzagLookahead");
        preferHomeWhenFleeing = serializedObject.FindProperty("preferHomeWhenFleeing");
        fleeHomeStartRadius = serializedObject.FindProperty("fleeHomeStartRadius");
        fleeHomeStopRadius = serializedObject.FindProperty("fleeHomeStopRadius");

        // Passive herd
        herdPanicRadius = serializedObject.FindProperty("herdPanicRadius");
        herdJoinRadius = serializedObject.FindProperty("herdJoinRadius");
        herdCohesionWeightWhileFleeing = serializedObject.FindProperty("herdCohesionWeightWhileFleeing");
        herdSeparationWeightWhileFleeing = serializedObject.FindProperty("herdSeparationWeightWhileFleeing");
        herdTightFleeSeconds = serializedObject.FindProperty("herdTightFleeSeconds");
        herdScatterRadius = serializedObject.FindProperty("herdScatterRadius");
        herdSafeFactor = serializedObject.FindProperty("herdSafeFactor");
        herdPreferredDistance = serializedObject.FindProperty("herdPreferredDistance");
        herdRegroupSpeed = serializedObject.FindProperty("herdRegroupSpeed");

        // Charge
        windupDuration = serializedObject.FindProperty("windupDuration");
        chargeSpeed = serializedObject.FindProperty("chargeSpeed");
        chargeDuration = serializedObject.FindProperty("chargeDuration");
        chargeDetectionRange = serializedObject.FindProperty("chargeDetectionRange");
        chargeDamage = serializedObject.FindProperty("chargeDamage");
        chargeDamageRadius = serializedObject.FindProperty("chargeDamageRadius");
        maxChargeAttempts = serializedObject.FindProperty("maxChargeAttempts");
        chargeCooldownDuration = serializedObject.FindProperty("chargeCooldownDuration");

        // Retaliation
        retaliationDelay = serializedObject.FindProperty("retaliationDelay");

        // Type4
        randomizeAggressiveStats = serializedObject.FindProperty("randomizeAggressiveStats");
        speedRandomRange = serializedObject.FindProperty("speedRandomRange");
        turnRateDeg = serializedObject.FindProperty("turnRateDeg");
        turnSlowdownAngle = serializedObject.FindProperty("turnSlowdownAngle");
        turnSlowdownMinFactor = serializedObject.FindProperty("turnSlowdownMinFactor");
        leadPredictionSeconds = serializedObject.FindProperty("leadPredictionSeconds");
        leadMaxDistance = serializedObject.FindProperty("leadMaxDistance");
        playerVelSmoothing = serializedObject.FindProperty("playerVelSmoothing");
        t4Surges = serializedObject.FindProperty("t4Surges");
        t4SurgeEvery = serializedObject.FindProperty("t4SurgeEvery");
        t4SurgeMultiplier = serializedObject.FindProperty("t4SurgeMultiplier");
        t4SurgeDuration = serializedObject.FindProperty("t4SurgeDuration");
        t4FatigueMultiplier = serializedObject.FindProperty("t4FatigueMultiplier");
        t4FatigueDuration = serializedObject.FindProperty("t4FatigueDuration");
        t4SpeedSmoothing = serializedObject.FindProperty("t4SpeedSmoothing");

        // Pack
        packCallRadius = serializedObject.FindProperty("packCallRadius");
        coordinationTime = serializedObject.FindProperty("coordinationTime");
        maxPackSize = serializedObject.FindProperty("maxPackSize");
        packChaseRadius = serializedObject.FindProperty("packChaseRadius");
        packCollapseSeconds = serializedObject.FindProperty("packCollapseSeconds");
        packMinRadiusFactor = serializedObject.FindProperty("packMinRadiusFactor");
        packAttackWindowDeg = serializedObject.FindProperty("packAttackWindowDeg");
        packOrbitingEnabled = serializedObject.FindProperty("packOrbitingEnabled");
        packOrbitAngularSpeedDeg = serializedObject.FindProperty("packOrbitAngularSpeedDeg");
        packRepathInterval = serializedObject.FindProperty("packRepathInterval");
        packRearBoost = serializedObject.FindProperty("packRearBoost");
        packRearAngleWindowDeg = serializedObject.FindProperty("packRearAngleWindowDeg");
        packLungeSpeed = serializedObject.FindProperty("packLungeSpeed");
        packLungeDuration = serializedObject.FindProperty("packLungeDuration");
        packLungeOvershoot = serializedObject.FindProperty("packLungeOvershoot");
        packLungeCooldown = serializedObject.FindProperty("packLungeCooldown");

        // Jump
        jumpHeight = serializedObject.FindProperty("jumpHeight");
        jumpDuration = serializedObject.FindProperty("jumpDuration");
        jumpLandingProbeRadius = serializedObject.FindProperty("jumpLandingProbeRadius");
        chaseDuration = serializedObject.FindProperty("chaseDuration");
        jumpSpotTag = serializedObject.FindProperty("jumpSpotTag");
        jumpSpotApproachRadius = serializedObject.FindProperty("jumpSpotApproachRadius");
        perchCooldown = serializedObject.FindProperty("perchCooldown");
        jumpReturnApproachDistance = serializedObject.FindProperty("jumpReturnApproachDistance");
        jumpReturnStopDistance = serializedObject.FindProperty("jumpReturnStopDistance");
        jumpHorizontalSpeed = serializedObject.FindProperty("jumpHorizontalSpeed");
        minJumpDuration = serializedObject.FindProperty("minJumpDuration");
        maxJumpDuration = serializedObject.FindProperty("maxJumpDuration");

        // Migration
        migrationPath = serializedObject.FindProperty("migrationPath");
        migrationOwner = serializedObject.FindProperty("migrationOwner");
        migrationMoveSpeed = serializedObject.FindProperty("migrationMoveSpeed");
        migrationFollowerSpeed = serializedObject.FindProperty("migrationFollowerSpeed");
        migrationCohesionRadius = serializedObject.FindProperty("migrationCohesionRadius");
        migrationMinSeparation = serializedObject.FindProperty("migrationMinSeparation");
        migrationImpactRadius = serializedObject.FindProperty("migrationImpactRadius");
        migrationImpactDot = serializedObject.FindProperty("migrationImpactDot");

        // Companion
        companionFollowDistance = serializedObject.FindProperty("companionFollowDistance");
        companionCircleRadius = serializedObject.FindProperty("companionCircleRadius");
        companionCircleSpeed = serializedObject.FindProperty("companionCircleSpeed");
        companionRotationLerp = serializedObject.FindProperty("companionRotationLerp");
        companionJitterThresholdSq = serializedObject.FindProperty("companionJitterThresholdSq");
        idleMoveInterval = serializedObject.FindProperty("idleMoveInterval");
        idleMinRadius = serializedObject.FindProperty("idleMinRadius");
        idleMaxRadius = serializedObject.FindProperty("idleMaxRadius");
        targetTag = serializedObject.FindProperty("targetTag");
        companionDetectionRange = serializedObject.FindProperty("companionDetectionRange");
        maxChaseDistance = serializedObject.FindProperty("maxChaseDistance");
        limitTargetsToCameraView = serializedObject.FindProperty("limitTargetsToCameraView");
        screenEdgePadding = serializedObject.FindProperty("screenEdgePadding");
        companionTargetCamera = serializedObject.FindProperty("companionTargetCamera");
        flockNeighborRadius = serializedObject.FindProperty("flockNeighborRadius");
        flockSeparationWeight = serializedObject.FindProperty("flockSeparationWeight");
        flockCohesionWeight = serializedObject.FindProperty("flockCohesionWeight");
        flockAlignmentWeight = serializedObject.FindProperty("flockAlignmentWeight");

        // styles
        _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        _richStyle = new GUIStyle(EditorStyles.label) { richText = true };

        _nextDebugRefreshAt = EditorApplication.timeSinceStartup;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Top: core identity
        EditorGUILayout.PropertyField(aiType);
        EditorGUILayout.PropertyField(animalType);

        var type = (CuteAnimalAI.AIType)aiType.enumValueIndex;

        EditorGUILayout.Space(6);

        // Toggles
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _showDesignerInspector = EditorGUILayout.ToggleLeft("Show Designer Inspector (type-based)", _showDesignerInspector);
            _showRuntimeDebug = EditorGUILayout.ToggleLeft("Show Runtime Debug (Play Mode)", _showRuntimeDebug);
            _showSceneOverlay = EditorGUILayout.ToggleLeft("Show Scene Overlay (Play Mode)", _showSceneOverlay);
        }

        if (_showDesignerInspector)
        {
            DrawCoreMovementBlock();

            // Type-specific blocks
            if (IsPassive(type)) DrawFleeBlock();
            if (type == CuteAnimalAI.AIType.PassiveHerd) DrawPassiveHerdBlock();

            if (IsAggressiveFamily(type)) DrawCombatBlock();

            if (type == CuteAnimalAI.AIType.AggressiveType1) DrawChargeBlock();
            if (type == CuteAnimalAI.AIType.AggressiveType2) DrawRetaliationBlock();
            if (type == CuteAnimalAI.AIType.AggressiveType3) DrawPackBlock();
            if (type == CuteAnimalAI.AIType.AggressiveType4) DrawType4Block();
            if (type == CuteAnimalAI.AIType.AggressiveJumping) DrawJumpBlock();
            if (type == CuteAnimalAI.AIType.MigratingAi) DrawMigrationBlock();
            if (type == CuteAnimalAI.AIType.Companion) DrawCompanionBlock();
        }

        // Runtime debug (inspector)
        if (_showRuntimeDebug)
        {
            DrawRuntimeDebugInspector();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // -----------------------------
    // Scene overlay merged in here
    // -----------------------------
    void OnSceneGUI()
    {
        if (!_showSceneOverlay) return;
        if (targets == null || targets.Length != 1) return; // keep overlay clean

        var ai = target as CuteAnimalAI;
        if (!ai) return;
        if (!Application.isPlaying) return;
        if (!ai.agent) return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        Vector3 pos = ai.transform.position + Vector3.up * 2f;

        // Label style (no allocations per line)
        var style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = Color.white;

        int line = 0;
        void Line(string label, string value, Color c)
        {
            style.normal.textColor = c;
            Handles.Label(pos + Vector3.up * (line * 0.25f), $"{label}: {value}", style);
            line++;
        }

        string stateName = ai.StateMachine?.CurrentState?.GetType().Name ?? "None";
        Line("AI State", stateName, Color.white);
        Line("AI Type", ai.aiType.ToString(), Color.cyan);

        if (ai.player)
        {
            float dist = Vector3.Distance(ai.transform.position, ai.player.position);
            Line("Player Distance", dist.ToString("F1") + " units", (dist < ai.detectionRange) ? Color.red : Color.gray);

            // Threat line for aggressives
            if (ai.aiType.ToString().ToLower().Contains("aggressive"))
            {
                Handles.color = Color.red;
                Handles.DrawDottedLine(ai.transform.position, ai.player.position, 2f);
            }
        }

        // Agent info
        Line("Is On NavMesh", ai.agent.isOnNavMesh.ToString(), ai.agent.isOnNavMesh ? Color.green : Color.red);
        Line("Is Stopped", ai.agent.isStopped.ToString(), ai.agent.isStopped ? Color.yellow : Color.green);
        Line("Path Pending", ai.agent.pathPending.ToString(), ai.agent.pathPending ? Color.yellow : Color.white);
        Line("Remaining Dist", ai.agent.remainingDistance.ToString("F1"), Color.white);
        Line("Stopping Dist", ai.agent.stoppingDistance.ToString("F1"), Color.white);

        // Destination line
        if (ai.agent.hasPath)
        {
            Handles.color = Color.green;
            Handles.DrawLine(ai.transform.position + Vector3.up * 0.2f, ai.agent.destination);
            Handles.SphereHandleCap(0, ai.agent.destination, Quaternion.identity, 0.3f, EventType.Repaint);
            Line("Destination", ai.agent.destination.ToString(), Color.green);
        }
    }

    // -----------------------------
    // Debug (Inspector) merged in here
    // -----------------------------
    void DrawRuntimeDebugInspector()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("🐾 Runtime Debug", _headerStyle);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view runtime debug.", MessageType.Info);
                return;
            }

            if (targets == null || targets.Length != 1)
            {
                EditorGUILayout.HelpBox("Select a single AI to view runtime debug.", MessageType.Info);
                return;
            }

            var ai = target as CuteAnimalAI;
            if (!ai)
            {
                EditorGUILayout.HelpBox("Target missing.", MessageType.Warning);
                return;
            }

            if (!ai.agent)
            {
                EditorGUILayout.HelpBox("NavMeshAgent missing on this AI.", MessageType.Warning);
                return;
            }

            // Throttle heavy refresh
            double now = EditorApplication.timeSinceStartup;
            if (now >= _nextDebugRefreshAt)
            {
                _nextDebugRefreshAt = now + DebugRefreshInterval;

                _cachedStateName = ai.StateMachine?.CurrentState?.GetType().Name ?? "None";

                // Only do threat scan if you really want it (and only for aggressives)
                _cachedThreatName = "None";
                _cachedThreatDist = -1f;

                bool aggressive = ai.aiType.ToString().ToLower().Contains("aggressive");
                if (aggressive)
                {
                    // This can be expensive depending on your implementation — throttled above.
                    Transform t = ai.GetClosestThreatForCombat(ai.detectionRange);
                    if (t)
                    {
                        _cachedThreatName = t.name;
                        _cachedThreatDist = Vector3.Distance(ai.transform.position, t.position);
                    }
                }

                // keep inspector fresh without spam
                Repaint();
            }

            EditorGUILayout.LabelField("Current State:", $"<b>{_cachedStateName}</b>", _richStyle);
            EditorGUILayout.LabelField("AI Type:", ai.aiType.ToString(), _richStyle);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Threat Target:", $"<color=orange>{_cachedThreatName}</color>", _richStyle);
            if (_cachedThreatDist >= 0f)
                EditorGUILayout.LabelField("Threat Distance:", $"{_cachedThreatDist:F1} units", _richStyle);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);

            BoolLine("Provoked", ai.wasProvoked);
            BoolLine("Territorial(Agg)", ai.territorialAggressive);
            BoolLine("Is Migrating", ai.isMigratingMoving);
            BoolLine("Charge Cooldown", ai.isChargeCooldownActive);
            BoolLine("Jump Session Active", ai.jumpSessionActive);
            BoolLine("Perch Ready", Time.time >= ai.nextPerchEngageTime);

            if (ai.isChargeCooldownActive)
                EditorGUILayout.LabelField("Charge CD Left:", $"{ai.chargeCooldownTimer:F1}s", _richStyle);

            if (ai.jumpSessionActive)
                EditorGUILayout.LabelField("Jump Session Ends In:", $"{ai.jumpSessionDeadline - Time.time:F1}s", _richStyle);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("NavMesh", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("On NavMesh:", ai.agent.isOnNavMesh.ToString(), _richStyle);
            EditorGUILayout.LabelField("Path Pending:", ai.agent.pathPending.ToString(), _richStyle);
            EditorGUILayout.LabelField("Remaining Dist:", $"{ai.agent.remainingDistance:F1}", _richStyle);
        }
    }

    void BoolLine(string label, bool value)
    {
        string val = value ? "<color=green>Yes</color>" : "<color=gray>No</color>";
        EditorGUILayout.LabelField(label + ":", val, _richStyle);
    }

    // -----------------------------
    // Blocks
    // -----------------------------
    void DrawCoreMovementBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Core", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(homeRadius);
        EditorGUILayout.PropertyField(enableAutoBraking);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(wanderSpeed);
        EditorGUILayout.PropertyField(chaseSpeed);
        EditorGUILayout.PropertyField(fleeSpeed);
        EditorGUILayout.PropertyField(rotationSpeed);
        EditorGUILayout.PropertyField(stopDistance);
        EditorGUILayout.PropertyField(wanderRadius);
        EditorGUILayout.PropertyField(wanderInterval);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Flocking / Spacing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableFlocking);
        EditorGUILayout.PropertyField(repulsionDistance);
        EditorGUILayout.PropertyField(repulsionStrength);
        EditorGUILayout.PropertyField(alignmentWeight);
        EditorGUILayout.PropertyField(cohesionWeight);
    }

    void DrawCombatBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(detectionRange);
        EditorGUILayout.PropertyField(attackRange);
        EditorGUILayout.PropertyField(attackDamage);
        EditorGUILayout.PropertyField(attackCooldown);
        EditorGUILayout.PropertyField(attackDuration);
        EditorGUILayout.PropertyField(facingConeToAttack);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Territory (Aggressive)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(territorialAggressive);
        if (territorialAggressive.boolValue)
            EditorGUILayout.PropertyField(aggressiveTerritoryRadius);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Pounce", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(canPounce);
        if (canPounce.boolValue)
        {
            EditorGUILayout.PropertyField(pounceCooldown);
            EditorGUILayout.PropertyField(pounceHeight);
            EditorGUILayout.PropertyField(pounceForwardDistance);
            EditorGUILayout.PropertyField(fleePounceTriggerDistance);
        }
    }

    void DrawFleeBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Flee", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fleeRange);
        EditorGUILayout.PropertyField(fleeAttempts);
        EditorGUILayout.PropertyField(navMeshEdgeThreshold);
        EditorGUILayout.PropertyField(playerPredictionTime);
        EditorGUILayout.PropertyField(playerForwardPredictionDistance);
        EditorGUILayout.PropertyField(fleeBurstMultiplier);
        EditorGUILayout.PropertyField(fleeBurstDuration);
        EditorGUILayout.PropertyField(zigzagSpeed);
        EditorGUILayout.PropertyField(zigzagStrength);
        EditorGUILayout.PropertyField(zigzagLookahead);

        EditorGUILayout.PropertyField(preferHomeWhenFleeing);
        if (preferHomeWhenFleeing.boolValue)
        {
            EditorGUILayout.PropertyField(fleeHomeStartRadius);
            EditorGUILayout.PropertyField(fleeHomeStopRadius);
        }
    }

    void DrawPassiveHerdBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Passive Herd", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(herdPanicRadius);
        EditorGUILayout.PropertyField(herdJoinRadius);
        EditorGUILayout.PropertyField(herdCohesionWeightWhileFleeing);
        EditorGUILayout.PropertyField(herdSeparationWeightWhileFleeing);
        EditorGUILayout.PropertyField(herdTightFleeSeconds);
        EditorGUILayout.PropertyField(herdScatterRadius);
        EditorGUILayout.PropertyField(herdSafeFactor);
        EditorGUILayout.PropertyField(herdPreferredDistance);
        EditorGUILayout.PropertyField(herdRegroupSpeed);
    }

    void DrawChargeBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Charge (Type1)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(windupDuration);
        EditorGUILayout.PropertyField(chargeSpeed);
        EditorGUILayout.PropertyField(chargeDuration);
        EditorGUILayout.PropertyField(chargeDetectionRange);
        EditorGUILayout.PropertyField(chargeDamage);
        EditorGUILayout.PropertyField(chargeDamageRadius);
        EditorGUILayout.PropertyField(maxChargeAttempts);
        EditorGUILayout.PropertyField(chargeCooldownDuration);
    }

    void DrawRetaliationBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Retaliation (Type2)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(retaliationDelay);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Territory (Type2)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(territorial);
        if (territorial.boolValue)
            EditorGUILayout.PropertyField(territoryRadius);
    }

    void DrawType4Block()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Aggressive Type4", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(randomizeAggressiveStats);
        EditorGUILayout.PropertyField(speedRandomRange);
        EditorGUILayout.PropertyField(turnRateDeg);
        EditorGUILayout.PropertyField(turnSlowdownAngle);
        EditorGUILayout.PropertyField(turnSlowdownMinFactor);
        EditorGUILayout.PropertyField(leadPredictionSeconds);
        EditorGUILayout.PropertyField(leadMaxDistance);
        EditorGUILayout.PropertyField(playerVelSmoothing);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Surges", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(t4Surges);
        if (t4Surges.boolValue)
        {
            EditorGUILayout.PropertyField(t4SurgeEvery);
            EditorGUILayout.PropertyField(t4SurgeMultiplier);
            EditorGUILayout.PropertyField(t4SurgeDuration);
            EditorGUILayout.PropertyField(t4FatigueMultiplier);
            EditorGUILayout.PropertyField(t4FatigueDuration);
            EditorGUILayout.PropertyField(t4SpeedSmoothing);
        }
    }

    void DrawPackBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Pack Hunting (Type3)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(packCallRadius);
        EditorGUILayout.PropertyField(coordinationTime);
        EditorGUILayout.PropertyField(maxPackSize);
        EditorGUILayout.PropertyField(packChaseRadius);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Collapse & Commit", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(packCollapseSeconds);
        EditorGUILayout.PropertyField(packMinRadiusFactor);
        EditorGUILayout.PropertyField(packAttackWindowDeg);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Orbiting", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(packOrbitingEnabled);
        if (packOrbitingEnabled.boolValue)
        {
            EditorGUILayout.PropertyField(packOrbitAngularSpeedDeg);
            EditorGUILayout.PropertyField(packRepathInterval);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Rear Boost", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(packRearBoost);
        EditorGUILayout.PropertyField(packRearAngleWindowDeg);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Lunge", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(packLungeSpeed);
        EditorGUILayout.PropertyField(packLungeDuration);
        EditorGUILayout.PropertyField(packLungeOvershoot);
        EditorGUILayout.PropertyField(packLungeCooldown);
    }

    void DrawJumpBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Jumping", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(jumpHeight);
        EditorGUILayout.PropertyField(jumpDuration);
        EditorGUILayout.PropertyField(jumpLandingProbeRadius);
        EditorGUILayout.PropertyField(chaseDuration);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(jumpHorizontalSpeed);
        EditorGUILayout.PropertyField(minJumpDuration);
        EditorGUILayout.PropertyField(maxJumpDuration);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Perch", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(jumpSpotTag);
        EditorGUILayout.PropertyField(jumpSpotApproachRadius);
        EditorGUILayout.PropertyField(perchCooldown);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Return", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(jumpReturnApproachDistance);
        EditorGUILayout.PropertyField(jumpReturnStopDistance);
    }

    void DrawMigrationBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Migration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(migrationPath);
        EditorGUILayout.PropertyField(migrationOwner);
        EditorGUILayout.PropertyField(migrationMoveSpeed);
        EditorGUILayout.PropertyField(migrationFollowerSpeed);
        EditorGUILayout.PropertyField(migrationCohesionRadius);
        EditorGUILayout.PropertyField(migrationMinSeparation);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Impact", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(migrationImpactRadius);
        EditorGUILayout.PropertyField(migrationImpactDot);
    }

    void DrawCompanionBlock()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Companion", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(companionFollowDistance);
        EditorGUILayout.PropertyField(companionCircleRadius);
        EditorGUILayout.PropertyField(companionCircleSpeed);
        EditorGUILayout.PropertyField(companionRotationLerp);
        EditorGUILayout.PropertyField(companionJitterThresholdSq);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Idle Moves", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idleMoveInterval);
        EditorGUILayout.PropertyField(idleMinRadius);
        EditorGUILayout.PropertyField(idleMaxRadius);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetTag);
        EditorGUILayout.PropertyField(companionDetectionRange);
        EditorGUILayout.PropertyField(maxChaseDistance);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Screen Gate", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(limitTargetsToCameraView);
        if (limitTargetsToCameraView.boolValue)
        {
            EditorGUILayout.PropertyField(screenEdgePadding);
            EditorGUILayout.PropertyField(companionTargetCamera);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Companion Flocking", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flockNeighborRadius);
        EditorGUILayout.PropertyField(flockSeparationWeight);
        EditorGUILayout.PropertyField(flockCohesionWeight);
        EditorGUILayout.PropertyField(flockAlignmentWeight);
    }

    // -----------------------------
    // Helpers
    // -----------------------------
    static bool IsPassive(CuteAnimalAI.AIType t)
    {
        return t == CuteAnimalAI.AIType.Passive
            || t == CuteAnimalAI.AIType.PassiveEasy
            || t == CuteAnimalAI.AIType.PassiveVeryEasy
            || t == CuteAnimalAI.AIType.PassiveHerd;
    }

    static bool IsAggressiveFamily(CuteAnimalAI.AIType t)
    {
        return t == CuteAnimalAI.AIType.Aggressive
            || t == CuteAnimalAI.AIType.AggressiveType1
            || t == CuteAnimalAI.AIType.AggressiveType2
            || t == CuteAnimalAI.AIType.AggressiveType3
            || t == CuteAnimalAI.AIType.AggressiveType4
            || t == CuteAnimalAI.AIType.AggressiveJumping;
    }
}
#endif

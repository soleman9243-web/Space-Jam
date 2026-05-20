using UnityEngine;
using SpaceJam.Minigames;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceJam.Minigames
{
    [ExecuteAlways]
    public class TVStealthBuilder : MonoBehaviour
    {
        [Header("Television Settings")]
        [Tooltip("The sweep rotation speed of the vision cone.")]
        public float sweepSpeed = 2f;

        [Tooltip("The maximum angle of the sweep (left and right of base direction).")]
        public float maxSweepAngle = 35f;

        [Tooltip("The direction the TV light points initially: 0 = Up, 90 = Right, 180 = Down, 270 = Left.")]
        public float baseOrientationAngle = 180f;

        [Header("Stealth Gameplay & Difficulty")]
        [Tooltip("Difficulty preset for the TV quest. Easy=15 stability damage, Normal=35, Hard=60, Extreme=95. Select Custom to set your own value below!")]
        public TVDifficultyPreset difficulty = TVDifficultyPreset.Normal;

        [Tooltip("How much stability the player loses when caught in the TV light cone (Used directly if Custom is selected).")]
        public float stabilityPenalty = 35f;

        [Tooltip("The range/length of the triangular vision cone.")]
        public float visionRange = 5.5f;

        [Tooltip("The field of view (angle) of the vision cone triangle.")]
        public float visionFovAngle = 40f;

        [Header("Visual & Rendering Settings")]
        [Tooltip("The Sorting Layer name for the vision cone. Change this if the cone is covered by the floor!")]
        public string coneSortingLayerName = "Default";

        [Tooltip("The Sorting Order for the vision cone. Increase this (e.g. 5, 10) if the floor is rendering on top of the light cone!")]
        public int coneSortingOrder = 5;

        [ContextMenu("Build TV Stealth Quest Automatically")]
        public void BuildTVQuest()
        {
            Debug.Log("[TVStealthBuilder] Constructing TV Stealth Quest...");

            #if UNITY_EDITOR
            // 1. Create Parent GameObject or use current if attached
            GameObject questParent = gameObject;
            if (questParent.name.Contains("ArenaBuilder") || questParent.name.Contains("Manager"))
            {
                questParent = new GameObject("TVStealthQuest");
                questParent.transform.position = transform.position;
            }
            else
            {
                questParent.name = "TVStealthQuest";
            }

            // 2. Attach TVStealthTask script
            TVStealthTask tvTask = questParent.GetComponent<TVStealthTask>();
            if (tvTask == null)
            {
                tvTask = questParent.AddComponent<TVStealthTask>();
            }
            tvTask.taskText = "Turn off the TV (Press F)";

            // 3. Create TV Screen ON Visual
            GameObject tvOn = GameObject.Find("TV_Visual_ON");
            if (tvOn != null && tvOn.transform.parent == questParent.transform)
            {
                DestroyImmediate(tvOn);
            }
            tvOn = new GameObject("TV_Visual_ON");
            tvOn.transform.SetParent(questParent.transform);
            tvOn.transform.localPosition = Vector3.zero;

            SpriteRenderer onSR = tvOn.AddComponent<SpriteRenderer>();
            onSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            onSR.color = new Color(0.2f, 0.7f, 1f, 1f); // Glowing light blue screen
            onSR.drawMode = SpriteDrawMode.Sliced;
            onSR.size = new Vector2(1.2f, 0.9f);
            onSR.sortingOrder = 1;

            // Add TV body/frame visual
            GameObject tvBody = new GameObject("TV_Frame");
            tvBody.transform.SetParent(tvOn.transform);
            tvBody.transform.localPosition = Vector3.zero;
            SpriteRenderer bodySR = tvBody.AddComponent<SpriteRenderer>();
            bodySR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bodySR.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark plastic body
            bodySR.drawMode = SpriteDrawMode.Sliced;
            bodySR.size = new Vector2(1.4f, 1.1f);
            bodySR.sortingOrder = 0;

            // 4. Create TV Screen OFF Visual
            GameObject tvOff = GameObject.Find("TV_Visual_OFF");
            if (tvOff != null && tvOff.transform.parent == questParent.transform)
            {
                DestroyImmediate(tvOff);
            }
            tvOff = new GameObject("TV_Visual_OFF");
            tvOff.transform.SetParent(questParent.transform);
            tvOff.transform.localPosition = Vector3.zero;

            SpriteRenderer offSR = tvOff.AddComponent<SpriteRenderer>();
            offSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            offSR.color = new Color(0.08f, 0.08f, 0.08f, 1f); // Dead screen
            offSR.drawMode = SpriteDrawMode.Sliced;
            offSR.size = new Vector2(1.2f, 0.9f);
            offSR.sortingOrder = 1;

            GameObject tvBodyOff = new GameObject("TV_Frame_Off");
            tvBodyOff.transform.SetParent(tvOff.transform);
            tvBodyOff.transform.localPosition = Vector3.zero;
            SpriteRenderer bodySR2 = tvBodyOff.AddComponent<SpriteRenderer>();
            bodySR2.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bodySR2.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            bodySR2.drawMode = SpriteDrawMode.Sliced;
            bodySR2.size = new Vector2(1.4f, 1.1f);
            bodySR2.sortingOrder = 0;

            // Hide off visual by default
            tvOff.SetActive(false);

            // 5. Create Circle Collider Trigger for interaction on Parent
            CircleCollider2D interactCol = questParent.GetComponent<CircleCollider2D>();
            if (interactCol == null)
            {
                interactCol = questParent.AddComponent<CircleCollider2D>();
            }
            interactCol.radius = 2.2f; // Increased to 2.2f so player can easily turn it off
            interactCol.isTrigger = true;

            // Add solid BoxCollider2D so player cannot walk through the TV
            BoxCollider2D solidCol = questParent.GetComponent<BoxCollider2D>();
            if (solidCol == null)
            {
                solidCol = questParent.AddComponent<BoxCollider2D>();
            }
            solidCol.isTrigger = false; // SOLID - blocks player
            solidCol.size = new Vector2(1.4f, 1.1f);
            solidCol.offset = Vector2.zero;

            // Add standard InteractObject2D component to support standard interaction bubbles
            InteractObject2D interactObj = questParent.GetComponent<InteractObject2D>();
            if (interactObj == null)
            {
                interactObj = questParent.AddComponent<InteractObject2D>();
            }
            interactObj.requireActiveTask = true;
            interactObj.linkedTask = tvTask;

            // Clear existing persistent listeners to avoid duplicate calls
            while (interactObj.onInteract.GetPersistentEventCount() > 0)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(interactObj.onInteract, 0);
            }
            // Bind TurnOffTV method to standard interaction event
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(interactObj.onInteract, tvTask.TurnOffTV);

            // 6. Create Rotating Vision Cone Child
            GameObject visionCone = GameObject.Find("VisionCone");
            if (visionCone != null && visionCone.transform.parent == questParent.transform)
            {
                DestroyImmediate(visionCone);
            }
            visionCone = new GameObject("VisionCone");
            visionCone.transform.SetParent(questParent.transform);
            visionCone.transform.localPosition = Vector3.zero;
            visionCone.transform.localRotation = Quaternion.Euler(0f, 0f, baseOrientationAngle);

            // Add the Vision Cone script to listen to collisions
            TVVisionCone tvConeScript = visionCone.AddComponent<TVVisionCone>();
            tvConeScript.Initialize(tvTask);

            // Serialize TVVisionCone's parentTask reference so it persists in the scene
            SerializedObject coneSO = new SerializedObject(tvConeScript);
            coneSO.FindProperty("parentTask").objectReferenceValue = tvTask;
            coneSO.ApplyModifiedProperties();

            // 7. Create Safe Spawn Point Offset (Safe Zone where player teleport when caught)
            GameObject safeSpawn = GameObject.Find("TV_SafeSpawnPoint");
            if (safeSpawn != null && safeSpawn.transform.parent == questParent.transform)
            {
                // Retain it but keep safe
            }
            else
            {
                safeSpawn = new GameObject("TV_SafeSpawnPoint");
                safeSpawn.transform.SetParent(questParent.transform);
                safeSpawn.transform.localPosition = new Vector3(-3f, -2f, 0f); // Default safe offset
            }

            // 8. Configure script references in SerializedObject to save instantly
            SerializedObject so = new SerializedObject(tvTask);
            so.FindProperty("sweepSpeed").floatValue = sweepSpeed;
            so.FindProperty("maxSweepAngle").floatValue = maxSweepAngle;
            so.FindProperty("baseAngle").floatValue = baseOrientationAngle;
            so.FindProperty("visionRange").floatValue = visionRange;
            so.FindProperty("visionFovAngle").floatValue = visionFovAngle;
            so.FindProperty("difficulty").enumValueIndex = (int)difficulty;
            so.FindProperty("stabilityPenalty").floatValue = stabilityPenalty;
            so.FindProperty("coneSortingLayerName").stringValue = coneSortingLayerName;
            so.FindProperty("coneSortingOrder").intValue = coneSortingOrder;
            so.FindProperty("safeSpawnPoint").objectReferenceValue = safeSpawn.transform;
            so.FindProperty("visionConeObject").objectReferenceValue = visionCone;
            so.FindProperty("tvOnVisual").objectReferenceValue = tvOn;
            so.FindProperty("tvOffVisual").objectReferenceValue = tvOff;

            so.ApplyModifiedProperties();

            // 9. Generate the geometry immediately in editor
            tvTask.GenerateVisionConeGeometry();
            
            // Force evaluation in editor
            tvTask.ApplyDifficultyPreset();
            tvTask.ApplyRenderingSettings();

            EditorUtility.SetDirty(tvTask);
            EditorUtility.SetDirty(interactObj);
            EditorUtility.SetDirty(tvConeScript);

            Debug.Log($"[TVStealthBuilder] TV Stealth Quest built successfully! Parent: {questParent.name}, VisionRange: {visionRange}, FOV: {visionFovAngle}, Initial Base Angle: {baseOrientationAngle}");
            #else
            Debug.LogWarning("[TVStealthBuilder] Building can only be run inside the Unity Editor!");
            #endif
        }
    }
}

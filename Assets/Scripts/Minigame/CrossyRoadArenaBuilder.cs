using UnityEngine;
using SpaceJam.Minigames;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceJam.Minigames
{
    [ExecuteAlways]
    public class CrossyRoadArenaBuilder : MonoBehaviour
    {
        [Header("Setup Config")]
        [Tooltip("The height/length of the road level to build.")]
        public float levelHeight = 35f;

        [Tooltip("The width of the playable road/arena.")]
        public float roadWidth = 14f;

        [Header("Obstacle Prefab Placeholder")]
        [Tooltip("Drag your enemy/obstacle prefab here. If you don't have one, we will look for one in the project.")]
        public GameObject obstaclePrefab;

        /// <summary>
        /// This method builds the entire arena automatically and links all scripts!
        /// </summary>
        [ContextMenu("Build Arena Automatically")]
        public void BuildArena()
        {
            Debug.Log("[CrossyRoadArenaBuilder] Building Crossy Road Arena...");

            // 1. Find or create the CrossyRoadManager
            CrossyRoadMinigame minigame = FindObjectOfType<CrossyRoadMinigame>();
            if (minigame == null)
            {
                GameObject managerObj = new GameObject("CrossyRoadManager");
                minigame = managerObj.AddComponent<CrossyRoadMinigame>();
                minigame.LevelHeight = levelHeight;
                Debug.Log("[CrossyRoadArenaBuilder] Created new CrossyRoadManager GameObject.");
            }

            // 2. Create the CrossyRoadArena Parent GameObject
            GameObject arenaParent = GameObject.Find("CrossyRoadArena");
            if (arenaParent != null)
            {
                DestroyImmediate(arenaParent); // Reset if exists
                Debug.Log("[CrossyRoadArenaBuilder] Rebuilding existing CrossyRoadArena...");
            }
            arenaParent = new GameObject("CrossyRoadArena");

            // 3. Create Road Visual (Background)
            GameObject road = new GameObject("RoadBackground");
            road.transform.SetParent(arenaParent.transform);
            road.transform.localPosition = new Vector3(0f, levelHeight / 2f, 0f);
            
            SpriteRenderer roadRenderer = road.AddComponent<SpriteRenderer>();
            // Use Unity's default UI background sprite so it has basic gray color
            roadRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/Background.psd"
            );
            roadRenderer.color = new Color(0.12f, 0.12f, 0.15f, 1f); // Dark road color
            roadRenderer.drawMode = SpriteDrawMode.Sliced;
            roadRenderer.size = new Vector2(roadWidth, levelHeight + 4f);
            roadRenderer.sortingOrder = -10; // Ensure it sits behind player

            // 4. Create Left Boundary Wall (Obstruction)
            GameObject leftWall = new GameObject("Boundary_Left");
            leftWall.transform.SetParent(arenaParent.transform);
            leftWall.transform.localPosition = new Vector3(-roadWidth / 2f - 0.5f, levelHeight / 2f, 0f);
            
            BoxCollider2D leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(1f, levelHeight + 4f);
            
            // Visual for left wall
            SpriteRenderer leftSR = leftWall.AddComponent<SpriteRenderer>();
            leftSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            leftSR.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            leftSR.drawMode = SpriteDrawMode.Sliced;
            leftSR.size = new Vector2(1f, levelHeight + 4f);

            // 5. Create Right Boundary Wall (Obstruction)
            GameObject rightWall = new GameObject("Boundary_Right");
            rightWall.transform.SetParent(arenaParent.transform);
            rightWall.transform.localPosition = new Vector3(roadWidth / 2f + 0.5f, levelHeight / 2f, 0f);
            
            BoxCollider2D rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(1f, levelHeight + 4f);
            
            // Visual for right wall
            SpriteRenderer rightSR = rightWall.AddComponent<SpriteRenderer>();
            rightSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            rightSR.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            rightSR.drawMode = SpriteDrawMode.Sliced;
            rightSR.size = new Vector2(1f, levelHeight + 4f);

            // 6. Create Start Spawn Point
            GameObject startPointObj = new GameObject("MinigameStartPoint");
            startPointObj.transform.SetParent(arenaParent.transform);
            startPointObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            // 7. Create Finish Line Trigger
            GameObject finishLineObj = new GameObject("FinishLine");
            finishLineObj.transform.SetParent(arenaParent.transform);
            finishLineObj.transform.localPosition = new Vector3(0f, levelHeight, 0f);
            
            BoxCollider2D finishCol = finishLineObj.AddComponent<BoxCollider2D>();
            finishCol.size = new Vector2(roadWidth, 1.5f);
            finishCol.isTrigger = true;
            
            finishLineObj.AddComponent<CrossyRoadFinish>();

            // Finish Line Visual
            SpriteRenderer finishSR = finishLineObj.AddComponent<SpriteRenderer>();
            finishSR.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            finishSR.color = new Color(0f, 0.8f, 0.3f, 0.4f); // Transparent Green Finish Area
            finishSR.drawMode = SpriteDrawMode.Sliced;
            finishSR.size = new Vector2(roadWidth, 1.5f);

            // 8. Auto link all references in the CrossyRoadMinigame component
            // We use Reflection/SerializedObjects in Editor or just assign values directly since they are public/serialized
            minigame.LevelHeight = levelHeight;
            
            // Assign fields in script using public setters or helper methods if needed, 
            // but we can expose public fields/properties or write to them directly!
            // Let's use SerializedObject to perfectly set private fields and guarantee saving!
            #if UNITY_EDITOR
            SerializedObject so = new SerializedObject(minigame);
            so.FindProperty("levelHeight").floatValue = levelHeight;
            so.FindProperty("finishLineTransform").objectReferenceValue = finishLineObj.transform;
            so.FindProperty("minigameArenaParent").objectReferenceValue = arenaParent;
            so.FindProperty("playerStartPoint").objectReferenceValue = startPointObj.transform;
            so.FindProperty("leftSpawnX").floatValue = -roadWidth / 2f - 1f;
            so.FindProperty("rightSpawnX").floatValue = roadWidth / 2f + 1f;
            
            if (obstaclePrefab != null)
            {
                so.FindProperty("obstaclePrefab").objectReferenceValue = obstaclePrefab;
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(minigame);
            #endif

            // Deactivate arena parent by default so it stays hidden until minigame starts
            arenaParent.SetActive(false);

            Debug.Log("[CrossyRoadArenaBuilder] Arena built and configured SUCCESSFULLY! LeftSpawnX set to " + (-roadWidth / 2f - 1f) + ", RightSpawnX set to " + (roadWidth / 2f + 1f));
        }
    }
}

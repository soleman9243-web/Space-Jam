using UnityEngine;
using SpaceJam.Minigames;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceJam.Minigames
{
    [ExecuteAlways]
    public class CandlePuzzleBuilder : MonoBehaviour
    {
        [Header("Puzzle Settings")]
        [Tooltip("Jumlah lilin yang muncul melingkari player.")]
        public int candleCount = 5;

        [Tooltip("Jarak radius lingkaran lilin dari player.")]
        public float circleRadius = 3f;

        [Tooltip("Radius interaksi setiap lilin.")]
        public float interactRadius = 1.2f;

        [Header("Visual Settings")]
        [Tooltip("Sorting Order untuk visual lilin.")]
        public int sortingOrder = 5;

        [ContextMenu("Build Candle Puzzle Quest Automatically")]
        public void BuildCandlePuzzle()
        {
            Debug.Log("[CandlePuzzleBuilder] Constructing Candle Puzzle Quest...");

            #if UNITY_EDITOR
            // 1. Setup parent GameObject
            GameObject questParent = gameObject;
            questParent.name = "CandlePuzzleQuest";

            // 2. Add CandlePuzzleTask
            CandlePuzzleTask puzzleTask = questParent.GetComponent<CandlePuzzleTask>();
            if (puzzleTask == null)
            {
                puzzleTask = questParent.AddComponent<CandlePuzzleTask>();
            }
            puzzleTask.taskText = $"Nyalakan {candleCount} lilin dengan urutan yang benar!";

            // 3. Configure via SerializedObject
            SerializedObject so = new SerializedObject(puzzleTask);
            so.FindProperty("candleCount").intValue = candleCount;
            so.FindProperty("circleRadius").floatValue = circleRadius;
            so.FindProperty("interactRadius").floatValue = interactRadius;
            so.FindProperty("sortingOrder").intValue = sortingOrder;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(puzzleTask);

            Debug.Log($"[CandlePuzzleBuilder] Done! {candleCount} lilin akan muncul melingkari player saat quest aktif. Radius: {circleRadius}");
            Debug.Log("[CandlePuzzleBuilder] Tekan Play, lalu klik kanan CandlePuzzleTask > 'Force Activate Candle Puzzle' untuk test!");
            #else
            Debug.LogWarning("[CandlePuzzleBuilder] Building can only be done in the Unity Editor!");
            #endif
        }
    }
}

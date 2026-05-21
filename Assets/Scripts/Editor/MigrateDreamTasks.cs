using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class MigrateDreamTasks : EditorWindow
{
    [MenuItem("Tools/Space-Jam/1-Click Setup: Merge Semua Task ke SampleScene")]
    public static void MergeTasks()
    {
        // Pastikan kita ada di SampleScene
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Equals("SampleScene"))
        {
            Debug.LogError("[MigrateDreamTasks] Harap buka 'SampleScene' terlebih dahulu sebelum menekan tombol ini!");
            return;
        }

        string scene2Path = "Assets/CODINGAN/Scenes/SampleScene 2.unity";
        
        // Cek apakah scene 2 ada
        if (System.IO.File.Exists(scene2Path))
        {
            // Buka SampleScene 2 secara Additive (ditumpuk sementara)
            Scene scene2 = EditorSceneManager.OpenScene(scene2Path, OpenSceneMode.Additive);
            
            // Buat parent penampung di SampleScene
            GameObject taskContainer = GameObject.Find("[DREAM PHASE TASKS]");
            if (taskContainer == null)
            {
                taskContainer = new GameObject("[DREAM PHASE TASKS]");
                SceneManager.MoveGameObjectToScene(taskContainer, activeScene);
            }

            // Pindahkan semua root object dari SampleScene 2 yang berhubungan dengan task
            GameObject[] rootObjects = scene2.GetRootGameObjects();
            int movedCount = 0;

            foreach (GameObject root in rootObjects)
            {
                // Jangan pindahkan hal-hal dasar seperti Main Camera, Directional Light, EventSystem
                if (root.name.Contains("Camera") || root.name.Contains("Light") || root.name.Contains("EventSystem"))
                    continue;

                // Cek apakah object ini mengandung BaseTask (Candle, TV Stealth) atau LaptopQTE
                bool isTask = root.GetComponentInChildren<BaseTask>(true) != null || 
                              root.GetComponentInChildren<LaptopQTE>(true) != null ||
                              root.name.ToLower().Contains("tv") ||
                              root.name.ToLower().Contains("candle") ||
                              root.name.ToLower().Contains("laptop") ||
                              root.name.ToLower().Contains("enemy") ||
                              root.name.ToLower().Contains("quest");

                if (isTask)
                {
                    // Pindahkan ke SampleScene
                    SceneManager.MoveGameObjectToScene(root, activeScene);
                    root.transform.SetParent(taskContainer.transform);
                    movedCount++;
                    Debug.Log("[MigrateDreamTasks] Berhasil memindahkan: " + root.name);
                }
            }

            // Tutup SampleScene 2 tanpa di-save
            EditorSceneManager.CloseScene(scene2, true);

            // Simpan SampleScene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"[MigrateDreamTasks] SUKSES! {movedCount} Object Task (TV, Lilin, Laptop) berhasil digabungkan ke SampleScene!");
        }
        else
        {
            Debug.LogError("[MigrateDreamTasks] SampleScene 2 tidak ditemukan di path: " + scene2Path);
        }
    }
}

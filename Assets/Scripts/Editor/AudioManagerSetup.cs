using UnityEngine;
using UnityEditor;

public class AudioManagerSetup
{
    [MenuItem("Tools/Space-Jam/Setup Audio Manager")]
    public static void Setup()
    {
        AudioManager existingManager = Object.FindObjectOfType<AudioManager>();
        if (existingManager != null)
        {
            if (!EditorUtility.DisplayDialog("Setup Audio Manager", 
                "AudioManager sudah ada di scene. Hapus dan buat ulang?", "Ya", "Batal"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existingManager.gameObject);
        }

        GameObject audioGO = new GameObject("AudioManager");
        AudioManager manager = audioGO.AddComponent<AudioManager>();

        // Create Audio Sources
        AudioSource bgmSource = audioGO.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        
        AudioSource sfxSource = audioGO.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        AudioSource footstepSource = audioGO.AddComponent<AudioSource>();
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;

        // Wire to Manager
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("bgmSource").objectReferenceValue = bgmSource;
        so.FindProperty("sfxSource").objectReferenceValue = sfxSource;
        so.FindProperty("footstepSource").objectReferenceValue = footstepSource;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(audioGO, "Create Audio Manager");
        Selection.activeGameObject = audioGO;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Setup Selesai", "AudioManager berhasil dibuat!\n\nSilakan lihat Hierarchy (AudioManager) dan masukkan AudioClip MP3/WAV ke slot yang tersedia.", "Mantap");
    }
}

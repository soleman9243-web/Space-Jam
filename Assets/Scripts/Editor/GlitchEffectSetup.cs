using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlitchEffectSetup
{
    [MenuItem("Tools/Space-Jam/Setup Glitch Effect")]
    public static void Setup()
    {
        // 1. Bersihkan versi lama jika ada
        DistortionEffectController existingController = Object.FindObjectOfType<DistortionEffectController>();
        if (existingController != null)
        {
            if (!EditorUtility.DisplayDialog("Setup Glitch Effect", 
                "DistortionEffectController sudah ada di scene. Hapus dan buat ulang?", "Ya", "Batal"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existingController.gameObject);
        }

        // 2. Buat GameObject baru
        GameObject glitchGO = new GameObject("DistortionEffectController");
        DistortionEffectController controller = glitchGO.AddComponent<DistortionEffectController>();
        
        // 3. Cari PhaseLoopManager
        PhaseLoopManager phaseLoop = Object.FindObjectOfType<PhaseLoopManager>();
        if (phaseLoop != null)
        {
            controller.phaseManager = phaseLoop;
        }

        // 4. Cari atau buat Global Volume
        Volume volume = Object.FindObjectOfType<Volume>();
        if (volume == null)
        {
            GameObject volumeGO = new GameObject("Global Volume");
            volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            Undo.RegisterCreatedObjectUndo(volumeGO, "Create Global Volume");
            Debug.Log("[GlitchSetup] Global Volume baru dibuat karena tidak ditemukan di scene.");
        }
        controller.globalVolume = volume;

        // 5. Pastikan Main Camera menyalakan Post-Processing
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            UniversalAdditionalCameraData camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            camData.renderPostProcessing = true;
            Debug.Log("[GlitchSetup] Main Camera Post-Processing diaktifkan.");
        }

        Undo.RegisterCreatedObjectUndo(glitchGO, "Create Glitch Effect");
        Selection.activeGameObject = glitchGO;

        // Tandai scene berubah (dirty)
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        // Selesai
        string report = "Efek Glitch berhasil dipasang!\n\n" +
                        "Otomatis mengatur:\n" +
                        "- DistortionEffectController\n" +
                        "- PhaseLoopManager Wire\n" +
                        "- Global Volume Wire\n" +
                        "- Camera Post-Processing\n\n" +
                        "Logic Glitch (Fase 2 / Stability 50%-30% per 30 detik) sudah siap!";
        
        EditorUtility.DisplayDialog("Setup Selesai", report, "Mantap!");
        Debug.Log("[GlitchSetup] Setup berhasil!");
    }
}

using UnityEngine;
using UnityEditor;

public class ConvertMaterialsToURP : EditorWindow
{
    [MenuItem("Tools/Space-Jam/Convert All Sprite Materials To Lit")]
    public static void ShowWindow()
    {
        GetWindow<ConvertMaterialsToURP>("Material Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Convert All Sprite Materials to URP Lit", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Klik tombol di bawah untuk mengubah semua material", EditorStyles.wordWrappedLabel);
        GUILayout.Label("Sprite menjadi 'Universal Render Pipeline/2D/Sprite-Lit-Default'.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(20);

        if (GUILayout.Button("Convert Now!"))
        {
            ConvertMaterials();
        }
    }

    private void ConvertMaterials()
    {
        // Cari shader URP Lit 2D
        Shader litShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (litShader == null)
        {
            Debug.LogError("Shader 'Universal Render Pipeline/2D/Sprite-Lit-Default' tidak ditemukan!");
            return;
        }

        int convertedCount = 0;

        // Cari semua SpriteRenderer di scene
        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr.sharedMaterial != null)
            {
                // Jika shader lama adalah standard/unlit sprite
                if (sr.sharedMaterial.shader.name == "Sprites/Default" || 
                    sr.sharedMaterial.shader.name == "Universal Render Pipeline/2D/Sprite-Unlit-Default")
                {
                    // Pastikan kita tidak mengubah material bawaan Unity secara permanen
                    // Tapi karena ini instruksi paksa, kita coba ubah shadernya langsung
                    sr.sharedMaterial.shader = litShader;
                    EditorUtility.SetDirty(sr.sharedMaterial);
                    convertedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ConvertMaterialsToURP] Berhasil mengkonversi {convertedCount} komponen SpriteRenderer ke URP Lit!");
        EditorUtility.DisplayDialog("Sukses!", $"Berhasil mengkonversi {convertedCount} objek ke material URP Lit. Sekarang ruangan harusnya terkena cahaya/shader!", "OK");
    }
}

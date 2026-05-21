using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mengelola spawn/destroy ShadowMimic di fase Liminal.
/// Jumlah shadow scale berdasarkan loop: loop 1 = 1 shadow,
/// tiap 2 loop tambah 1, maksimal maxShadowCount.
/// </summary>
public class ShadowSpawner : MonoBehaviour
{
    public static ShadowSpawner Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject shadowPrefab;

    [Header("Difficulty")]
    [SerializeField] private int currentLoop = 1;

    [Tooltip("Maksimal shadow yang bisa spawn sekaligus.")]
    [SerializeField] private int maxShadowCount = 4;

    [Tooltip("Jarak offset antar shadow saat spawn (supaya tidak tumpuk).")]
    [SerializeField] private float spawnOffsetRadius = 1.5f;

    private readonly List<GameObject> activeShadows = new();

    private void Awake()
    {
        Instance = this;
    }

    // ?? Public API ?????????????????????????????????????????????????????????

    /// <summary>
    /// Spawn shadow sesuai jumlah yang dihitung dari loop saat ini.
    /// Dipanggil oleh LiminalRoomManager.
    /// </summary>
    public void SpawnOnPlayer()
    {
        if (PlayerRecorder.Instance == null)
        {
            Debug.LogWarning("[ShadowSpawner] PlayerRecorder tidak ditemukan!");
            return;
        }

        if (shadowPrefab == null)
        {
            Debug.LogWarning("[ShadowSpawner] Shadow Prefab belum di-assign!");
            return;
        }

        RemoveAllShadows();

        int count = GetShadowCountForLoop(currentLoop);

        Transform player = PlayerRecorder.Instance.transform;

        for (int i = 0; i < count; i++)
        {
            // Offset melingkar agar tidak menumpuk di titik yang sama
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * spawnOffsetRadius,
                Mathf.Sin(angle) * spawnOffsetRadius,
                0f
            );

            GameObject obj = Instantiate(
                shadowPrefab,
                player.position + offset,
                Quaternion.identity
            );

            ShadowMimic shadow = obj.GetComponent<ShadowMimic>();
            if (shadow != null)
            {
                shadow.SetLoop(currentLoop);
            }

            activeShadows.Add(obj);
        }

        Debug.Log(
            $"[ShadowSpawner] Spawn {count} shadow (loop {currentLoop})"
        );
    }

    /// <summary>
    /// Hapus semua shadow yang aktif.
    /// </summary>
    public void RemoveShadow() => RemoveAllShadows();

    public void RemoveAllShadows()
    {
        foreach (var s in activeShadows)
        {
            if (s != null) Destroy(s);
        }

        activeShadows.Clear();
    }

    /// <summary>
    /// Update loop saat ini dan sesuaikan difficulty shadow yang aktif.
    /// </summary>
    public void SetLoop(int loop)
    {
        currentLoop = loop;

        foreach (var s in activeShadows)
        {
            if (s == null) continue;

            ShadowMimic mimic = s.GetComponent<ShadowMimic>();
            mimic?.SetLoop(currentLoop);
        }
    }

    // ?? Helper ?????????????????????????????????????????????????????????????

    /// <summary>
    /// Loop 1-2 ? 1 shadow, loop 3-4 ? 2 shadow, dst. Max = maxShadowCount.
    /// </summary>
    private int GetShadowCountForLoop(int loop)
    {
        int count = 1 + (loop - 1) / 2;
        return Mathf.Clamp(count, 1, maxShadowCount);
    }
}
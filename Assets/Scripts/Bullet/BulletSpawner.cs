using System.Collections.Generic;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private RingBullet bulletPrefab;
    [SerializeField] private Transform center;

    [Header("Ring Settings")]
    [SerializeField] private int count = 12;
    [SerializeField] private float spawnDistance = 10f;

    [Header("Reference")]
    [SerializeField] private PlayerMovement player;

    private int ringIndex;
    private bool isActive;

    // Track semua bullet yang di-spawn agar bisa di-cleanup saat stop
    private readonly List<RingBullet> activeBullets = new();

    private Coroutine spawnCoroutine;
    [SerializeField] private float spawnInterval = 3f;

    public void StartSpawning()
    {
        isActive = true;
        ringIndex = 0;
        
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private System.Collections.IEnumerator SpawnLoop()
    {
        while (isActive)
        {
            SpawnRing();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void StopSpawning()
    {
        isActive = false;
        
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // Destroy semua bullet yang masih aktif
        foreach (var bullet in activeBullets)
        {
            if (bullet != null) Destroy(bullet.gameObject);
        }

        activeBullets.Clear();
    }

    public void SpawnRing()
    {
        // Jangan spawn ring baru kalau spawner sudah di-stop
        if (!isActive)
        {
            return;
        }

        float angleStep = 360f / count;
        float rotationOffset = (angleStep * 0.5f) * ringIndex;

        for (int i = 0; i < count; i++)
        {
            float angle = (i * angleStep) + rotationOffset;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 spawnPos = new Vector2(
                center.position.x + Mathf.Cos(rad) * spawnDistance,
                center.position.y + Mathf.Sin(rad) * spawnDistance
            );

            Vector2 dirToCenter =
                (center.position - (Vector3)spawnPos).normalized;

            RingBullet bullet = Instantiate(
                bulletPrefab,
                spawnPos,
                Quaternion.identity
            );

            bullet.SetDirection(dirToCenter);
            bullet.SetPlayer(player);
            bullet.SetSpawner(this);
            
            // Trigger spawn dari bullet tidak diperlukan lagi
            bullet.SetCanSpawnNextRing(false);

            activeBullets.Add(bullet);
        }

        ringIndex++;
    }

    // Dipanggil oleh RingBullet saat destroyed, agar list tetap bersih
    public void UnregisterBullet(RingBullet bullet)
    {
        activeBullets.Remove(bullet);
    }
}
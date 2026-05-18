using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowSpawner : MonoBehaviour
{
    public static ShadowSpawner Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject shadowPrefab;

    [Header("Spawn Offset")]
    [SerializeField] private Vector3 offset = new Vector3(-2f, 0f, 0f);

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnOnPlayer()
    {
        if (PlayerRecorder.Instance == null)
        {
            return;
        }

        Transform player = PlayerRecorder.Instance.transform;

        Vector3 spawnPos = player.position + offset;

        Instantiate(shadowPrefab, spawnPos, Quaternion.identity);
    }

    public void SpawnShadow(Vector3 position)
    {
        Instantiate(shadowPrefab, position, Quaternion.identity);
    }
}
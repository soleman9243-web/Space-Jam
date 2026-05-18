using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowTestTrigger : MonoBehaviour
{
    [Header("Test Toggle")]
    [SerializeField] private bool spawnShadow;

    private void Update()
    {
        if (spawnShadow)
        {
            spawnShadow = false;

            Spawn();
        }
    }

    private void Spawn()
    {
        if (ShadowSpawner.Instance == null)
        {
            Debug.LogWarning("ShadowSpawner belum ada di scene!");
            return;
        }

        ShadowSpawner.Instance.SpawnOnPlayer();

        Debug.Log("Shadow spawned via test bool");
    }
}
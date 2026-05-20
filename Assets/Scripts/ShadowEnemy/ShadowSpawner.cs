using UnityEngine;

public class ShadowSpawner : MonoBehaviour
{
    public static ShadowSpawner Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject shadowPrefab;

    [Header("Difficulty")]
    [SerializeField] private int currentLoop = 1;

    private GameObject currentShadow;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnOnPlayer();
    }

    public void SpawnOnPlayer()
    {
        if (PlayerRecorder.Instance == null)
        {
            Debug.LogWarning("PlayerRecorder tidak ditemukan!");
            return;
        }

        if (shadowPrefab == null)
        {
            Debug.LogWarning("Shadow Prefab belum di assign!");
            return;
        }

        if (currentShadow != null)
        {
            return;
        }

        Transform player = PlayerRecorder.Instance.transform;

        currentShadow = Instantiate(
            shadowPrefab,
            player.position,
            Quaternion.identity
        );

        ShadowMimic shadow = currentShadow.GetComponent<ShadowMimic>();

        if (shadow != null)
        {
            shadow.SetLoop(currentLoop);
        }
    }

    public void RemoveShadow()
    {
        if (currentShadow == null)
        {
            return;
        }

        Destroy(currentShadow);

        currentShadow = null;
    }

    public void SetLoop(int loop)
    {
        currentLoop = loop;

        if (currentShadow != null)
        {
            ShadowMimic shadow = currentShadow.GetComponent<ShadowMimic>();

            if (shadow != null)
            {
                shadow.SetLoop(currentLoop);
            }
        }
    }
}
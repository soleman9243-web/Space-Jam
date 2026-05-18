using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DreamChasingLight : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public PhaseLoopManager phaseManager;

    [Header("Light Movement Settings")]
    [Tooltip("Kecepatan awal pergerakan lampu sorot.")]
    public float initialSpeed = 3f;
    [Tooltip("Berapa kecepatan bertambah setiap detiknya (makin lama makin cepat).")]
    public float speedIncreaseRate = 0.1f;
    [Tooltip("Batas jarak maksimal lampu bisa bergeser secara acak dari pusat/pemain.")]
    public float wanderRadius = 10f;

    [Header("Room Bounds (Batas Ruangan Kamera)")]
    [Tooltip("Batas layar kiri")]
    public float minX = -8f;
    [Tooltip("Batas layar kanan")]
    public float maxX = 8f;
    [Tooltip("Batas layar bawah")]
    public float minY = -4.5f;
    [Tooltip("Batas layar atas")]
    public float maxY = 4.5f;

    [Header("Safety & Damage Settings")]
    [Tooltip("Jari-jari area terang lampu. Jika player keluar dari ini, Stability berkurang.")]
    public float safeRadius = 3.5f;
    [Tooltip("Jumlah stability yang hilang per detik jika berada di luar lingkaran terang.")]
    public float stabilityLossPerSecond = 10f;

    private float currentSpeed;
    private Vector2 targetPosition;
    private bool isActive = false;
    private Light2D pointLight;

    private void Start()
    {
        currentSpeed = initialSpeed;
        
        pointLight = GetComponent<Light2D>();
        
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (phaseManager == null) phaseManager = FindObjectOfType<PhaseLoopManager>();
        
        if (phaseManager != null)
        {
            phaseManager.OnPhaseChanged.AddListener(HandlePhaseChanged);
            // Cek status saat pertama kali nyala
            HandlePhaseChanged(phaseManager.CurrentState);
        }
        
        PickNewTarget();
    }

    private void HandlePhaseChanged(GameState state)
    {
        if (state == GameState.Dream)
        {
            isActive = true;
            currentSpeed = initialSpeed;
            if (pointLight != null) pointLight.enabled = true;
            
            // Mulai posisi lampu dari dekat pemain agar pemain punya waktu bersiap
            if (player != null) transform.position = player.position;
            PickNewTarget();
        }
        else
        {
            isActive = false;
            // Matikan lampu kalau bukan di fase Dream
            if (pointLight != null) pointLight.enabled = false;
        }
    }

    private void Update()
    {
        if (!isActive || player == null) return;

        // 1. Pergerakan Lampu secara Acak (Mengejar target point)
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
        
        // Kecepatan lampu bertambah terus seiring waktu (bikin panik)
        currentSpeed += speedIncreaseRate * Time.deltaTime;

        // Jika lampu sudah sampai di target point, cari titik acak baru
        if (Vector2.Distance(transform.position, targetPosition) < 0.2f)
        {
            PickNewTarget();
        }

        // 2. Mekanik Hukuman Pengurangan Stability (jika player di luar lampu)
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer > safeRadius)
        {
            // Kurangi stability 10% (10 angka) per detik secara mulus (pakai deltaTime)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ModifyStability(-stabilityLossPerSecond * Time.deltaTime);
            }
            else if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.ReduceStability(stabilityLossPerSecond * Time.deltaTime);
            }
        }
    }

    private void PickNewTarget()
    {
        if (player == null) return;
        // Cari posisi tujuang pergerakan acak baru di area sekitar pemain
        Vector2 randomDir = Random.insideUnitCircle * wanderRadius;
        Vector2 newTarget = (Vector2)player.position + randomDir;

        // Kunci posisi target agar TIDAK bisa keluar dari batas layar/ruangan
        newTarget.x = Mathf.Clamp(newTarget.x, minX, maxX);
        newTarget.y = Mathf.Clamp(newTarget.y, minY, maxY);

        targetPosition = newTarget;
    }

    // Untuk membantu memvisualisasikan ukuran zona aman dan batas ruangan di layar Editor
    private void OnDrawGizmos()
    {
        // Gambar lingkaran aman (kuning)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, safeRadius);

        // Gambar kotak batas ruangan (merah)
        Gizmos.color = Color.red;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}

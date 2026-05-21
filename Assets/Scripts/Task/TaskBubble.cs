using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Dialogue bubble yang muncul di atas player untuk menampilkan task objective.
///
/// Setup:
///   1. Buat child GameObject di player: "TaskBubble"
///   2. Tambahkan World Space Canvas sebagai child-nya, anchor di atas player
///   3. Di dalam Canvas, buat Panel (bubbleRoot) + TextMeshPro (bubbleText)
///   4. Assign semua referensi di Inspector
///
/// Behaviour:
///   - Tab  ? toggle bubble on/off
///   - Task baru ? bubble otomatis muncul + typewriter mulai dari awal
///   - Progress update (trash, dll) ? text langsung ganti tanpa typewriter
///   - Tidak ada task ? bubble otomatis sembunyi
/// </summary>
public class TaskBubble : MonoBehaviour
{
    // =========================================================
    // INSPECTOR FIELDS
    // =========================================================

    [Header("References")]
    [Tooltip("Root GameObject bubble (Panel/background). Di-toggle aktif/nonaktif.")]
    [SerializeField] private GameObject bubbleRoot;

    [Tooltip("TextMeshPro di dalam bubble untuk menampilkan task text.")]
    [SerializeField] private TextMeshProUGUI bubbleText;

    [Header("Typewriter")]
    [Tooltip("Jeda antar karakter dalam detik (lebih kecil = lebih cepat).")]
    [SerializeField] private float charDelay = 0.035f;

    [Header("Input")]
    [Tooltip("Tombol untuk toggle bubble.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Auto-Hide")]
    [Tooltip("Bubble otomatis sembunyi setelah durasi ini (detik). 0 = tidak auto-hide.")]
    [SerializeField] private float autoHideDelay = 0f;

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private bool isVisible = false;

    /// <summary>
    /// Text terakhir yang di-set (untuk keperluan re-show saat toggle).
    /// </summary>
    private string currentText = "";

    private Coroutine typewriterCoroutine;
    private Coroutine autoHideCoroutine;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (bubbleRoot != null)
        {
            bubbleRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>
    /// Tampilkan bubble dengan task text baru + typewriter dari awal.
    /// Dipanggil otomatis saat task baru di-assign.
    /// </summary>
    public void ShowNewTask(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            HideBubble();
            return;
        }

        currentText = text;

        ShowBubble(text);
    }

    /// <summary>
    /// Update text bubble tanpa typewriter dan tanpa auto-show.
    /// Dipakai untuk progress update (misal: "Clean Trash 2/5").
    /// Kalau bubble sedang sembunyi, currentText tetap di-update
    /// sehingga saat player toggle buka, teks sudah yang terbaru.
    /// </summary>
    public void RefreshText(string text)
    {
        currentText = text;

        // Hanya update tampilan kalau bubble sedang terbuka
        if (!isVisible || bubbleText == null)
        {
            return;
        }

        // Stop typewriter lama kalau masih jalan
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        bubbleText.text = text;
    }

    /// <summary>
    /// Tampilkan bubble dengan typewriter dari awal.
    /// </summary>
    public void ShowBubble(string text)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            Debug.LogWarning("[TaskBubble] bubbleRoot atau bubbleText belum di-assign!");
            return;
        }

        isVisible = true;

        if (!gameObject.activeInHierarchy)
        {
            bubbleText.text = text;
            return;
        }

        bubbleRoot.SetActive(true);

        StopAllRunningCoroutines();

        typewriterCoroutine = StartCoroutine(TypewriterEffect(text));

        if (autoHideDelay > 0f)
        {
            autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(autoHideDelay));
        }
    }

    /// <summary>
    /// Sembunyikan bubble.
    /// </summary>
    public void HideBubble()
    {
        isVisible = false;

        StopAllRunningCoroutines();

        if (bubbleRoot != null)
        {
            bubbleRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle antara tampil / sembunyi.
    /// Saat dibuka lagi, typewriter mulai dari awal dengan text terbaru.
    /// </summary>
    public void Toggle()
    {
        if (isVisible)
        {
            HideBubble();
        }
        else
        {
            if (!string.IsNullOrEmpty(currentText))
            {
                ShowBubble(currentText);
            }
        }
    }

    /// <summary>
    /// Kosongkan text dan sembunyikan bubble.
    /// Dipanggil saat semua task selesai atau masuk Liminal.
    /// </summary>
    public void ClearBubble()
    {
        currentText = "";

        HideBubble();
    }

    // =========================================================
    // PRIVATE HELPERS
    // =========================================================

    private void StopAllRunningCoroutines()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    private IEnumerator TypewriterEffect(string text)
    {
        bubbleText.text = "";

        foreach (char c in text)
        {
            bubbleText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        typewriterCoroutine = null;
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        HideBubble();

        autoHideCoroutine = null;
    }
}
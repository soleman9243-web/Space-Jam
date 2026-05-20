using UnityEngine;

namespace SpaceJam.Minigames
{
    /// <summary>
    /// Script individual pada setiap lilin. Mengelola state nyala/mati dan interaksi player.
    /// </summary>
    public class CandleInteractable : MonoBehaviour
    {
        [SerializeField] private int candleIndex;
        [SerializeField] private CandlePuzzleTask parentTask;
        [SerializeField] private GameObject flameVisual;
        [SerializeField] private SpriteRenderer bodyRenderer;

        [Header("Colors")]
        [SerializeField] private Color unlitBodyColor = new Color(0.35f, 0.22f, 0.1f, 1f);
        [SerializeField] private Color litBodyColor = new Color(0.55f, 0.35f, 0.15f, 1f);

        private bool isLit = false;

        public bool IsLit => isLit;
        public int CandleIndex => candleIndex;

        private void Awake()
        {
            // Fallback: auto-find parent task if not serialized
            if (parentTask == null)
            {
                parentTask = GetComponentInParent<CandlePuzzleTask>();
            }
        }

        public void Initialize(CandlePuzzleTask task, int index, GameObject flame, SpriteRenderer body)
        {
            parentTask = task;
            candleIndex = index;
            flameVisual = flame;
            bodyRenderer = body;
            SetLit(false);
        }

        /// <summary>
        /// Dipanggil oleh InteractObject2D.onInteract saat player menekan tombol interaksi.
        /// </summary>
        public void TryLight()
        {
            if (isLit) return; // Sudah nyala, abaikan
            if (parentTask != null)
            {
                parentTask.OnCandleInteracted(candleIndex);
            }
        }

        public void SetLit(bool lit)
        {
            isLit = lit;

            if (flameVisual != null)
                flameVisual.SetActive(lit);

            if (bodyRenderer != null)
                bodyRenderer.color = lit ? litBodyColor : unlitBodyColor;
        }
    }
}

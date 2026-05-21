using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SlidingTransitionSetup : EditorWindow
{
    [MenuItem("Tools/Space-Jam/Setup Sliding Transition")]
    public static void SetupTransition()
    {
        // Cari IntroCanvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas introCanvas = null;
        foreach (var c in canvases)
        {
            if (c.name.Contains("IntroCanvas"))
            {
                introCanvas = c;
                break;
            }
        }

        if (introCanvas == null)
        {
            Debug.LogError("[SlidingTransitionSetup] IntroCanvas tidak ditemukan di scene!");
            return;
        }

        // Cari atau buat ScreenFader
        Transform faderTrans = introCanvas.transform.Find("ScreenFader");
        GameObject faderGO;
        if (faderTrans == null)
        {
            faderGO = new GameObject("ScreenFader", typeof(RectTransform), typeof(CanvasGroup));
            faderGO.transform.SetParent(introCanvas.transform, false);
        }
        else
        {
            faderGO = faderTrans.gameObject;
        }

        // Setup Fader GO
        faderGO.layer = LayerMask.NameToLayer("UI");
        RectTransform faderRT = faderGO.GetComponent<RectTransform>();
        faderRT.anchorMin = Vector2.zero;
        faderRT.anchorMax = Vector2.one;
        faderRT.sizeDelta = Vector2.zero;
        faderRT.anchoredPosition = Vector2.zero;
        
        CanvasGroup cg = faderGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = faderGO.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;
        
        // Hapus Image di root fader jika ada
        Image rootImg = faderGO.GetComponent<Image>();
        if (rootImg != null)
        {
            DestroyImmediate(rootImg);
        }

        Color doorColor = new Color(0.02f, 0.02f, 0.05f, 1f);

        // Buat Left Door
        Transform lTrans = faderGO.transform.Find("LeftDoor");
        GameObject lGO = lTrans != null ? lTrans.gameObject : new GameObject("LeftDoor", typeof(RectTransform), typeof(Image));
        lGO.transform.SetParent(faderGO.transform, false);
        lGO.layer = LayerMask.NameToLayer("UI");
        
        RectTransform lRT = lGO.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 0f);
        lRT.anchorMax = new Vector2(0.5f, 1f);
        lRT.pivot = new Vector2(0f, 0.5f);
        lRT.sizeDelta = Vector2.zero;
        lRT.anchoredPosition = Vector2.zero;
        lRT.localScale = new Vector3(0, 1, 1); // TERBUKA (hidden)
        
        Image lImg = lGO.GetComponent<Image>();
        if (lImg == null) lImg = lGO.AddComponent<Image>();
        lImg.color = doorColor;

        // Buat Right Door
        Transform rTrans = faderGO.transform.Find("RightDoor");
        GameObject rGO = rTrans != null ? rTrans.gameObject : new GameObject("RightDoor", typeof(RectTransform), typeof(Image));
        rGO.transform.SetParent(faderGO.transform, false);
        rGO.layer = LayerMask.NameToLayer("UI");

        RectTransform rRT = rGO.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.5f, 0f);
        rRT.anchorMax = new Vector2(1f, 1f);
        rRT.pivot = new Vector2(1f, 0.5f);
        rRT.sizeDelta = Vector2.zero;
        rRT.anchoredPosition = Vector2.zero;
        rRT.localScale = new Vector3(0, 1, 1); // TERBUKA (hidden)

        Image rImg = rGO.GetComponent<Image>();
        if (rImg == null) rImg = rGO.AddComponent<Image>();
        rImg.color = doorColor;

        // Mark scene as dirty so changes are saved
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        Debug.Log("[SlidingTransitionSetup] SUKSES! Sliding Doors berhasil dipasang di IntroCanvas.");
    }
}

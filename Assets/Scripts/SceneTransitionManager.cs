using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    private const int OverlaySortOrder = 32767;

    private static SceneTransitionManager instance;

    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.35f;
    [SerializeField] private Color fadeColor = Color.black;

    private CanvasGroup fadeCanvasGroup;
    private Image fadeImage;
    private bool isTransitioning;

    public static void LoadScene(string sceneName)
    {
        EnsureInstance().StartTransition(sceneName);
    }

    private static SceneTransitionManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject(nameof(SceneTransitionManager));
        instance = managerObject.AddComponent<SceneTransitionManager>();
        instance.InitializeOverlay();
        DontDestroyOnLoad(managerObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        InitializeOverlay();
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeOverlay()
    {
        if (fadeCanvasGroup != null)
            return;

        GameObject canvasObject = new GameObject("SceneFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        fadeCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true;
    }

    private void StartTransition(string sceneName)
    {
        if (isTransitioning)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneTransitionManager] Scene name is empty.", this);
            return;
        }

        StartCoroutine(TransitionToScene(sceneName));
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;
        yield return Fade(1f, fadeOutDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation == null)
        {
            Debug.LogWarning($"[SceneTransitionManager] Could not load scene '{sceneName}'.", this);
            yield return Fade(0f, fadeInDuration);
            isTransitioning = false;
            yield break;
        }

        while (!loadOperation.isDone)
            yield return null;

        yield return Fade(0f, fadeInDuration);
        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        InitializeOverlay();

        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = true;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        bool isVisible = targetAlpha > 0f;
        fadeCanvasGroup.blocksRaycasts = isVisible;
        fadeCanvasGroup.interactable = isVisible;
    }
}

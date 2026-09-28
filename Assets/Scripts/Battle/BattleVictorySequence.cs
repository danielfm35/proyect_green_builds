using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BossUI))]
public sealed class BattleVictorySequence : MonoBehaviour
{
    private const int VictoryGold = 10;
    private const int OverlaySortOrder = 20000;

    private BossUI bossUI;
    private ShopManager shopManager;
    private bool victoryStarted;
    private EncounterProgression progression;

    private void Start()
    {
        progression = GetComponent<EncounterProgression>();
        if (progression == null)
            progression = gameObject.AddComponent<EncounterProgression>();
        progression.Initialize(bossUI);
    }

    public void ResetVictory() => victoryStarted = false;

    private void Awake()
    {
        bossUI = GetComponent<BossUI>();
    }

    private void OnEnable()
    {
        if (bossUI == null)
            bossUI = GetComponent<BossUI>();
        bossUI.HealthChanged.AddListener(OnBossHealthChanged);
    }

    private void OnDisable()
    {
        if (bossUI != null)
            bossUI.HealthChanged.RemoveListener(OnBossHealthChanged);
    }

    private void OnBossHealthChanged(int currentHealth, int maximumHealth)
    {
        if (!victoryStarted && currentHealth <= 0)
        {
            victoryStarted = true;
            StartCoroutine(PlayVictorySequence());
        }
    }

    private IEnumerator PlayVictorySequence()
    {
        shopManager = FindFirstObjectByType<ShopManager>();

        GameObject canvasObject = new GameObject(
            "VictoryCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image dimmer = CreateImage(canvasObject.transform, "Dimmer", new Color(0.01f, 0.025f, 0.06f, 0f));
        Stretch(dimmer.rectTransform);

        RectTransform banner = CreateBanner(canvasObject.transform);
        CanvasGroup bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        banner.localScale = Vector3.one * 0.94f;
        bannerGroup.alpha = 0f;

        float elapsed = 0f;
        const float entranceDuration = 0.48f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            banner.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
            banner.anchoredPosition = new Vector2(0f, Mathf.Lerp(-20f, 0f, eased));
            bannerGroup.alpha = eased;
            dimmer.color = new Color(0.01f, 0.025f, 0.06f, 0.42f * eased);
            yield return null;
        }
        banner.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(0.85f);
        yield return VictoryGoldTransfer.Play(canvas, banner, bannerGroup, dimmer, shopManager, VictoryGold);

        Button continueButton = CreateContinueButton(canvasObject.transform);
        RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
        CanvasGroup buttonGroup = continueButton.gameObject.AddComponent<CanvasGroup>();
        buttonGroup.alpha = 0f;
        buttonRect.localScale = Vector3.one * 0.7f;
        elapsed = 0f;
        const float buttonDuration = 0.4f;
        while (elapsed < buttonDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / buttonDuration);
            buttonGroup.alpha = t;
            buttonRect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
    }

    private static RectTransform CreateBanner(Transform parent)
    {
        GameObject bannerObject = new GameObject(
            "VictoryBanner",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        bannerObject.transform.SetParent(parent, false);
        RectTransform rect = bannerObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(720f, 210f);

        Image background = bannerObject.GetComponent<Image>();
        background.color = new Color(0.025f, 0.035f, 0.055f, 0.98f);
        background.raycastTarget = false;
        Outline outline = bannerObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.52f, 0.2f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject textObject = new GameObject(
            "VictoryText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(rect, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(0f, 55f);
        text.text = "VICTORY";
        text.fontSize = 76f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.94f, 0.72f, 1f);
        text.outlineColor = new Color32(45, 19, 4, 255);
        text.outlineWidth = 0.08f;
        text.raycastTarget = false;
        var rewardObject = new GameObject("VictoryReward", typeof(RectTransform), typeof(TextMeshProUGUI));
        rewardObject.transform.SetParent(rect, false);
        var reward = rewardObject.GetComponent<TextMeshProUGUI>();
        reward.rectTransform.sizeDelta = new Vector2(400f, 50f);
        reward.rectTransform.anchoredPosition = new Vector2(0f, -55f);
        reward.text = $"+{VictoryGold} ORO";
        reward.fontSize = 30f;
        reward.characterSpacing = 5f;
        reward.alignment = TextAlignmentOptions.Center;
        reward.color = new Color(1f, 0.79f, 0.35f);
        reward.raycastTarget = false;
        return rect;
    }

    private Button CreateContinueButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "ContinueButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(Outline));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(360f, 92f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.32f, 0.63f, 1f);
        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.72f, 0.12f, 1f);
        outline.effectDistance = new Vector2(5f, -5f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.15f, 0.48f, 0.9f, 1f);
        colors.pressedColor = new Color(0.04f, 0.2f, 0.45f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            button.interactable = false;
            progression.ShowAfterVictory();
            Destroy(parent.gameObject);
        });

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(rect, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        Stretch(label.rectTransform);
        label.text = "CONTINUAR";
        label.fontSize = 42f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.94f, 0.72f, 1f);
        label.outlineWidth = 0.2f;
        label.raycastTarget = false;
        return button;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = name == "Dimmer";
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

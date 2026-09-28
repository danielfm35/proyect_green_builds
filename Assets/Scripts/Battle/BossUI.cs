using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class BossUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BossData bossData;

    [Header("View")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Gold milestones")]
    [Tooltip("Porcentaje de vida maxima que hay que quitar para obtener cada recompensa.")]
    [SerializeField, Range(1, 100)] private int rewardIntervalPercent = 20;
    [Tooltip("Oro entregado por cada hito alcanzado.")]
    [SerializeField, Min(0)] private int goldPerMilestone = 10;
    [SerializeField] private ShopManager goldReceiver;

    [Header("Health plates")]
    [SerializeField] private Color plateDividerColor = new Color(0.16f, 0.10f, 0.06f, 1f);
    [SerializeField, Min(1f)] private float plateDividerWidth = 3f;

    private int currentHealth;
    private int rewardedMilestoneCount;
    private RectTransform milestoneContainer;
    private RectMask2D portraitMask;
    private AspectRatioFitter portraitFitter;

    public BossData Data => bossData;
    public int CurrentHealth => currentHealth;
    public int MaximumHealth => bossData == null ? 1 : bossData.MaximumHealth;
    public UnityEvent<int, int> HealthChanged { get; } = new();

    public void SetGoldMilestonesVisible(bool visible)
    {
        if (milestoneContainer != null)
            milestoneContainer.gameObject.SetActive(visible);
    }

    private void Awake()
    {
        if (Application.isPlaying && GetComponent<BattleVictorySequence>() == null)
            gameObject.AddComponent<BattleVictorySequence>();

        RefreshFromData(true);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            RefreshFromData(true);
    }

    public void SetBoss(BossData newBoss)
    {
        bossData = newBoss;
        RefreshFromData(true);
    }

    public void SetHealth(int value)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, 0, MaximumHealth);
        RefreshHealth();
        RewardCrossedMilestones(previousHealth, currentHealth);
        HealthChanged.Invoke(currentHealth, MaximumHealth);
    }

    public void TakeDamage(int amount)
    {
        SetHealth(currentHealth - Mathf.Max(0, amount));
    }

    public IEnumerator PlayDamageEffect(int amount)
    {
        int damage = Mathf.Max(0, amount);
        if (damage == 0)
            yield break;

        RectTransform hitTarget = portraitImage != null
            ? portraitImage.rectTransform
            : transform as RectTransform;
        if (hitTarget == null)
        {
            TakeDamage(damage);
            yield break;
        }

        GameObject woundObject = new GameObject(
            "BossWound",
            typeof(RectTransform),
            typeof(CanvasGroup)
        );
        woundObject.transform.SetParent(hitTarget, false);
        woundObject.transform.SetAsLastSibling();

        RectTransform woundRect = woundObject.GetComponent<RectTransform>();
        woundRect.anchorMin = new Vector2(0.5f, 0.5f);
        woundRect.anchorMax = new Vector2(0.5f, 0.5f);
        woundRect.pivot = new Vector2(0.5f, 0.5f);
        woundRect.anchoredPosition = Vector2.zero;
        woundRect.sizeDelta = new Vector2(150f, 150f);
        woundRect.localScale = Vector3.zero;

        CreateDamageImage(woundRect);

        GameObject damageTextObject = new GameObject(
            "DamageText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        damageTextObject.transform.SetParent(woundRect, false);
        RectTransform damageTextRect = damageTextObject.GetComponent<RectTransform>();
        damageTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        damageTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        damageTextRect.anchoredPosition = new Vector2(0f, 92f);
        damageTextRect.sizeDelta = new Vector2(180f, 70f);

        TextMeshProUGUI damageText = damageTextObject.GetComponent<TextMeshProUGUI>();
        damageText.text = $"-{damage}";
        damageText.fontSize = 48f;
        damageText.fontStyle = FontStyles.Bold;
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.color = new Color(1f, 0.18f, 0.12f, 1f);
        damageText.outlineColor = new Color32(35, 0, 0, 255);
        damageText.outlineWidth = 0.25f;
        damageText.raycastTarget = false;

        TakeDamage(damage);

        RectTransform panelRect = transform as RectTransform;
        Vector2 panelStartPosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
        Color portraitStartColor = portraitImage != null ? portraitImage.color : Color.white;
        CanvasGroup woundCanvasGroup = woundObject.GetComponent<CanvasGroup>();
        const float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float woundScale = t < 0.2f
                ? Mathf.Lerp(0f, 1.25f, t / 0.2f)
                : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.2f) / 0.18f));
            woundRect.localScale = Vector3.one * woundScale;
            woundCanvasGroup.alpha = t < 0.65f ? 1f : 1f - ((t - 0.65f) / 0.35f);
            damageTextRect.anchoredPosition = new Vector2(0f, 92f + 34f * t);

            if (panelRect != null)
            {
                float shakeStrength = 14f * (1f - t);
                panelRect.anchoredPosition = panelStartPosition + new Vector2(
                    Mathf.Sin(t * 70f) * shakeStrength,
                    Mathf.Cos(t * 53f) * shakeStrength * 0.35f
                );
            }

            if (portraitImage != null)
                portraitImage.color = Color.Lerp(new Color(1f, 0.15f, 0.12f, 1f), portraitStartColor, t);

            yield return null;
        }

        if (panelRect != null)
            panelRect.anchoredPosition = panelStartPosition;
        if (portraitImage != null)
            portraitImage.color = portraitStartColor;
        Destroy(woundObject);
    }

    private static void CreateDamageImage(RectTransform parent)
    {
        GameObject damageObject = new GameObject(
            "DamageImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        damageObject.transform.SetParent(parent, false);

        RectTransform damageRect = damageObject.GetComponent<RectTransform>();
        damageRect.anchorMin = new Vector2(0.5f, 0.5f);
        damageRect.anchorMax = new Vector2(0.5f, 0.5f);
        damageRect.pivot = new Vector2(0.5f, 0.5f);
        damageRect.anchoredPosition = Vector2.zero;
        damageRect.sizeDelta = new Vector2(220f, 220f);

        Image damageImage = damageObject.GetComponent<Image>();
        damageImage.sprite = Resources.Load<Sprite>("Images/UI/damage");
        damageImage.preserveAspect = true;
        damageImage.raycastTarget = false;
    }

    public void Heal(int amount)
    {
        SetHealth(currentHealth + Mathf.Max(0, amount));
    }

    [ContextMenu("Refresh From Data")]
    public void RefreshFromData()
    {
        RefreshFromData(false);
    }

    private void RefreshFromData(bool resetHealth)
    {
        if (bossData == null)
        {
            if (nameText != null) nameText.text = "Boss";
            if (portraitImage != null) portraitImage.enabled = false;
            currentHealth = 0;
            RefreshHealth();
            return;
        }

        if (nameText != null)
            nameText.text = bossData.DisplayName;

        if (portraitImage != null)
        {
            portraitImage.sprite = bossData.Portrait;
            portraitImage.enabled = bossData.Portrait != null;
            portraitImage.preserveAspect = true;
            ConfigurePortraitCrop();
        }

        if (resetHealth || currentHealth <= 0 || currentHealth > MaximumHealth)
        {
            currentHealth = MaximumHealth;
            rewardedMilestoneCount = 0;
        }

        RefreshHealth();

        if (Application.isPlaying)
            EnsureMilestoneIndicators();
    }

    private void ConfigurePortraitCrop()
    {
        if (!Application.isPlaying || portraitImage == null)
            return;

        RectTransform frame = portraitImage.rectTransform.parent as RectTransform;
        if (frame == null)
            return;

        if (portraitMask == null)
            portraitMask = frame.GetComponent<RectMask2D>() ?? frame.gameObject.AddComponent<RectMask2D>();
        if (portraitFitter == null)
            portraitFitter = portraitImage.GetComponent<AspectRatioFitter>();

        bool crop = bossData != null && bossData.CropPortraitToFrame && bossData.Portrait != null;
        portraitMask.enabled = crop;
        if (portraitFitter != null)
        {
            portraitFitter.aspectRatio = crop
                ? bossData.Portrait.rect.width / bossData.Portrait.rect.height
                : 1f;
            portraitFitter.enabled = crop;
        }
        RectTransform portrait = portraitImage.rectTransform;
        portrait.anchorMin = crop ? new Vector2(0f, 1f) : Vector2.zero;
        portrait.anchorMax = Vector2.one;
        portrait.pivot = crop ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
        portrait.anchoredPosition = Vector2.zero;
        portrait.sizeDelta = Vector2.zero;
    }

    private void RewardCrossedMilestones(int previousHealth, int newHealth)
    {
        if (!Application.isPlaying || newHealth >= previousHealth || goldPerMilestone <= 0)
            return;

        int interval = Mathf.Clamp(rewardIntervalPercent, 1, 100);
        float damagePercent = 100f * (MaximumHealth - newHealth) / MaximumHealth;
        int reachedMilestones = Mathf.FloorToInt((damagePercent + 0.0001f) / interval);
        // El 100% (derrotar al jefe) no entrega oro mediante este sistema.
        int totalMilestones = 99 / interval;
        reachedMilestones = Mathf.Clamp(reachedMilestones, 0, totalMilestones);

        int newlyReached = reachedMilestones - rewardedMilestoneCount;
        if (newlyReached <= 0)
            return;

        if (goldReceiver == null)
            goldReceiver = FindFirstObjectByType<ShopManager>();

        if (goldReceiver == null)
        {
            Debug.LogWarning("[BossUI] No se encontro ShopManager; no se pudo entregar la recompensa de oro.", this);
            return;
        }

        int firstMilestone = rewardedMilestoneCount + 1;
        rewardedMilestoneCount = reachedMilestones;
        RectTransform bar = milestoneContainer != null ? milestoneContainer
            : healthFillImage != null ? healthFillImage.rectTransform.parent as RectTransform : null;
        if (bar == null)
        {
            goldReceiver.AddGold(newlyReached * goldPerMilestone);
            return;
        }
        Canvas canvas = bar.GetComponentInParent<Canvas>()?.rootCanvas;
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var origins = new Vector2[newlyReached];
        for (int i = 0; i < newlyReached; i++)
        {
            float fraction = 1f - (firstMilestone + i) * interval / 100f;
            Vector3 point = bar.TransformPoint(new Vector3(Mathf.Lerp(bar.rect.xMin, bar.rect.xMax, fraction), bar.rect.center.y, 0f));
            origins[i] = RectTransformUtility.WorldToScreenPoint(camera, point);
        }
        HealthMilestoneGoldEffect.Play(goldReceiver, origins, newlyReached * goldPerMilestone);
    }

    private void EnsureMilestoneIndicators()
    {
        if (healthFillImage == null)
            return;

        RectTransform fillRect = healthFillImage.rectTransform;
        RectTransform barRect = fillRect.parent as RectTransform;
        if (barRect == null)
            return;

        if (milestoneContainer != null)
        {
            milestoneContainer.gameObject.SetActive(false);
            Destroy(milestoneContainer.gameObject);
        }

        // A sibling of the fill: dividers stay fixed as health drains and follow
        // the bar automatically during layout changes and damage animations.
        GameObject containerObject = new GameObject("HealthPlates", typeof(RectTransform));
        milestoneContainer = containerObject.GetComponent<RectTransform>();
        milestoneContainer.SetParent(barRect, false);
        milestoneContainer.anchorMin = fillRect.anchorMin;
        milestoneContainer.anchorMax = fillRect.anchorMax;
        milestoneContainer.offsetMin = fillRect.offsetMin;
        milestoneContainer.offsetMax = fillRect.offsetMax;

        int interval = Mathf.Clamp(rewardIntervalPercent, 1, 100);
        int totalDividers = 99 / interval;
        for (int index = 1; index <= totalDividers; index++)
        {
            float healthFraction = 1f - index * interval / 100f;
            GameObject dividerObject = new GameObject(
                $"PlateDivider_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
            dividerRect.SetParent(milestoneContainer, false);
            dividerRect.anchorMin = new Vector2(healthFraction, 0f);
            dividerRect.anchorMax = new Vector2(healthFraction, 1f);
            dividerRect.anchoredPosition = Vector2.zero;
            dividerRect.sizeDelta = new Vector2(plateDividerWidth, 0f);

            Image divider = dividerObject.GetComponent<Image>();
            divider.color = plateDividerColor;
            divider.raycastTarget = false;
        }

        // Keep the health number readable above the plate divisions.
        if (healthText != null)
            healthText.transform.SetAsLastSibling();
    }

    private void RefreshHealth()
    {
        int maximum = MaximumHealth;
        float normalized = maximum <= 0 ? 0f : (float)currentHealth / maximum;

        if (healthFillImage != null)
        {
            RectTransform fillRect = healthFillImage.rectTransform;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.localScale = new Vector3(normalized, 1f, 1f);

            // Scaling the whole fill also scales its Shine child, so the lost
            // portion cannot remain visually covered after taking damage.
            healthFillImage.type = Image.Type.Simple;
            healthFillImage.fillAmount = 1f;

            Image background = fillRect.parent != null
                ? fillRect.parent.GetComponent<Image>()
                : null;
            if (background != null)
                background.color = Color.black;
        }

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maximum}";
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class PlayerUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerData playerData;

    [Header("View")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text healthMultiplierText;
    [SerializeField] private TMP_Text shieldMultiplierText;
    [SerializeField] private TMP_Text damageMultiplierText;
    [SerializeField] private TMP_Text magicResistMultiplierText;

    private int currentHealth;
    private int battleHealthBonus;
    private float armorRating;
    private float magicResistRating;
    private Coroutine healthGainAnimation;
    private RectTransform animatedHealthBar;
    private Vector3 healthBarOriginalScale;

    public PlayerData Data => playerData;
    public TMP_Text HealthStatText => healthMultiplierText;
    public TMP_Text ShieldStatText => shieldMultiplierText;
    public TMP_Text DamageStatText => damageMultiplierText;
    public TMP_Text MagicResistStatText => magicResistMultiplierText;
    public int CurrentHealth => currentHealth;
    public float ArmorRating => armorRating;
    public float MagicResistRating => magicResistRating;
    public int MaximumHealth => (playerData == null ? 1 : playerData.MaximumHealth) + battleHealthBonus;
    public UnityEvent<int, int> HealthChanged { get; } = new();

    private void Awake()
    {
        RefreshFromData(true);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            RefreshFromData(true);
    }

    private void OnDisable()
    {
        if (animatedHealthBar != null)
            animatedHealthBar.localScale = healthBarOriginalScale;

        healthGainAnimation = null;
        animatedHealthBar = null;
    }

    public void SetPlayer(PlayerData newPlayer)
    {
        playerData = newPlayer;
        battleHealthBonus = 0;
        RefreshFromData(true);
    }

    public void AddBattleHealth(int amount)
    {
        int healthToAdd = Mathf.Max(0, amount);
        if (healthToAdd == 0)
            return;

        battleHealthBonus += healthToAdd;
        currentHealth += healthToAdd;
        RefreshHealth();
        HealthChanged.Invoke(currentHealth, MaximumHealth);

        if (healthGainAnimation != null)
            StopCoroutine(healthGainAnimation);
        healthGainAnimation = StartCoroutine(AnimateHealthGain());
    }

    private IEnumerator AnimateHealthGain()
    {
        animatedHealthBar = healthFillImage != null
            ? healthFillImage.transform.parent as RectTransform
            : null;
        if (animatedHealthBar == null)
        {
            healthGainAnimation = null;
            yield break;
        }

        healthBarOriginalScale = animatedHealthBar.localScale;
        Vector3 enlargedScale = healthBarOriginalScale * 1.18f;
        const float growDuration = 0.18f;
        const float shrinkDuration = 0.28f;

        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / growDuration));
            animatedHealthBar.localScale = Vector3.LerpUnclamped(healthBarOriginalScale, enlargedScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / shrinkDuration));
            animatedHealthBar.localScale = Vector3.LerpUnclamped(enlargedScale, healthBarOriginalScale, t);
            yield return null;
        }

        animatedHealthBar.localScale = healthBarOriginalScale;
        animatedHealthBar = null;
        healthGainAnimation = null;
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, MaximumHealth);
        RefreshHealth();
        HealthChanged.Invoke(currentHealth, MaximumHealth);
    }

    public void SetCombatDefenses(float armor, float magicResist)
    {
        armorRating = Mathf.Max(0f, armor);
        magicResistRating = Mathf.Max(0f, magicResist);
    }

    public void TakeDamage(int amount, DamageType type = DamageType.Physical)
    {
        SetHealth(currentHealth - CombatDefense.ResolveDamage(amount, type, armorRating, magicResistRating));
    }

    public IEnumerator PlayDamageEffect(int amount, DamageType type = DamageType.Physical)
    {
        int damage = CombatDefense.ResolveDamage(amount, type, armorRating, magicResistRating);
        if (damage == 0)
            yield break;

        RectTransform hitTarget = portraitImage != null
            ? portraitImage.rectTransform
            : transform as RectTransform;
        if (hitTarget == null)
        {
            SetHealth(currentHealth - damage);
            yield break;
        }

        GameObject hitObject = new GameObject("PlayerHit", typeof(RectTransform), typeof(CanvasGroup));
        hitObject.transform.SetParent(hitTarget, false);
        hitObject.transform.SetAsLastSibling();

        RectTransform hitRect = hitObject.GetComponent<RectTransform>();
        hitRect.anchorMin = hitRect.anchorMax = new Vector2(0.5f, 0.5f);
        hitRect.pivot = new Vector2(0.5f, 0.5f);
        hitRect.anchoredPosition = Vector2.zero;
        hitRect.sizeDelta = new Vector2(150f, 150f);
        hitRect.localScale = Vector3.zero;

        CreateDamageImage(hitRect);

        GameObject damageTextObject = new GameObject(
            "DamageText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        damageTextObject.transform.SetParent(hitRect, false);
        RectTransform damageTextRect = damageTextObject.GetComponent<RectTransform>();
        damageTextRect.anchorMin = damageTextRect.anchorMax = new Vector2(0.5f, 0.5f);
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

        SetHealth(currentHealth - damage);

        RectTransform panelRect = transform as RectTransform;
        Vector2 panelStartPosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
        Color portraitStartColor = portraitImage != null ? portraitImage.color : Color.white;
        CanvasGroup hitCanvasGroup = hitObject.GetComponent<CanvasGroup>();
        const float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float hitScale = t < 0.2f
                ? Mathf.Lerp(0f, 1.25f, t / 0.2f)
                : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.2f) / 0.18f));
            hitRect.localScale = Vector3.one * hitScale;
            hitCanvasGroup.alpha = t < 0.65f ? 1f : 1f - ((t - 0.65f) / 0.35f);
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
        Destroy(hitObject);
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
        damageRect.anchorMin = damageRect.anchorMax = new Vector2(0.5f, 0.5f);
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

    public void SetDisplayedCombatValues(
        float healthPoints,
        float healthMultiplier,
        float shieldPoints,
        float shieldMultiplier,
        float damagePoints,
        float damageMultiplier,
        float magicResistPoints,
        float magicResistMultiplier,
        bool showTotals = false)
    {
        if (magicResistMultiplierText != null)
            magicResistMultiplierText.text = FormatDefenseAndMultiplier(
                magicResistPoints, magicResistMultiplier, showTotals);

        if (healthMultiplierText != null)
            healthMultiplierText.text = FormatStatAndMultiplier(
                healthPoints,
                healthMultiplier,
                showTotals
            );

        if (shieldMultiplierText != null)
            shieldMultiplierText.text = FormatDefenseAndMultiplier(
                shieldPoints,
                shieldMultiplier,
                showTotals
            );

        if (damageMultiplierText != null)
            damageMultiplierText.text = FormatStatAndMultiplier(
                damagePoints,
                damageMultiplier,
                showTotals
            );
    }

    [ContextMenu("Refresh From Data")]
    public void RefreshFromData()
    {
        RefreshFromData(false);
    }

    private void RefreshFromData(bool resetHealth)
    {
        if (resetHealth)
            SetCombatDefenses(0f, 0f);

        if (playerData == null)
        {
            if (nameText != null) nameText.text = "Player";
            if (portraitImage != null) portraitImage.enabled = false;
            currentHealth = 0;
            RefreshHealth();
            return;
        }

        if (nameText != null)
            nameText.text = playerData.DisplayName;

        if (portraitImage != null)
        {
            portraitImage.sprite = playerData.Portrait;
            portraitImage.enabled = playerData.Portrait != null;
            portraitImage.preserveAspect = true;
        }

        if (healthMultiplierText != null)
            healthMultiplierText.text = FormatStatAndMultiplier(0f, playerData.HealthMultiplier);
        if (shieldMultiplierText != null)
            shieldMultiplierText.text = FormatStatAndMultiplier(0f, playerData.ShieldMultiplier);
        if (damageMultiplierText != null)
            damageMultiplierText.text = FormatStatAndMultiplier(0f, playerData.DamageMultiplier);
        if (magicResistMultiplierText != null)
            magicResistMultiplierText.text = FormatStatAndMultiplier(0f, playerData.MagicResistMultiplier);

        if (resetHealth || currentHealth <= 0 || currentHealth > MaximumHealth)
            currentHealth = MaximumHealth;

        RefreshHealth();
    }

    private void RefreshHealth()
    {
        float normalized = (float)currentHealth / MaximumHealth;
        if (healthFillImage != null)
        {
            RectTransform fillRect = healthFillImage.rectTransform;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.localScale = new Vector3(normalized, 1f, 1f);
            healthFillImage.type = Image.Type.Simple;
            healthFillImage.fillAmount = 1f;

            Image background = fillRect.parent != null
                ? fillRect.parent.GetComponent<Image>()
                : null;
            if (background != null)
                background.color = Color.black;
        }
        if (healthText != null)
            healthText.text = $"{currentHealth} / {MaximumHealth}";
    }

    private static string FormatStatAndMultiplier(float accumulatedStat, float multiplier, bool showTotal = false)
    {
        float points = Mathf.Max(0f, accumulatedStat);
        float safeMultiplier = Mathf.Max(0f, multiplier);
        if (showTotal)
            return $"<color=#FFFFFF>{points * safeMultiplier:0.##}</color>";

        string operation = $"<color=#FFD34E>{points:0.##}</color> x <color=#72F2C0>{safeMultiplier:0.##}</color>";
        return operation;
    }

    private static string FormatDefenseAndMultiplier(float points, float multiplier, bool showTotal)
    {
        string value = FormatStatAndMultiplier(points, multiplier, showTotal);
        if (!showTotal)
            return value;

        double percent = CombatDefense.Reduction(Mathf.Max(0f, points) * Mathf.Max(0f, multiplier)) * 100d;
        // Truncate so the display cannot round a finite rating up to 100%.
        percent = System.Math.Min(99.9d, System.Math.Floor(percent * 10d) / 10d);
        return value + $" <size=65%>({percent:0.#}%)</size>";
    }
}

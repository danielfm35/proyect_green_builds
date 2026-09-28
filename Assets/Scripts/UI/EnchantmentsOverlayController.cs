using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnchantmentsOverlayController : MonoBehaviour
{
    private const int ModalSortingOrder = 30000;

    [Header("Opening Cost")]
    [SerializeField, Min(0)] private int openingGoldCost = 5;

    private CanvasGroup overlayGroup;
    private GameObject overlay;
    private Coroutine transition;
    private RectTransform choicesRoot;
    private RectTransform grimoireRect;
    private RectTransform openingCostRoot;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI openingCostText;
    private ShopManager shopManager;

    private void Awake()
    {
        GetComponent<Button>()?.onClick.AddListener(Show);
        BuildOverlay();
    }

    private void OnDestroy()
    {
        GetComponent<Button>()?.onClick.RemoveListener(Show);
    }

    public void Show()
    {
        BuildOverlay();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        ResetOffer();
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(FadeOverlay(overlayGroup.alpha, 1f, true));
    }

    public void Hide()
    {
        if (overlay == null) return;
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(FadeOverlay(overlayGroup.alpha, 0f, false));
    }

    private void BuildOverlay()
    {
        if (overlay != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform modalLayer = canvas.transform.Find("ModalLayer");
        Transform parent = modalLayer != null ? modalLayer : canvas.transform;
        if (modalLayer != null)
        {
            Canvas modalCanvas = modalLayer.GetComponent<Canvas>();
            if (modalCanvas == null) modalCanvas = modalLayer.gameObject.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = ModalSortingOrder;

            if (modalLayer.GetComponent<GraphicRaycaster>() == null)
                modalLayer.gameObject.AddComponent<GraphicRaycaster>();
        }

        Transform existing = parent.Find("EnchantmentsOverlay");
        if (existing != null)
        {
            overlay = existing.gameObject;
            overlayGroup = overlay.GetComponent<CanvasGroup>();
            return;
        }

        overlay = new GameObject("EnchantmentsOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Button));
        overlay.layer = canvas.gameObject.layer;
        overlay.transform.SetParent(parent, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect);

        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = new Color(0.015f, 0.01f, 0.035f, 0.97f);
        Button backdropButton = overlay.GetComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(Hide);

        overlayGroup = overlay.GetComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;

        GameObject titleObject = CreateText(overlay.transform, "Title", "GRIMORIO DE ENCANTAMIENTOS", 34f);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -55f);
        titleRect.sizeDelta = new Vector2(700f, 60f);

        GameObject hintObject = CreateText(overlay.transform, "Hint", "Pasa el cursor y haz clic para abrir", 21f);
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 58f);
        hintRect.sizeDelta = new Vector2(650f, 45f);
        TextMeshProUGUI hint = hintObject.GetComponent<TextMeshProUGUI>();
        hint.color = new Color(0.78f, 0.72f, 1f, 1f);
        hintText = hint;

        GameObject flashObject = new GameObject("OpeningFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashObject.layer = overlay.layer;
        flashObject.transform.SetParent(overlay.transform, false);
        Stretch(flashObject.GetComponent<RectTransform>());
        Image flash = flashObject.GetComponent<Image>();
        flash.color = new Color(0.65f, 0.45f, 1f, 0f);
        flash.raycastTarget = false;

        GameObject bookObject = new GameObject("Grimoire", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GrimoirePackInteraction));
        bookObject.layer = overlay.layer;
        bookObject.transform.SetParent(overlay.transform, false);
        RectTransform bookRect = bookObject.GetComponent<RectTransform>();
        grimoireRect = bookRect;
        bookRect.anchorMin = bookRect.anchorMax = new Vector2(0.5f, 0.5f);
        bookRect.pivot = new Vector2(0.5f, 0.5f);
        bookRect.anchoredPosition = Vector2.zero;
        bookRect.sizeDelta = new Vector2(520f, 410f);
        Image bookImage = bookObject.GetComponent<Image>();
        bookImage.sprite = Resources.Load<Sprite>("Images/UI/grimorio");
        bookImage.preserveAspect = true;
        bookImage.color = Color.white;
        Button bookButton = bookObject.GetComponent<Button>();
        bookButton.transition = Selectable.Transition.None;
        bookButton.onClick.AddListener(() => { });
        bookObject.GetComponent<GrimoirePackInteraction>().Initialize(flash, hint, TryPayOpeningCost, RevealChoices);

        GameObject costObject = new GameObject("OpeningCost", typeof(RectTransform));
        costObject.layer = overlay.layer;
        costObject.transform.SetParent(overlay.transform, false);
        openingCostRoot = costObject.GetComponent<RectTransform>();
        openingCostRoot.anchorMin = openingCostRoot.anchorMax = new Vector2(0.5f, 0.5f);
        openingCostRoot.pivot = new Vector2(0.5f, 0.5f);
        openingCostRoot.anchoredPosition = new Vector2(0f, -245f);
        openingCostRoot.sizeDelta = new Vector2(105f, 38f);

        GameObject costIconObject = new GameObject("GoldIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        costIconObject.layer = overlay.layer;
        costIconObject.transform.SetParent(openingCostRoot, false);
        RectTransform costIconRect = costIconObject.GetComponent<RectTransform>();
        costIconRect.anchorMin = costIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        costIconRect.pivot = new Vector2(1f, 0.5f);
        costIconRect.anchoredPosition = new Vector2(-4f, 0f);
        costIconRect.sizeDelta = new Vector2(28f, 28f);
        Image costIcon = costIconObject.GetComponent<Image>();
        costIcon.sprite = Resources.Load<Sprite>("Images/UI/gold");
        costIcon.preserveAspect = true;
        costIcon.raycastTarget = false;

        GameObject costTextObject = CreateText(openingCostRoot, "Value", openingGoldCost.ToString(), 25f);
        RectTransform costTextRect = costTextObject.GetComponent<RectTransform>();
        costTextRect.anchorMin = costTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        costTextRect.pivot = new Vector2(0f, 0.5f);
        costTextRect.anchoredPosition = new Vector2(4f, 0f);
        costTextRect.sizeDelta = new Vector2(48f, 36f);
        openingCostText = costTextObject.GetComponent<TextMeshProUGUI>();
        openingCostText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject choicesObject = new GameObject("EnchantmentChoices", typeof(RectTransform));
        choicesObject.layer = overlay.layer;
        choicesObject.transform.SetParent(overlay.transform, false);
        choicesRoot = choicesObject.GetComponent<RectTransform>();
        choicesRoot.anchorMin = choicesRoot.anchorMax = new Vector2(0.5f, 0.5f);
        choicesRoot.pivot = new Vector2(0.5f, 0.5f);
        choicesRoot.anchoredPosition = new Vector2(0f, -8f);
        choicesRoot.sizeDelta = new Vector2(1100f, 500f);
        choicesObject.SetActive(false);

        GameObject closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObject.layer = overlay.layer;
        closeObject.transform.SetParent(overlay.transform, false);
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-28f, -28f);
        closeRect.sizeDelta = new Vector2(62f, 62f);
        closeObject.GetComponent<Image>().color = new Color(0.48f, 0.12f, 0.18f, 0.96f);
        closeObject.GetComponent<Button>().onClick.AddListener(Hide);
        GameObject closeLabel = CreateText(closeObject.transform, "Label", "X", 30f);
        Stretch(closeLabel.GetComponent<RectTransform>());

        overlay.SetActive(false);
    }

    private void ResetOffer()
    {
        if (choicesRoot != null)
        {
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
                Destroy(choicesRoot.GetChild(i).gameObject);
            choicesRoot.gameObject.SetActive(false);
        }
        if (grimoireRect != null)
        {
            grimoireRect.gameObject.SetActive(true);
            grimoireRect.anchoredPosition = Vector2.zero;
            grimoireRect.localScale = Vector3.one;
            grimoireRect.localEulerAngles = Vector3.zero;
        }
        if (openingCostRoot != null) openingCostRoot.gameObject.SetActive(true);
        UpdateOpeningCostVisual();
        if (hintText != null) hintText.text = "Pasa el cursor y haz clic para abrir";
    }

    private bool TryPayOpeningCost()
    {
        if (shopManager == null) shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager == null)
        {
            if (hintText != null) hintText.text = "No se encontró el contador de oro";
            return false;
        }

        if (!shopManager.TrySpendGold(openingGoldCost))
        {
            if (hintText != null) hintText.text = $"Oro insuficiente: necesitas {openingGoldCost}";
            UpdateOpeningCostVisual();
            return false;
        }

        UpdateOpeningCostVisual();
        return true;
    }

    private void UpdateOpeningCostVisual()
    {
        if (openingCostText == null) return;
        if (shopManager == null) shopManager = FindFirstObjectByType<ShopManager>();

        openingCostText.text = openingGoldCost.ToString();
        bool canAfford = shopManager == null || shopManager.CanSpendGold(openingGoldCost);
        openingCostText.color = canAfford
            ? new Color(1f, 0.84f, 0.21f, 1f)
            : new Color(1f, 0.3f, 0.28f, 1f);
    }

    private void RevealChoices()
    {
        StartCoroutine(ShowChoices());
    }

    private IEnumerator ShowChoices()
    {
        EnchantmentData[] all = Resources.LoadAll<EnchantmentData>("ScriptableObjects/Enchantments");
        if (all.Length == 0)
        {
            hintText.text = "No hay encantamientos disponibles";
            yield break;
        }

        var pool = new List<EnchantmentData>(all);
        for (int i = 0; i < pool.Count; i++)
        {
            int swap = Random.Range(i, pool.Count);
            EnchantmentData temp = pool[i]; pool[i] = pool[swap]; pool[swap] = temp;
        }

        grimoireRect.gameObject.SetActive(false);
        if (openingCostRoot != null) openingCostRoot.gameObject.SetActive(false);
        choicesRoot.gameObject.SetActive(true);
        hintText.text = "Elige un encantamiento";
        int count = Mathf.Min(3, pool.Count);
        float spacing = 350f;
        for (int i = 0; i < count; i++)
        {
            RectTransform card = CreateCard(pool[i], i, count, spacing);
            card.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.18f);
                float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.13f;
                card.localScale = Vector3.one * (t * overshoot);
                yield return null;
            }
            card.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.07f);
        }
    }

    private RectTransform CreateCard(EnchantmentData data, int index, int count, float spacing)
    {
        GameObject card = new GameObject("EnchantmentCard_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(EnchantmentCardHover));
        card.layer = overlay.layer;
        card.transform.SetParent(choicesRoot, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((index - (count - 1) * 0.5f) * spacing, 0f);
        rect.sizeDelta = new Vector2(310f, 460f);
        Image background = card.GetComponent<Image>();
        background.color = new Color(0.09f, 0.065f, 0.16f, 0.98f);
        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(0.65f, 0.42f, 1f, 1f);
        outline.effectDistance = new Vector2(4f, -4f);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = card.layer;
        iconObject.transform.SetParent(card.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -22f);
        iconRect.sizeDelta = new Vector2(230f, 205f);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = data.icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject nameObject = CreateText(card.transform, "Name", data.enchantmentName, 25f);
        RectTransform nameRect = nameObject.GetComponent<RectTransform>();
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -230f);
        nameRect.sizeDelta = new Vector2(270f, 55f);
        nameObject.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.84f, 0.3f, 1f);

        GameObject descriptionObject = CreateText(card.transform, "Description", data.DisplayDescription, 17f);
        RectTransform descriptionRect = descriptionObject.GetComponent<RectTransform>();
        descriptionRect.anchorMin = descriptionRect.anchorMax = new Vector2(0.5f, 1f);
        descriptionRect.pivot = new Vector2(0.5f, 1f);
        descriptionRect.anchoredPosition = new Vector2(0f, -292f);
        descriptionRect.sizeDelta = new Vector2(270f, 82f);
        TextMeshProUGUI description = descriptionObject.GetComponent<TextMeshProUGUI>();
        description.fontStyle = FontStyles.Normal;
        description.color = new Color(0.88f, 0.85f, 0.96f, 1f);

        GridPlacementRule modifierRule = GridPlacementRule.None;
        IReadOnlyList<GridPlacementRule> modifierRules = GridPlacementRuleUtility.GetSelectableRules();
        if (modifierRules.Count > 0)
        {
            modifierRule = modifierRules[Random.Range(0, modifierRules.Count)];
            GameObject modifierObject = new GameObject("PlacementModifier_" + modifierRule, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            modifierObject.layer = card.layer;
            modifierObject.transform.SetParent(card.transform, false);

            RectTransform modifierRect = modifierObject.GetComponent<RectTransform>();
            modifierRect.anchorMin = modifierRect.anchorMax = new Vector2(0.5f, 0f);
            modifierRect.pivot = new Vector2(0.5f, 0.5f);
            modifierRect.anchoredPosition = new Vector2(0f, 42f);
            modifierRect.sizeDelta = new Vector2(58f, 58f);

            Image modifierImage = modifierObject.GetComponent<Image>();
            modifierImage.sprite = GridPlacementRuleUtility.LoadRuleSprite(modifierRule);
            modifierImage.preserveAspect = true;
            modifierImage.raycastTarget = false;
        }

        EnchantmentInstance offeredEnchantment = new EnchantmentInstance(data, modifierRule);
        card.GetComponent<Button>().onClick.AddListener(() => SelectEnchantment(offeredEnchantment, card.transform));
        return rect;
    }

    private void SelectEnchantment(EnchantmentInstance selected, Transform selectedCard)
    {
        ChestStorageManager storage = FindFirstObjectByType<ChestStorageManager>();
        if (storage == null || !storage.TryStoreEnchantment(selected))
        {
            hintText.text = "No hay espacio para más encantamientos";
            return;
        }

        for (int i = 0; i < choicesRoot.childCount; i++)
        {
            Transform card = choicesRoot.GetChild(i);
            Button button = card.GetComponent<Button>();
            if (button != null) button.interactable = false;
            Image image = card.GetComponent<Image>();
            if (image != null && card != selectedCard) image.color = new Color(0.04f, 0.035f, 0.07f, 0.45f);
        }
        selectedCard.localScale = Vector3.one * 1.1f;
        hintText.text = "Guardado en el baúl: " + selected.data.enchantmentName;
        Debug.Log("[Enchantments] Selected: " + selected.data.enchantmentName + " / " + selected.targetRule, this);

        ChestInventoryToggle chestToggle = FindFirstObjectByType<ChestInventoryToggle>();
        RectTransform chestTarget = chestToggle != null ? chestToggle.EnchantmentTarget : null;
        ChestStorageLightEffect.Play(
            this,
            selectedCard as RectTransform,
            chestTarget,
            new Color(0.68f, 0.3f, 1f, 0.95f),
            Hide);
    }

    private IEnumerator FadeOverlay(float from, float to, bool showing)
    {
        overlayGroup.interactable = showing;
        overlayGroup.blocksRaycasts = showing;
        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            overlayGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        overlayGroup.alpha = to;
        if (!showing) overlay.SetActive(false);
        transition = null;
    }

    private static GameObject CreateText(Transform parent, string name, string value, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public class GrimoirePackInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform rect;
    private Image flash;
    private TextMeshProUGUI hint;
    private Vector3 targetScale = Vector3.one;
    private bool opening;
    private System.Func<bool> tryOpen;
    private System.Action opened;

    public void Initialize(Image openingFlash, TextMeshProUGUI hintText, System.Func<bool> onTryOpen, System.Action onOpened)
    {
        flash = openingFlash;
        hint = hintText;
        tryOpen = onTryOpen;
        opened = onOpened;
    }

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    private void Update()
    {
        if (!opening)
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!opening) targetScale = Vector3.one * 1.13f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!opening) targetScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (opening) return;
        if (tryOpen != null && !tryOpen.Invoke())
        {
            targetScale = Vector3.one;
            return;
        }

        StartCoroutine(OpenLikePack());
    }

    private IEnumerator OpenLikePack()
    {
        opening = true;
        if (hint != null) hint.text = "Mantén la mirada...";
        Vector2 origin = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < 0.48f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.48f);
            float shake = Mathf.Lerp(3f, 15f, t);
            rect.anchoredPosition = origin + Random.insideUnitCircle * shake;
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 38f) * Mathf.Lerp(3f, 10f, t));
            rect.localScale = Vector3.one * Mathf.Lerp(1.13f, 0.92f, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.18f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.18f);
            rect.localScale = new Vector3(Mathf.Lerp(0.92f, 1.42f, t), Mathf.Lerp(0.92f, 0.08f, t), 1f);
            if (flash != null) flash.color = new Color(0.72f, 0.52f, 1f, t * 0.92f);
            yield return null;
        }

        SpawnBurst();
        elapsed = 0f;
        while (elapsed < 0.42f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.42f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rect.localScale = Vector3.one * Mathf.Lerp(1.45f, 1.05f, eased);
            rect.localEulerAngles = new Vector3(0f, Mathf.Lerp(18f, 0f, eased), 0f);
            if (flash != null) flash.color = new Color(0.72f, 0.52f, 1f, Mathf.Lerp(0.92f, 0f, t));
            yield return null;
        }

        rect.anchoredPosition = origin;
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
        targetScale = Vector3.one;
        if (hint != null) hint.text = "¡Encantamiento revelado!  •  Haz clic para abrir otra vez";
        opening = false;
        if (opened != null) opened.Invoke();
    }

    private void SpawnBurst()
    {
        for (int i = 0; i < 18; i++)
        {
            GameObject spark = new GameObject("MagicSpark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            spark.layer = gameObject.layer;
            spark.transform.SetParent(transform.parent, false);
            RectTransform sparkRect = spark.GetComponent<RectTransform>();
            sparkRect.anchorMin = sparkRect.anchorMax = new Vector2(0.5f, 0.5f);
            sparkRect.anchoredPosition = Random.insideUnitCircle * Random.Range(150f, 330f);
            sparkRect.sizeDelta = Vector2.one * Random.Range(7f, 18f);
            Image image = spark.GetComponent<Image>();
            image.color = Color.Lerp(new Color(0.45f, 0.2f, 1f, 0.9f), new Color(1f, 0.86f, 0.25f, 0.95f), Random.value);
            image.raycastTarget = false;
            Destroy(spark, 0.7f);
        }
    }
}

public class EnchantmentCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 target = Vector3.one;

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GetComponent<Button>().interactable) target = Vector3.one * 1.075f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GetComponent<Button>().interactable) target = Vector3.one;
    }
}

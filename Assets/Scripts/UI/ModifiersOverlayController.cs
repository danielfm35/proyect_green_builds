using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ModifiersOverlayController : MonoBehaviour
{
    private const int ModalSortingOrder = 30000;

    [Header("Opening Cost")]
    [SerializeField, Min(0)] private int openingGoldCost = 2;

    private GameObject overlay;
    private CanvasGroup overlayGroup;
    private RectTransform tongsRect;
    private RectTransform costRoot;
    private RectTransform choicesRoot;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI costText;
    private ShopManager shopManager;
    private Coroutine transition;
    private bool transferring;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Show);
        BuildOverlay();
    }

    private void OnEnable()
    {
        Button button = GetComponent<Button>();
        button.onClick.RemoveListener(Show);
        button.onClick.AddListener(Show);
    }

    private void OnDestroy()
    {
        GetComponent<Button>()?.onClick.RemoveListener(Show);
    }

    public void Show()
    {
        if (transferring) return;
        BuildOverlay();
        if (overlay == null) return;
        ResetOffer();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(Fade(overlayGroup.alpha, 1f, true));
    }

    public void Hide()
    {
        if (transferring) return;
        if (overlay == null) return;
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(Fade(overlayGroup.alpha, 0f, false));
    }

    private void BuildOverlay()
    {
        if (overlay != null) return;

        Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>()?.rootCanvas;
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

        Transform existing = parent.Find("ModifiersOverlay");
        if (existing != null)
        {
            overlay = existing.gameObject;
            overlayGroup = overlay.GetComponent<CanvasGroup>();
            return;
        }

        overlay = new GameObject("ModifiersOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Button));
        overlay.layer = canvas.gameObject.layer;
        overlay.transform.SetParent(parent, false);
        Stretch(overlay.GetComponent<RectTransform>());

        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = new Color(0.005f, 0.035f, 0.018f, 0.97f);
        Button backdropButton = overlay.GetComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(Hide);

        overlayGroup = overlay.GetComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;

        TextMeshProUGUI title = CreateText(overlay.transform, "Title", "FORJA DE MODIFICADORES", 34f);
        ConfigureRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(720f, 60f));

        hintText = CreateText(overlay.transform, "Hint", "Pasa el cursor y haz clic para abrir", 21f);
        hintText.color = new Color(0.66f, 1f, 0.76f, 1f);
        hintText.rectTransform.anchorMin = hintText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hintText.rectTransform.pivot = new Vector2(0.5f, 0f);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, 58f);
        hintText.rectTransform.sizeDelta = new Vector2(650f, 45f);

        GameObject flashObject = new GameObject("OpeningFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashObject.layer = overlay.layer;
        flashObject.transform.SetParent(overlay.transform, false);
        Stretch(flashObject.GetComponent<RectTransform>());
        Image flash = flashObject.GetComponent<Image>();
        flash.color = new Color(0.2f, 1f, 0.46f, 0f);
        flash.raycastTarget = false;

        GameObject tongsObject = new GameObject("Tongs", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ModifierPackInteraction));
        tongsObject.layer = overlay.layer;
        tongsObject.transform.SetParent(overlay.transform, false);
        tongsRect = tongsObject.GetComponent<RectTransform>();
        ConfigureRect(tongsRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f));
        Image tongsImage = tongsObject.GetComponent<Image>();
        tongsImage.sprite = Resources.Load<Sprite>("Images/UI/tongs");
        tongsImage.preserveAspect = true;
        tongsObject.GetComponent<Button>().transition = Selectable.Transition.None;
        tongsObject.GetComponent<ModifierPackInteraction>().Initialize(flash, hintText, TryPay, RevealChoices);

        costRoot = new GameObject("OpeningCost", typeof(RectTransform)).GetComponent<RectTransform>();
        costRoot.gameObject.layer = overlay.layer;
        costRoot.SetParent(overlay.transform, false);
        ConfigureRect(costRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, -245f), new Vector2(105f, 38f));

        Image gold = CreateImage(costRoot, "Gold", Resources.Load<Sprite>("Images/UI/gold"));
        ConfigureRect(gold.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-18f, 0f), new Vector2(28f, 28f));
        costText = CreateText(costRoot, "Value", openingGoldCost.ToString(), 25f);
        ConfigureRect(costText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(48f, 36f));
        costText.color = new Color(0.45f, 1f, 0.58f, 1f);

        choicesRoot = new GameObject("ModifierChoices", typeof(RectTransform)).GetComponent<RectTransform>();
        choicesRoot.gameObject.layer = overlay.layer;
        choicesRoot.SetParent(overlay.transform, false);
        ConfigureRect(choicesRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(1100f, 500f));
        choicesRoot.gameObject.SetActive(false);

        GameObject closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObject.layer = overlay.layer;
        closeObject.transform.SetParent(overlay.transform, false);
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-28f, -28f);
        closeRect.sizeDelta = new Vector2(62f, 62f);
        closeObject.GetComponent<Image>().color = new Color(0.13f, 0.42f, 0.23f, 0.98f);
        closeObject.GetComponent<Button>().onClick.AddListener(Hide);
        TextMeshProUGUI closeLabel = CreateText(closeObject.transform, "Label", "X", 30f);
        Stretch(closeLabel.rectTransform);

        overlay.SetActive(false);
    }

    private bool TryPay()
    {
        if (shopManager == null) shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null && shopManager.TrySpendGold(openingGoldCost)) return true;
        hintText.text = $"Oro insuficiente: necesitas {openingGoldCost}";
        return false;
    }

    private void ResetOffer()
    {
        if (choicesRoot != null)
        {
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
                Destroy(choicesRoot.GetChild(i).gameObject);
            choicesRoot.gameObject.SetActive(false);
        }
        if (tongsRect != null)
        {
            tongsRect.gameObject.SetActive(true);
            tongsRect.anchoredPosition = Vector2.zero;
            tongsRect.localScale = Vector3.one;
            tongsRect.localEulerAngles = Vector3.zero;
        }
        if (costRoot != null) costRoot.gameObject.SetActive(true);
        if (costText != null) costText.text = openingGoldCost.ToString();
        if (hintText != null) hintText.text = "Pasa el cursor y haz clic para abrir";
    }

    private void RevealChoices()
    {
        StartCoroutine(ShowChoices());
    }

    private IEnumerator ShowChoices()
    {
        ModifierData[] all = Resources.LoadAll<ModifierData>("ScriptableObjects/Modifiers");
        if (all.Length == 0)
        {
            hintText.text = "No hay modificadores disponibles";
            yield break;
        }

        List<ModifierData> pool = new List<ModifierData>(all);
        for (int i = 0; i < pool.Count; i++)
        {
            int swap = Random.Range(i, pool.Count);
            ModifierData temp = pool[i]; pool[i] = pool[swap]; pool[swap] = temp;
        }

        tongsRect.gameObject.SetActive(false);
        costRoot.gameObject.SetActive(false);
        choicesRoot.gameObject.SetActive(true);
        hintText.text = "Elige un modificador";
        int count = Mathf.Min(3, pool.Count);
        for (int i = 0; i < count; i++)
        {
            RectTransform card = CreateCard(pool[i], i, count);
            card.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.18f);
                card.localScale = Vector3.one * (t * (1f + Mathf.Sin(t * Mathf.PI) * 0.13f));
                yield return null;
            }
            card.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.07f);
        }
    }

    private RectTransform CreateCard(ModifierData data, int index, int count)
    {
        GameObject card = new GameObject("ModifierCard_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        card.layer = overlay.layer;
        card.transform.SetParent(choicesRoot, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        ConfigureRect(rect, new Vector2(0.5f, 0.5f), new Vector2((index - (count - 1) * 0.5f) * 350f, 0f), new Vector2(310f, 460f));
        card.GetComponent<Image>().color = new Color(0.035f, 0.14f, 0.075f, 0.99f);
        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(0.25f, 1f, 0.48f, 1f);
        outline.effectDistance = new Vector2(4f, -4f);

        Image icon = CreateImage(card.transform, "Icon", data.icon);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        icon.rectTransform.pivot = new Vector2(0.5f, 1f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, -28f);
        icon.rectTransform.sizeDelta = new Vector2(220f, 200f);

        TextMeshProUGUI nameText = CreateText(card.transform, "Name", data.modifierName, 25f);
        nameText.color = new Color(0.5f, 1f, 0.62f, 1f);
        nameText.rectTransform.anchorMin = nameText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        nameText.rectTransform.pivot = new Vector2(0.5f, 1f);
        nameText.rectTransform.anchoredPosition = new Vector2(0f, -245f);
        nameText.rectTransform.sizeDelta = new Vector2(270f, 55f);

        TextMeshProUGUI description = CreateText(card.transform, "Description", data.description, 18f);
        description.fontStyle = FontStyles.Normal;
        description.color = new Color(0.86f, 0.96f, 0.89f, 1f);
        description.enableWordWrapping = true;
        description.rectTransform.anchorMin = description.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        description.rectTransform.pivot = new Vector2(0.5f, 1f);
        description.rectTransform.anchoredPosition = new Vector2(0f, -310f);
        description.rectTransform.sizeDelta = new Vector2(270f, 105f);

        card.GetComponent<Button>().onClick.AddListener(() => SelectModifier(data, card.transform));
        return rect;
    }

    private void SelectModifier(ModifierData data, Transform selectedCard)
    {
        if (transferring) return;
        ChestStorageManager storage = FindFirstObjectByType<ChestStorageManager>();
        ModifierInstance instance = new ModifierInstance(data);
        if (storage == null || !storage.TryStoreModifier(instance))
        {
            hintText.text = "No hay espacio para más modificadores";
            return;
        }

        for (int i = 0; i < choicesRoot.childCount; i++)
        {
            Button button = choicesRoot.GetChild(i).GetComponent<Button>();
            if (button != null) button.interactable = false;
        }
        hintText.text = "Guardado en el baúl: " + data.modifierName;
        ChestInventoryToggle chest = FindFirstObjectByType<ChestInventoryToggle>();
        RectTransform chestTarget = chest != null ? chest.ModifierTarget : null;
        Image destinationIcon = null;
        foreach (ChestSlotUI slot in storage.GetComponentsInChildren<ChestSlotUI>(true))
        {
            if (slot.CurrentModifier != instance || !slot.gameObject.activeInHierarchy) continue;
            ModifierDragVisual visual = slot.GetComponentInChildren<ModifierDragVisual>();
            if (visual != null) destinationIcon = visual.GetComponent<Image>();
            chestTarget = destinationIcon != null ? destinationIcon.rectTransform : slot.transform as RectTransform;
            break;
        }
        if (transition != null) StopCoroutine(transition);
        transition = null;
        transferring = true;
        overlayGroup.interactable = false;
        hintText.text = "";
        ModifierTransferEffect.Play(selectedCard.Find("Icon").GetComponent<Image>(),
            chestTarget, destinationIcon, overlayGroup, () =>
            {
                if (this == null) return;
                transferring = false;
                Hide();
            });
    }

    private IEnumerator Fade(float from, float to, bool showing)
    {
        overlayGroup.interactable = showing;
        overlayGroup.blocksRaycasts = showing;
        float elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / 0.22f), 3f);
            overlayGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        overlayGroup.alpha = to;
        if (!showing) overlay.SetActive(false);
        transition = null;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size)
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
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public sealed class ModifierPackInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private RectTransform rect;
    private ModifierForgeBurst burst;
    private TextMeshProUGUI hint;
    private System.Func<bool> tryOpen;
    private System.Action opened;
    private Vector3 targetScale = Vector3.one;
    private bool opening;

    public void Initialize(Image openingFlash, TextMeshProUGUI hintText, System.Func<bool> onTryOpen, System.Action onOpened)
    {
        openingFlash.color = Color.clear;
        var go = new GameObject("EmeraldRadialBurst", typeof(RectTransform), typeof(CanvasRenderer), typeof(ModifierForgeBurst));
        go.layer = openingFlash.gameObject.layer;
        go.transform.SetParent(openingFlash.transform, false);
        var area = (RectTransform)go.transform;
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = area.offsetMax = Vector2.zero;
        burst = go.GetComponent<ModifierForgeBurst>();
        burst.raycastTarget = false;
        hint = hintText;
        tryOpen = onTryOpen;
        opened = onOpened;
    }

    private void Awake() { rect = (RectTransform)transform; }
    private void Update()
    {
        if (!opening) rect.localScale = Vector3.Lerp(rect.localScale, targetScale, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
    }
    public void OnPointerEnter(PointerEventData eventData) { if (!opening) targetScale = Vector3.one * 1.13f; }
    public void OnPointerExit(PointerEventData eventData) { if (!opening) targetScale = Vector3.one; }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (opening || (tryOpen != null && !tryOpen.Invoke())) return;
        StartCoroutine(Open());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (burst != null) burst.Clear();
        opening = false;
        targetScale = Vector3.one;
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
        Image icon = GetComponent<Image>();
        if (icon != null) icon.color = Color.white;
    }

    private IEnumerator Open()
    {
        opening = true;
        if (hint != null) hint.text = "Forjando modificadores...";
        Vector2 origin = rect.anchoredPosition;
        Image icon = GetComponent<Image>();
        Color originalColor = icon.color;
        try
        {
            float elapsed = 0f;
            while (elapsed < 0.48f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.48f);
                rect.anchoredPosition = origin + Random.insideUnitCircle * Mathf.Lerp(3f, 15f, t);
                rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 38f) * Mathf.Lerp(3f, 10f, t));
                rect.localScale = Vector3.one * Mathf.Lerp(1.13f, 0.92f, t);
                yield return null;
            }

            // Keep the enchantment pack's squash and spring-back; replace only its flash.
            elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.18f);
                rect.localScale = new Vector3(Mathf.Lerp(0.92f, 1.42f, t), Mathf.Lerp(0.92f, 0.08f, t), 1f);
                if (burst != null) burst.Show(t * 0.12f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.42f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.42f);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.localScale = Vector3.one * Mathf.Lerp(1.45f, 1.05f, eased);
                rect.localEulerAngles = new Vector3(0f, Mathf.Lerp(18f, 0f, eased), 0f);
                if (burst != null) burst.Show(Mathf.Lerp(0.12f, 1f, t));
                yield return null;
            }
        }
        finally
        {
            if (burst != null) burst.Clear();
            rect.anchoredPosition = origin;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            icon.color = originalColor;
            targetScale = Vector3.one;
            opening = false;
        }
        opened?.Invoke();
    }
}

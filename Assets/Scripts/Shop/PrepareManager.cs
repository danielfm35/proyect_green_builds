using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class PrepareManager : MonoBehaviour
{
    [Header("Hierarchy")]
    [SerializeField] private string bottomTrayName = "BottomTray";
    [SerializeField] private string uiRootName = "UI_Root";

    [Header("Button")]
    [SerializeField] private Vector2 buttonDefaultPosition = new(720f, 100f);
    [SerializeField] private Vector2 buttonDefaultSize = new(190f, 86f);
    [SerializeField] private Color buttonColor = new(0.54165184f, 0.5808816f, 0.6415094f, 1f);

    [Header("Modal")]
    [SerializeField] private Vector2 modalDefaultSize = new(390f, 820f);
    [SerializeField] private Vector2 modalHiddenPosition = new(430f, 30f);
    [SerializeField] private Vector2 modalVisiblePosition = new(-40f, 30f);
    [SerializeField] private Color modalColor = new(0f, 0f, 0f, 1f);
    [SerializeField] private Color modalButtonColor = new(0.2f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color closeButtonColor = new(0.75f, 0.18f, 0.18f, 1f);
    [SerializeField, Min(0.01f)] private float animationDuration = 0.28f;

    [Header("Scenes")]
    [SerializeField] private string battleSceneName = "BattleScene";

    private Button prepareButton;
    private RectTransform prepareModalRect;
    private CanvasGroup prepareModalCanvasGroup;
    private Coroutine prepareModalAnimation;
    private readonly List<PrepareSlotUI> prepareSlots = new();

#if UNITY_EDITOR
    private void OnEnable()
    {
        QueueSetupInEditMode();
    }

    private void OnValidate()
    {
        QueueSetupInEditMode();
    }

    private void QueueSetupInEditMode()
    {
        if (Application.isPlaying)
            return;

        EditorApplication.delayCall -= SetupInEditMode;
        EditorApplication.delayCall += SetupInEditMode;
    }

    private void SetupInEditMode()
    {
        if (this == null || Application.isPlaying)
            return;

        EnsurePrepareButton();
        EnsurePrepareModal();
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        EnsurePrepareButton();
        EnsurePrepareModal();
    }

    private void EnsurePrepareButton()
    {
        Transform bottomTray = GameObject.Find(bottomTrayName)?.transform;
        if (bottomTray == null)
            return;

        GameObject buttonObject = GameObject.Find("PrepareButton");
        bool createdButton = buttonObject == null;
        if (buttonObject == null)
            buttonObject = new GameObject("PrepareButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));

        buttonObject.transform.SetParent(bottomTray, false);
        buttonObject.transform.SetAsLastSibling();

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null)
            buttonRect = buttonObject.AddComponent<RectTransform>();

        if (createdButton)
        {
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = buttonDefaultPosition;
            buttonRect.sizeDelta = buttonDefaultSize;
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
        }

        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = buttonColor;
        buttonImage.type = Image.Type.Sliced;

        prepareButton = buttonObject.GetComponent<Button>();
        if (prepareButton == null)
            prepareButton = buttonObject.AddComponent<Button>();
        prepareButton.targetGraphic = buttonImage;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        if (layout == null)
            layout = buttonObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        layout.preferredWidth = buttonDefaultSize.x;
        layout.preferredHeight = buttonDefaultSize.y;
        layout.layoutPriority = 1;

        EnsurePrepareButtonText(buttonObject.transform);

        prepareButton.onClick.RemoveListener(ShowPrepareModal);
        prepareButton.onClick.AddListener(ShowPrepareModal);
    }

    private void EnsurePrepareButtonText(Transform buttonTransform)
    {
        Transform textTransform = buttonTransform.Find("PrepareButtonText");
        GameObject textObject = textTransform != null
            ? textTransform.gameObject
            : new GameObject("PrepareButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonTransform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (textRect == null)
            textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "Preparar";
        text.raycastTarget = false;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
    }

    private void EnsurePrepareModal()
    {
        Transform uiRoot = GameObject.Find(uiRootName)?.transform;
        if (uiRoot == null)
            return;

        GameObject modalObject = GameObject.Find("PrepareModal");
        bool createdModal = modalObject == null;
        if (modalObject == null)
            modalObject = new GameObject("PrepareModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));

        modalObject.transform.SetParent(uiRoot, false);
        modalObject.transform.SetAsLastSibling();

        prepareModalRect = modalObject.GetComponent<RectTransform>();
        if (prepareModalRect == null)
            prepareModalRect = modalObject.AddComponent<RectTransform>();

        if (createdModal)
        {
            prepareModalRect.anchorMin = new Vector2(1f, 0.5f);
            prepareModalRect.anchorMax = new Vector2(1f, 0.5f);
            prepareModalRect.pivot = new Vector2(1f, 0.5f);
            prepareModalRect.sizeDelta = modalDefaultSize;
            prepareModalRect.anchoredPosition = modalHiddenPosition;
        }

        Image modalImage = modalObject.GetComponent<Image>();
        if (modalImage == null)
            modalImage = modalObject.AddComponent<Image>();
        modalImage.color = modalColor;

        prepareModalCanvasGroup = modalObject.GetComponent<CanvasGroup>();
        if (prepareModalCanvasGroup == null)
            prepareModalCanvasGroup = modalObject.AddComponent<CanvasGroup>();

        if (createdModal)
        {
            prepareModalCanvasGroup.alpha = 0f;
            prepareModalCanvasGroup.interactable = false;
            prepareModalCanvasGroup.blocksRaycasts = false;
        }

        EnsurePrepareModalText(modalObject.transform);
        EnsurePrepareModalButtons(modalObject.transform);
        EnsurePrepareSlots(modalObject.transform);
    }

    private void EnsurePrepareSlots(Transform modalTransform)
    {
        prepareSlots.Clear();

        ConfigurePrepareSlot(modalTransform, "IventarySlot_01", PrepareSlotKind.Helmet);
        ConfigurePrepareSlot(modalTransform, "IventarySlot_02", PrepareSlotKind.Armor);
        ConfigurePrepareSlot(modalTransform, "IventarySlot_03", PrepareSlotKind.Shoulder);
        ConfigurePrepareSlot(modalTransform, "IventarySlot_04", PrepareSlotKind.Weapon);
        ConfigurePrepareSlot(modalTransform, "IventarySlot_05", PrepareSlotKind.Gloves);
        ConfigurePrepareSlot(modalTransform, "IventarySlot_06", PrepareSlotKind.Shield);
        ConfigurePrepareSlot(modalTransform, "IventarySlot_07", PrepareSlotKind.Boots);
    }

    private void ConfigurePrepareSlot(Transform modalTransform, string slotName, PrepareSlotKind slotKind)
    {
        Transform slotTransform = FindChildRecursive(modalTransform, slotName);
        if (slotTransform == null)
            return;

        PrepareSlotUI prepareSlot = slotTransform.GetComponent<PrepareSlotUI>();
        if (prepareSlot == null)
            prepareSlot = slotTransform.gameObject.AddComponent<PrepareSlotUI>();

        prepareSlot.Configure(slotKind);
        prepareSlots.Add(prepareSlot);
    }

    private void SetPrepareSourceHighlights(bool isVisible)
    {
        for (int i = 0; i < prepareSlots.Count; i++)
        {
            if (prepareSlots[i] != null)
                prepareSlots[i].SetSourceHighlightVisible(isVisible);
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void EnsurePrepareModalText(Transform modalTransform)
    {
        Transform titleTransform = modalTransform.Find("Title");
        GameObject titleObject = titleTransform != null
            ? titleTransform.gameObject
            : new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bool createdTitle = titleTransform == null;
        titleObject.transform.SetParent(modalTransform, false);

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        if (titleRect == null)
            titleRect = titleObject.AddComponent<RectTransform>();

        if (createdTitle)
        {
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -28f);
            titleRect.sizeDelta = new Vector2(-48f, 52f);
        }

        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        if (title == null)
            title = titleObject.AddComponent<TextMeshProUGUI>();
        if (createdTitle)
        {
            title.text = "Preparar";
            title.fontSize = 32f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.black;
            title.raycastTarget = false;
        }
    }

    private void EnsurePrepareModalButtons(Transform modalTransform)
    {
        Button closeButton = EnsureModalButton(
            modalTransform,
            "CloseButton",
            "X",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(72f, 72f),
            closeButtonColor,
            Color.white,
            36f
        );

        closeButton.onClick.RemoveListener(HidePrepareModal);
        closeButton.onClick.AddListener(HidePrepareModal);
        closeButton.transform.SetAsLastSibling();

        Button fightButton = EnsureModalButton(
            modalTransform,
            "FightButton",
            "Luchar",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 74f),
            new Vector2(210f, 78f),
            modalButtonColor,
            Color.white,
            30f
        );

        fightButton.onClick.RemoveListener(OnFightButtonClicked);
        fightButton.onClick.AddListener(OnFightButtonClicked);
        fightButton.transform.SetAsLastSibling();
    }

    private Button EnsureModalButton(
        Transform parent,
        string buttonName,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color backgroundColor,
        Color textColor,
        float fontSize
    )
    {
        Transform buttonTransform = parent.Find(buttonName);
        GameObject buttonObject = buttonTransform != null
            ? buttonTransform.gameObject
            : new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        bool createdButton = buttonTransform == null;
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null)
            buttonRect = buttonObject.AddComponent<RectTransform>();

        if (createdButton)
        {
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.pivot = pivot;
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = size;
        }

        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = buttonObject.AddComponent<Image>();
        if (createdButton)
        {
            buttonImage.color = backgroundColor;
            buttonImage.type = Image.Type.Sliced;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        Transform textTransform = buttonObject.transform.Find("Text");
        GameObject textObject = textTransform != null
            ? textTransform.gameObject
            : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (textRect == null)
            textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        if (textTransform == null)
        {
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = textColor;
        }

        return button;
    }

    public void ShowPrepareModal()
    {
        EnsurePrepareModal();

        if (prepareModalRect == null || prepareModalCanvasGroup == null)
            return;

        if (prepareModalAnimation != null)
            StopCoroutine(prepareModalAnimation);

        SetPrepareSourceHighlights(true);

        prepareModalAnimation = StartCoroutine(AnimatePrepareModal(
            prepareModalRect.anchoredPosition,
            modalVisiblePosition,
            prepareModalCanvasGroup.alpha,
            1f
        ));
    }

    public void HidePrepareModal()
    {
        EnsurePrepareModal();

        if (prepareModalRect == null || prepareModalCanvasGroup == null)
            return;

        if (prepareModalAnimation != null)
            StopCoroutine(prepareModalAnimation);

        SetPrepareSourceHighlights(false);

        prepareModalAnimation = StartCoroutine(AnimatePrepareModal(
            prepareModalRect.anchoredPosition,
            modalHiddenPosition,
            prepareModalCanvasGroup.alpha,
            0f
        ));
    }

    public void OnFightButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogWarning("[PrepareManager] Battle scene name is empty.", this);
            return;
        }

        SceneTransitionManager.LoadScene(battleSceneName);
    }

    private IEnumerator AnimatePrepareModal(Vector2 from, Vector2 to, float alphaFrom, float alphaTo)
    {
        prepareModalCanvasGroup.interactable = true;
        prepareModalCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            prepareModalRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            prepareModalCanvasGroup.alpha = Mathf.Lerp(alphaFrom, alphaTo, t);
            yield return null;
        }

        prepareModalRect.anchoredPosition = to;
        prepareModalCanvasGroup.alpha = alphaTo;
        bool isVisible = alphaTo > 0f;
        prepareModalCanvasGroup.interactable = isVisible;
        prepareModalCanvasGroup.blocksRaycasts = isVisible;
        prepareModalAnimation = null;
    }

}

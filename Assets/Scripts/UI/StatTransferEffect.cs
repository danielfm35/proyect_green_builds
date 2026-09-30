using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A stationary bonus placard followed by a single credit and a counter punch.</summary>
public static class StatTransferEffect
{
    public static IEnumerator Play(RectTransform source, TMP_Text counter, float amount,
        Color tint, Action<float> credit)
    {
        Canvas canvas = source != null ? source.GetComponentInParent<Canvas>()?.rootCanvas : null;
        if (canvas == null || counter == null || amount == 0f)
        {
            credit?.Invoke(amount);
            yield break;
        }

        var root = new GameObject("StatTransfer", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        var layer = (RectTransform)root.transform;
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        var group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform destination = counter.rectTransform;
        Vector3 sourceScale = source.localScale;
        Vector2 origin = Center(layer, source, canvas);
        // Match the original placard's position just above the item's upper-right corner.
        origin += (Vector2)layer.InverseTransformVector(source.TransformVector(
            new Vector3(45f, source.rect.height * 0.5f + 36f, 0f)));

        var cardObject = new GameObject("BonusPlacard", typeof(RectTransform), typeof(CanvasGroup));
        cardObject.transform.SetParent(layer, false);
        var card = (RectTransform)cardObject.transform;
        var cardGroup = cardObject.GetComponent<CanvasGroup>();
        card.sizeDelta = new Vector2(124f, 66f);
        Image shadow = Panel(card, "HardShadow", new Color(0.04f, 0.025f, 0.035f, 0.8f), Vector2.zero);
        shadow.rectTransform.anchoredPosition = new Vector2(5f, -6f);
        Panel(card, "DarkEdge", new Color(0.12f, 0.075f, 0.055f), Vector2.zero);
        Panel(card, "CreamBorder", new Color(1f, 0.94f, 0.78f), Vector2.one * 2f);
        Panel(card, "ColorFace", tint, Vector2.one * 4f);
        Image highlight = Panel(card, "TopBevel", Color.Lerp(tint, Color.white, 0.35f), new Vector2(5f, 5f));
        highlight.rectTransform.anchorMin = new Vector2(0f, 1f);
        highlight.rectTransform.anchorMax = Vector2.one;
        highlight.rectTransform.offsetMin = new Vector2(5f, -8f);
        highlight.rectTransform.offsetMax = new Vector2(-5f, -5f);
        Image bottom = Panel(card, "BottomBevel", Color.Lerp(tint, Color.black, 0.23f), Vector2.one * 5f);
        bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottom.rectTransform.offsetMin = new Vector2(5f, 5f);
        bottom.rectTransform.offsetMax = new Vector2(-5f, 9f);

        var labelObject = new GameObject("Bonus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(card, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = counter.font;
        label.text = amount.ToString("+0.##;-0.##;0");
        label.fontSize = 40f;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = 40f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.98f, 0.9f);
        label.outlineColor = new Color32(65, 35, 12, 220);
        label.outlineWidth = 0.12f;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(8f, 4f);
        label.rectTransform.offsetMax = new Vector2(-8f, -4f);

        const float pop = 0.22f;
        const float hold = 0.22f;
        const float dissolve = 0.16f;
        const float duration = pop + hold + dissolve;
        float elapsed = 0f;
        bool credited = false;
        try
        {
            while (elapsed < duration)
            {
                if (source == null || destination == null) yield break;
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float appear = Mathf.Clamp01(elapsed / pop);
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - pop - hold) / dissolve));
                card.anchoredPosition = origin;
                card.localScale = Vector3.one * BackOut(appear) * (1f - fade * 0.12f);
                card.localRotation = Quaternion.Euler(0f, 0f, -7f * (1f - appear));
                cardGroup.alpha = 1f - fade;
                source.localScale = sourceScale * (1f + Mathf.Sin(appear * Mathf.PI) * 0.09f);
                if (elapsed < duration) yield return null;
            }

            // Apply the entire bonus on the same frame the placard vanishes.
            cardGroup.alpha = 0f;
            credited = true;
            credit?.Invoke(amount);
            yield return PulseCounter(counter);
        }
        finally
        {
            if (source != null) source.localScale = sourceScale;
            if (root != null) UnityEngine.Object.Destroy(root);
            if (!credited) credit?.Invoke(amount);
        }
    }

    public static IEnumerator PulseCounter(TMP_Text counter)
    {
        if (counter == null) yield break;
        RectTransform rect = counter.rectTransform;
        Vector3 originalScale = rect.localScale;
        const float grow = 0.085f;
        const float duration = 0.36f;
        float elapsed = 0f;
        try
        {
            while (elapsed < duration && rect != null)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float size;
                if (elapsed < grow)
                    size = Mathf.Lerp(1f, 1.28f, 1f - Mathf.Pow(1f - elapsed / grow, 3f));
                else
                {
                    float t = Mathf.Clamp01((elapsed - grow) / (duration - grow));
                    size = 1f + 0.28f * (1f - t) * (1f - t) * Mathf.Cos(t * Mathf.PI);
                }
                rect.localScale = originalScale * size;
                yield return null;
            }
        }
        finally
        {
            if (rect != null) rect.localScale = originalScale;
        }
    }

    private static float BackOut(float t)
    {
        float x = t - 1f;
        return 1f + 2.35f * x * x * x + 1.35f * x * x;
    }

    private static Vector2 Center(RectTransform layer, RectTransform target, Canvas canvas)
    {
        Canvas targetCanvas = target.GetComponentInParent<Canvas>()?.rootCanvas;
        Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
        Camera layerCamera = canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCamera, target.TransformPoint(target.rect.center));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, layerCamera, out Vector2 local);
        return local;
    }

    private static Image Panel(Transform parent, string name, Color color, Vector2 inset)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = inset;
        image.rectTransform.offsetMax = -inset;
        return image;
    }
}

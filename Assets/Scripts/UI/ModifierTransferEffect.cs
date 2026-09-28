using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Self-owned lifetime: closing a modal cannot strand a hidden inventory icon.
public sealed class ModifierTransferEffect : MonoBehaviour
{
    private static Sprite softSprite;
    private static Sprite ringSprite;
    private Image sourceIcon;
    private Image destinationIcon;
    private bool sourceEnabled;
    private bool destinationEnabled;
    private Action completed;
    private readonly Color mint = new Color(0.38f, 1f, 0.72f);

    public static void Play(Image source, RectTransform target, Image destination,
        CanvasGroup modal, Action onComplete)
    {
        Canvas canvas = source != null ? source.GetComponentInParent<Canvas>() : null;
        if (canvas == null || target == null)
        {
            onComplete?.Invoke();
            return;
        }
        var go = new GameObject("ModifierTransferEffect", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasGroup), typeof(ModifierTransferEffect));
        go.layer = canvas.gameObject.layer;
        go.transform.SetParent(canvas.rootCanvas.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var effectCanvas = go.GetComponent<Canvas>();
        effectCanvas.overrideSorting = true;
        effectCanvas.sortingOrder = 31000;
        var group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        var effect = go.GetComponent<ModifierTransferEffect>();
        effect.completed = onComplete;
        effect.StartCoroutine(effect.Animate(source, target, destination, modal));
    }

    private IEnumerator Animate(Image source, RectTransform target, Image destination, CanvasGroup modal)
    {
        EnsureSprites();
        Canvas.ForceUpdateCanvases();
        RectTransform layer = (RectTransform)transform;
        Vector2 start = Center(layer, source.rectTransform);
        Vector2 originalSize = Size(layer, source.rectTransform);
        sourceIcon = source;
        sourceEnabled = source.enabled;
        destinationIcon = destination;
        destinationEnabled = destination != null && destination.enabled;
        source.enabled = false;
        if (destination != null) destination.enabled = false;

        // All graphics are allocated once. The overlapping soft samples form a continuous ribbon.
        const int samples = 112;
        Image[] trail = new Image[samples];
        for (int i = 0; i < samples; i++)
            trail[i] = Graphic("Ribbon", softSprite, mint, Vector2.one * 22f);
        Image halo = Graphic("Halo", softSprite, mint, Vector2.one * 195f);
        Image icon = Graphic("FlyingModifier", source.sprite, source.color, originalSize);
        icon.preserveAspect = true;
        Image ring = Graphic("ArrivalRing", ringSprite, mint, Vector2.one * 60f);
        ring.enabled = false;
        Image[] sparks = new Image[7];
        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i] = Graphic("ArrivalSpark", softSprite, Color.white, new Vector2(5f, 12f));
            sparks[i].enabled = false;
        }
        foreach (Image sample in trail) sample.enabled = false;

        float initialAlpha = modal != null ? modal.alpha : 1f;
        Vector2 lift = start + Vector2.up * 24f;
        float elapsed = 0f;
        const float anticipation = 0.16f;
        while (elapsed < anticipation)
        {
            if (target == null) { Finish(); yield break; }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / anticipation);
            float ease = Smooth(t);
            icon.rectTransform.anchoredPosition = Vector2.Lerp(start, lift, ease);
            icon.rectTransform.localScale = Vector3.one * (1f + 0.055f * Mathf.Sin(t * Mathf.PI));
            halo.rectTransform.anchoredPosition = icon.rectTransform.anchoredPosition;
            Tint(halo, mint, 0.32f * ease);
            if (modal != null) modal.alpha = initialAlpha * (1f - 0.25f * ease);
            yield return null;
        }

        Vector2 end = Center(layer, target);
        float distance = Vector2.Distance(lift, end);
        float duration = Mathf.Clamp(distance / 1250f, 0.48f, 0.78f);
        float arc = Mathf.Clamp(distance * 0.2f, 55f, 165f);
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null) { Finish(); yield break; }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            end = Center(layer, target);
            Vector2 position = Path(lift, end, arc, t);
            icon.rectTransform.anchoredPosition = position;
            icon.rectTransform.sizeDelta = Vector2.Lerp(originalSize, Size(layer, target), Smooth(t));
            icon.rectTransform.localScale = Vector3.one;
            icon.rectTransform.localEulerAngles = new Vector3(0f, 0f,
                -Mathf.Sign(end.x - lift.x) * 10f * Mathf.Sin(t * Mathf.PI));
            halo.rectTransform.anchoredPosition = position;
            halo.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(190f, 95f, t);
            Tint(halo, mint, 0.30f * Mathf.Sin(Mathf.PI * t) + 0.12f);
            for (int i = 0; i < samples; i++)
            {
                float age = (i + 1f) / samples;
                float past = t - age * 0.34f / duration;
                trail[i].enabled = past >= 0f;
                trail[i].rectTransform.anchoredPosition = Path(lift, end, arc, Mathf.Max(0f, past));
                trail[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(68f, 24f, age);
                Tint(trail[i], mint, 0.12f * (1f - age) * Mathf.Clamp01((1f - t) * 7f));
            }
            if (modal != null) modal.alpha = initialAlpha * 0.75f * (1f - Smooth(Mathf.Clamp01(t / 0.6f)));
            yield return null;
        }

        icon.enabled = false;
        if (destinationIcon != null) destinationIcon.enabled = destinationEnabled;
        foreach (Image sample in trail) sample.enabled = false;
        ring.enabled = true;
        elapsed = 0f;
        const float settle = 0.32f;
        while (elapsed < settle)
        {
            if (target == null) { Finish(); yield break; }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settle);
            end = Center(layer, target);
            float release = 1f - Mathf.Pow(1f - t, 3f);
            float diameter = Mathf.Max(Size(layer, target).x, Size(layer, target).y);
            ring.rectTransform.anchoredPosition = end;
            ring.rectTransform.sizeDelta = Vector2.one * diameter * Mathf.Lerp(0.75f, 1.85f, release);
            Tint(ring, mint, 0.65f * (1f - t) * (1f - t));
            halo.rectTransform.anchoredPosition = end;
            halo.rectTransform.sizeDelta = Vector2.one * diameter * Mathf.Lerp(1.4f, 2.3f, release);
            Tint(halo, mint, 0.48f * (1f - t) * (1f - t));
            for (int i = 0; i < sparks.Length; i++)
            {
                float angle = (i * 137.5f + 20f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                sparks[i].enabled = true;
                sparks[i].rectTransform.anchoredPosition = end + direction * diameter * (0.2f + release * 0.7f);
                sparks[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, angle * Mathf.Rad2Deg - 90f);
                Tint(sparks[i], Color.Lerp(mint, Color.white, 0.65f), 0.7f * (1f - t) * (1f - t));
            }
            yield return null;
        }
        Finish();
    }

    private void Finish()
    {
        RestoreIcons();
        Action callback = completed;
        completed = null;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private void OnDestroy() { RestoreIcons(); }

    private void RestoreIcons()
    {
        if (sourceIcon != null) sourceIcon.enabled = sourceEnabled;
        if (destinationIcon != null) destinationIcon.enabled = destinationEnabled;
    }

    private Image Graphic(string label, Sprite sprite, Color tint, Vector2 size)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = tint;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    private static void Tint(Image image, Color color, float alpha)
    {
        color.a = alpha;
        image.color = color;
    }

    private static Vector2 Center(RectTransform layer, RectTransform rect)
    {
        return layer.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
    }

    private static Vector2 Size(RectTransform layer, RectTransform rect)
    {
        Vector3 size = layer.InverseTransformVector(rect.TransformVector(rect.rect.size));
        return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    private static float Smooth(float t) { return t * t * t * (t * (6f * t - 15f) + 10f); }

    private static Vector2 Path(Vector2 start, Vector2 end, float arc, float time)
    {
        float t = Smooth(time);
        Vector2 control = (start + end) * 0.5f + Vector2.up * arc;
        return (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;
    }

    private static void EnsureSprites()
    {
        if (softSprite == null) softSprite = MakeSprite(false);
        if (ringSprite == null) ringSprite = MakeSprite(true);
    }

    private static Sprite MakeSprite(bool ring)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = ring ? "ModifierSoftRing" : "ModifierSoftGlow";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
            float a = ring ? Mathf.Exp(-Mathf.Pow((radius - 0.79f) / 0.038f, 2f))
                : Mathf.Exp(-radius * radius * 6f) * (1f - Smooth(Mathf.Clamp01(radius)));
            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}

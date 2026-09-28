using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class ChestStorageLightEffect
{
    private const int EffectSortingOrder = 31000;

    public static void Play(
        MonoBehaviour runner,
        RectTransform source,
        RectTransform target,
        Color color,
        Action onComplete = null)
    {
        if (runner == null)
        {
            onComplete?.Invoke();
            return;
        }

        runner.StartCoroutine(Animate(source, target, color, onComplete));
    }

    private static IEnumerator Animate(
        RectTransform source,
        RectTransform target,
        Color color,
        Action onComplete)
    {
        if (source == null || target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Canvas canvas = source.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        GameObject layerObject = new GameObject(
            "ChestStorageLightEffect",
            typeof(RectTransform),
            typeof(Canvas));
        layerObject.layer = canvas.gameObject.layer;
        layerObject.transform.SetParent(canvas.rootCanvas.transform, false);

        RectTransform layer = layerObject.GetComponent<RectTransform>();
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = Vector2.zero;
        layer.offsetMax = Vector2.zero;

        Canvas effectCanvas = layerObject.GetComponent<Canvas>();
        effectCanvas.overrideSorting = true;
        effectCanvas.sortingOrder = EffectSortingOrder;

        RectTransform glow = CreateLight(layer, "MovingGlow", color, new Vector2(58f, 58f));
        RectTransform core = CreateLight(glow, "Core", Color.white, new Vector2(18f, 18f));
        core.localEulerAngles = new Vector3(0f, 0f, 45f);

        Vector2 start = WorldToLayer(layer, source.position);
        Vector2 end = WorldToLayer(layer, target.position);
        Vector3 targetOriginalScale = target.localScale;
        float arcHeight = Mathf.Clamp(Vector2.Distance(start, end) * 0.24f, 90f, 260f);
        Vector2 control = (start + end) * 0.5f + Vector2.up * arcHeight;
        var trail = new List<Image>();

        float elapsed = 0f;
        const float travelDuration = 0.72f;
        float trailTimer = 0f;
        while (elapsed < travelDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float linearT = Mathf.Clamp01(elapsed / travelDuration);
            float t = linearT * linearT * (3f - 2f * linearT);
            end = WorldToLayer(layer, target.position);
            Vector2 position = QuadraticBezier(start, control, end, t);
            glow.anchoredPosition = position;
            glow.localScale = Vector3.one * (1f + Mathf.Sin(linearT * Mathf.PI * 8f) * 0.16f);
            glow.localEulerAngles = new Vector3(0f, 0f, linearT * 360f);

            trailTimer += Time.unscaledDeltaTime;
            if (trailTimer >= 0.035f)
            {
                trailTimer = 0f;
                RectTransform spark = CreateLight(layer, "Trail", color, new Vector2(25f, 25f));
                spark.anchoredPosition = position + UnityEngine.Random.insideUnitCircle * 7f;
                spark.localEulerAngles = new Vector3(0f, 0f, 45f);
                trail.Add(spark.GetComponent<Image>());
            }

            for (int i = trail.Count - 1; i >= 0; i--)
            {
                Image spark = trail[i];
                if (spark == null)
                {
                    trail.RemoveAt(i);
                    continue;
                }
                Color sparkColor = spark.color;
                sparkColor.a -= Time.unscaledDeltaTime * 2.7f;
                spark.color = sparkColor;
                spark.rectTransform.localScale += Vector3.one * Time.unscaledDeltaTime * 0.7f;
                if (sparkColor.a <= 0f)
                {
                    UnityEngine.Object.Destroy(spark.gameObject);
                    trail.RemoveAt(i);
                }
            }
            yield return null;
        }

        glow.gameObject.SetActive(false);
        RectTransform impact = CreateLight(layer, "ChestImpact", color, new Vector2(70f, 70f));
        impact.anchoredPosition = end;
        Image impactImage = impact.GetComponent<Image>();
        elapsed = 0f;
        const float impactDuration = 0.34f;
        while (elapsed < impactDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / impactDuration);
            impact.anchoredPosition = WorldToLayer(layer, target.position);
            impact.localScale = Vector3.one * Mathf.Lerp(0.35f, 2.5f, 1f - Mathf.Pow(1f - t, 3f));
            impact.localEulerAngles = new Vector3(0f, 0f, t * 135f);
            Color impactColor = color;
            impactColor.a = 1f - t;
            impactImage.color = impactColor;
            target.localScale = targetOriginalScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.14f);
            yield return null;
        }

        target.localScale = targetOriginalScale;
        UnityEngine.Object.Destroy(layerObject);
        onComplete?.Invoke();
    }

    private static RectTransform CreateLight(Transform parent, string name, Color color, Vector2 size)
    {
        GameObject lightObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lightObject.layer = parent.gameObject.layer;
        lightObject.transform.SetParent(parent, false);
        RectTransform rect = lightObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.localEulerAngles = new Vector3(0f, 0f, 45f);
        Image image = lightObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static Vector2 WorldToLayer(RectTransform layer, Vector3 worldPosition)
    {
        return layer.InverseTransformPoint(worldPosition);
    }

    private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }
}

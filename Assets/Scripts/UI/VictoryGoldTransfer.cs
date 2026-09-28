using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class VictoryGoldTransfer
{
    // One coin represents one gold. All transient graphics are allocated before the flight.
    public static IEnumerator Play(Canvas canvas, RectTransform banner, CanvasGroup bannerGroup,
        Image dimmer, ShopManager shop, int amount, Vector2[] screenOrigins = null)
    {
        if (shop == null || amount <= 0) yield break;
        RectTransform target = shop.GoldTarget;
        if (target == null) { shop.AddGold(amount); yield break; }
        var root = new GameObject("VictoryGoldTransfer", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform layer = (RectTransform)root.transform;
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        bool milestone = screenOrigins != null && screenOrigins.Length > 0;
        Vector2 origin = banner != null
            ? (Vector2)layer.InverseTransformPoint(banner.TransformPoint(new Vector3(0f, -55f)))
            : Vector2.zero;
        Vector3 originalScale = target.localScale;
        Sprite glowSprite = CreateGlow();
        Sprite coinSprite = Resources.Load<Sprite>("Images/UI/gold");
        Image[] coins = new Image[amount];
        Image[] halos = new Image[amount];
        Image[] wakes = new Image[amount];
        Image arrival = Create(layer, "CounterGlow", glowSprite, new Color(1f, 0.72f, 0.2f), 120f);
        arrival.color = Color.clear;
        for (int i = 0; i < amount; i++)
        {
            wakes[i] = Create(layer, "GoldWake", glowSprite, new Color(1f, 0.65f, 0.14f), 50f);
            halos[i] = Create(layer, "CoinHalo", glowSprite, new Color(1f, 0.78f, 0.3f), 70f);
            coins[i] = Create(layer, "RewardCoin_" + i, coinSprite != null ? coinSprite : glowSprite, Color.white, 34f);
            coins[i].preserveAspect = true;
            coins[i].enabled = halos[i].enabled = wakes[i].enabled = false;
        }
        int credited = 0;
        float elapsed = 0f;
        float lastArrival = -10f;
        const float stagger = 0.065f;
        const float gather = 0.19f;
        const float flight = 0.76f;
        float finish = (amount - 1) * stagger + gather + flight;
        try
        {
            while (elapsed < finish + 0.38f)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                Vector2 end = TargetCenter(layer, target);
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.48f));
                if (bannerGroup != null) bannerGroup.alpha = 1f - fade;
                if (banner != null) banner.anchoredPosition = Vector2.up * fade * 12f;
                if (dimmer != null) dimmer.color = new Color(0.01f, 0.025f, 0.06f, Mathf.Lerp(0.42f, 0.16f, fade));
                for (int i = 0; i < amount; i++)
                {
                    float age = elapsed - i * stagger;
                    if (age < 0f) continue;
                    if (age >= gather + flight)
                    {
                        coins[i].enabled = halos[i].enabled = wakes[i].enabled = false;
                        // Arrival order is deterministic, including when a slow frame skips multiple arrivals.
                        if (credited <= i)
                        {
                            shop.AddGold(1);
                            credited++;
                            lastArrival = elapsed;
                        }
                        continue;
                    }
                    coins[i].enabled = halos[i].enabled = wakes[i].enabled = true;
                    if (milestone)
                    {
                        int sourceIndex = Mathf.Min(screenOrigins.Length - 1, i * screenOrigins.Length / amount);
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenOrigins[sourceIndex], null, out origin);
                    }
                    float angle = (70f + i * 137.5f) * Mathf.Deg2Rad;
                    Vector2 spread = new Vector2(Mathf.Cos(angle) * 62f, 35f + Mathf.Sin(angle) * 32f);
                    Vector2 launch = origin + spread;
                    float t = Mathf.Clamp01((age - gather) / flight);
                    Vector2 position = age < gather
                        ? Vector2.Lerp(origin, launch, 1f - Mathf.Pow(1f - age / gather, 3f))
                        : Path(launch, end, i, t);
                    float size = Mathf.Lerp(34f, 20f, t * t);
                    coins[i].rectTransform.anchoredPosition = position;
                    coins[i].rectTransform.sizeDelta = Vector2.one * size;
                    coins[i].rectTransform.localScale = new Vector3(1f - 0.28f * Mathf.Sin(t * Mathf.PI * 2f + i * 0.4f) * Mathf.Sin(t * Mathf.PI), 1f, 1f);
                    coins[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI) * (i % 2 == 0 ? 16f : -16f));
                    float alpha = Mathf.Clamp01(age / 0.065f);
                    coins[i].color = new Color(1f, 1f, 1f, alpha);
                    halos[i].rectTransform.anchoredPosition = position;
                    halos[i].rectTransform.sizeDelta = Vector2.one * size * 2.4f;
                    halos[i].color = new Color(1f, 0.72f, 0.22f, alpha * 0.28f);
                    Vector2 previous = Path(launch, end, i, Mathf.Max(0f, t - 0.045f));
                    Vector2 direction = position - previous;
                    wakes[i].rectTransform.anchoredPosition = (position + previous) * 0.5f;
                    wakes[i].rectTransform.sizeDelta = new Vector2(direction.magnitude + 22f, 12f);
                    wakes[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                    wakes[i].color = new Color(1f, 0.68f, 0.18f, 0.26f * Mathf.Sin(t * Mathf.PI));
                }
                float sinceArrival = elapsed - lastArrival;
                float pulse = Mathf.Exp(-sinceArrival * 12f);
                arrival.rectTransform.anchoredPosition = end;
                arrival.rectTransform.sizeDelta = new Vector2(140f, 85f) * (1f + (1f - pulse) * 0.3f);
                arrival.color = new Color(1f, 0.76f, 0.24f, 0.4f * pulse);
                // Concurrent milestone bursts share the counter; their halos do not compete for its transform.
                if (!milestone) target.localScale = originalScale * (1f + 0.09f * Mathf.Sin(Mathf.Min(sinceArrival / 0.3f, 1f) * Mathf.PI) * Mathf.Exp(-sinceArrival * 5f));
                yield return null;
            }
        }
        finally
        {
            // Keep the reward exact even if the presentation is interrupted.
            if (shop != null && credited < amount) shop.AddGold(amount - credited);
            if (!milestone && target != null) target.localScale = originalScale;
            if (root != null) Object.Destroy(root);
            if (glowSprite != null)
            {
                Object.Destroy(glowSprite.texture);
                Object.Destroy(glowSprite);
            }
        }
    }

    private static Vector2 Path(Vector2 start, Vector2 end, int index, float t)
    {
        t = t * t * t * (t * (6f * t - 15f) + 10f);
        Vector2 control = (start + end) * 0.5f + Vector2.up * (120f + index * 7f);
        return (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;
    }

    private static Vector2 TargetCenter(RectTransform layer, RectTransform target)
    {
        Canvas canvas = target.GetComponentInParent<Canvas>()?.rootCanvas;
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local);
        return local;
    }

    private static Image Create(Transform parent, string name, Sprite sprite, Color color, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = Vector2.one * size;
        return image;
    }

    private static Sprite CreateGlow()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "VictorySoftGlow";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
            float alpha = Mathf.Exp(-radius * radius * 5f) * (1f - Mathf.SmoothStep(0f, 1f, radius));
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f);
    }
}

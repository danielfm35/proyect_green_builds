using UnityEngine;
using UnityEngine.UI;

// A soft perimeter aura, drawn in one UI mesh without textures or bloom.
public class EnchantmentCapacityGlow : MaskableGraphic
{
    private int state;
    private float phase;
    private float nextFrame;

    public void SetCapacity(int count, int maximum)
    {
        state = count <= 0 ? 0 : count >= maximum ? 2 : 1;
        color = state == 0 ? new Color(0.3f, 0.7f, 1f)
            : state == 1 ? new Color(1f, 0.64f, 0.12f)
            : new Color(0.18f, 1f, 0.46f);
        raycastTarget = false;
        gameObject.SetActive(maximum > 0);
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        phase = (GetInstanceID() & 255) * 0.023f;
    }

    private void Update()
    {
        if (!Application.isPlaying || Time.unscaledTime < nextFrame)
            return;

        nextFrame = Time.unscaledTime + 1f / 30f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width < 2f || rect.height < 2f)
            return;

        float time = Time.unscaledTime + phase;
        float speed = state == 0 ? 1.8f : state == 1 ? 3.2f : 1.3f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * speed);
        // Hold fully off briefly, then smoothly fade the aura and glints together.
        pulse = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 1f, pulse));
        float peakStrength = state == 0 ? 0.52f : state == 1 ? 0.85f : 0.9f;
        float strength = pulse * peakStrength;
        if (strength <= 0f)
            return;
        float unit = Mathf.Min(rect.width, rect.height) / 100f;
        float spread = (5f + pulse * 2f) * unit;

        AddBand(vh, rect, -spread, 0f, 0f, strength, unit);
        AddBand(vh, rect, 0f, 1.4f * unit, strength, strength, unit);
        AddBand(vh, rect, 1.4f * unit, spread + 2f * unit, strength, 0f, unit);

        // Filled items have traveling glints; empty items only breathe.
        int glints = state == 2 ? 3 : state == 1 ? 1 : 0;
        for (int i = 0; i < glints; i++)
        {
            float travel = Mathf.Repeat(time * (state == 2 ? 0.13f : 0.08f) + i / (float)glints, 1f);
            Vector2 point = Perimeter(rect, travel, 0.7f * unit, unit);
            float size = (3f + 1.5f * Mathf.Sin(time * 3f + i)) * unit;
            AddGlint(vh, point, size, strength);
        }
    }

    private void AddBand(VertexHelper vh, Rect rect, float outer, float inner,
        float outerAlpha, float innerAlpha, float unit)
    {
        const int segments = 64;
        int start = vh.currentVertCount;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            vh.AddVert(Perimeter(rect, t, outer, unit), Tint(outerAlpha), Vector2.zero);
            vh.AddVert(Perimeter(rect, t, inner, unit), Tint(innerAlpha), Vector2.zero);
            if (i == segments) continue;
            int v = start + i * 2;
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v + 1, v + 3, v + 2);
        }
    }

    private static Vector2 Perimeter(Rect rect, float t, float inset, float unit)
    {
        Vector2 half = rect.size * 0.5f - Vector2.one * inset;
        float radius = Mathf.Clamp(7f * unit - inset, 0.5f * unit, Mathf.Min(half.x, half.y));
        float section = Mathf.Repeat(t, 1f) * 8f;
        int side = Mathf.FloorToInt(section) / 2;
        float progress = section - Mathf.Floor(section);
        float angle = -side * Mathf.PI * 0.5f;
        Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 up = new Vector2(-right.y, right.x);
        float width = side % 2 == 0 ? half.x : half.y;
        float height = side % 2 == 0 ? half.y : half.x;
        if (Mathf.FloorToInt(section) % 2 == 0)
            return rect.center + right * Mathf.Lerp(-width + radius, width - radius, progress) + up * height;

        float arc = progress * Mathf.PI * 0.5f;
        return rect.center + right * (width - radius + radius * Mathf.Sin(arc))
            + up * (height - radius + radius * Mathf.Cos(arc));
    }

    private Color Tint(float alpha)
    {
        Color tint = color;
        tint.a *= alpha;
        return tint;
    }

    private void AddGlint(VertexHelper vh, Vector2 center, float size, float alpha)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, new Color(0.92f, 1f, 0.9f, alpha), Vector2.zero);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            float radius = i % 2 == 0 ? size * 1.8f : size * 0.3f;
            vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                Tint(0f), Vector2.zero);
        }
        for (int i = 0; i < 8; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
    }
}

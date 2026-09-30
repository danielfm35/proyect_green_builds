using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent emerald rays and four-point glints for the forge reveal.</summary>
public sealed class ModifierForgeBurst : MaskableGraphic
{
    private float progress;
    private bool visible;

    public void Show(float value)
    {
        progress = Mathf.Clamp01(value);
        visible = true;
        SetVerticesDirty();
    }

    public void Clear()
    {
        visible = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (!visible) return;
        float t = progress;
        float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.12f));
        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1f, t));
        float opacity = attack * fade;
        float spread = 1f - Mathf.Pow(1f - t, 3f);
        float radius = rectTransform.rect.size.magnitude * 0.52f;
        Vector2 center = rectTransform.rect.center;

        // Alternating broad shafts and narrow cores, with dark gaps between them.
        for (int i = 0; i < 12; i++)
        {
            float angle = (i * 30f + 8f + Mathf.Sin(i * 2.4f) * 7f + t * 9f) * Mathf.Deg2Rad;
            float length = radius * Mathf.Lerp(0.18f, 0.86f + 0.12f * Mathf.Sin(i * 1.7f), spread);
            float width = (i % 3 == 0 ? 8f : 5f) * Mathf.Deg2Rad;
            Ray(mesh, center, angle, width, length, new Color(0.1f, 1f, 0.32f, opacity * 0.55f));
            Ray(mesh, center, angle, width * 0.43f, length * 0.94f, new Color(0.68f, 1f, 0.68f, opacity * 0.42f));
        }

        // A localized soft core replaces the old full-screen wash.
        float core = Mathf.Lerp(28f, 155f, spread);
        for (int i = 0; i < 48; i++)
        {
            float a = i * Mathf.PI * 2f / 48f;
            float b = (i + 1) * Mathf.PI * 2f / 48f;
            Triangle(mesh, center, center + Direction(a) * core, center + Direction(b) * core,
                new Color(0.78f, 1f, 0.8f, opacity * 0.8f), Color.clear, Color.clear);
        }

        for (int i = 0; i < 18; i++)
        {
            float age = Mathf.Clamp01((t - i % 4 * 0.025f) / 0.85f);
            float twinkle = Mathf.Pow(Mathf.Sin(age * Mathf.PI), 2f) * fade;
            float angle = (i * 137.5f + 12f) * Mathf.Deg2Rad;
            float distance = radius * (0.14f + i % 5 * 0.047f) * Mathf.Lerp(0.65f, 1.12f, spread);
            Vector2 p = center + Direction(angle) * distance;
            float size = (9f + i % 3 * 5f) * twinkle;
            Color tint = i % 3 == 0 ? new Color(0.88f, 1f, 0.9f, twinkle) : new Color(0.2f, 1f, 0.52f, twinkle);
            Star(mesh, p, size * 1.8f, angle + t * 0.3f, new Color(0.1f, 0.9f, 0.3f, twinkle * 0.22f));
            Star(mesh, p, size, angle + t * 0.3f, tint);
        }
    }

    private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

    private static void Ray(VertexHelper mesh, Vector2 center, float angle, float width, float length, Color tint)
    {
        Vector2 left = Direction(angle - width);
        Vector2 right = Direction(angle + width);
        Vector2 a = center + left * length * 0.68f;
        Vector2 b = center + right * length * 0.68f;
        Vector2 c = center + left * length;
        Vector2 d = center + right * length;
        Triangle(mesh, center, a, b, tint, tint, tint);
        Triangle(mesh, a, c, b, tint, Color.clear, tint);
        Triangle(mesh, b, c, d, tint, Color.clear, Color.clear);
    }

    private static void Star(VertexHelper mesh, Vector2 center, float size, float angle, Color color)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 a = center + Direction(angle + i * Mathf.PI / 4f) * size * (i % 2 == 0 ? 1f : 0.22f);
            Vector2 b = center + Direction(angle + (i + 1) * Mathf.PI / 4f) * size * (i % 2 == 0 ? 0.22f : 1f);
            Triangle(mesh, center, a, b, color, color, color);
        }
    }

    private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
    {
        int index = mesh.currentVertCount;
        mesh.AddVert(a, ca, Vector2.zero);
        mesh.AddVert(b, cb, Vector2.zero);
        mesh.AddVert(c, cc, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
    }
}

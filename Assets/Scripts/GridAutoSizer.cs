using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridAutoSizer : MonoBehaviour
{
    public int columns = 4;
    public int rows = 4;
    public float spacing = 12f;
    public float padding = 0f; 

    GridLayoutGroup grid;
    RectTransform rt;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    public void Apply()
    {
        if (!grid || !rt || columns <= 0 || rows <= 0) return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;      grid.constraintCount = columns;
        grid.spacing = new Vector2(spacing, spacing);

        float width = rt.rect.width - padding * 2f - spacing * (columns - 1);
        float height = rt.rect.height - padding * 2f - spacing * (rows - 1);

        float cell = Mathf.Floor(Mathf.Min(width / columns, height / rows));
        grid.cellSize = new Vector2(cell, cell);
    }
}

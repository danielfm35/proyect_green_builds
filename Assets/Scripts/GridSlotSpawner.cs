using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridSlotSpawner : MonoBehaviour
{
    public const int MinGridSize = 3;
    public const int MaxGridSize = 5;

    [Header("Prefab")]
    public GameObject slotPrefab;

    [Header("Grid Size")]
    [Range(MinGridSize, MaxGridSize)] public int columns = MinGridSize;
    [Range(MinGridSize, MaxGridSize)] public int rows = MinGridSize;

    [Header("Fixed Slot Layout")]
    [Min(1f)] public float slotSize = 100f;
    [Min(0f)] public float spacing = 20f;

    private GridLayoutGroup grid;
#if UNITY_EDITOR
    private bool rebuildScheduled;
#endif


    private void Awake() => ApplyFixedLayout();

    private void OnValidate()
    {
        columns = Mathf.Clamp(columns, MinGridSize, MaxGridSize);
        rows = Mathf.Clamp(rows, MinGridSize, MaxGridSize);
        slotSize = Mathf.Max(1f, slotSize);
        spacing = Mathf.Max(0f, spacing);
        ApplyFixedLayout();

#if UNITY_EDITOR
        ScheduleEditorRebuildIfNeeded();
#endif
    }


#if UNITY_EDITOR
    private void ScheduleEditorRebuildIfNeeded()
    {
        if (Application.isPlaying || slotPrefab == null || transform.childCount == columns * rows || rebuildScheduled)
            return;

        rebuildScheduled = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            rebuildScheduled = false;
            if (!Application.isPlaying && slotPrefab != null && transform.childCount != columns * rows)
            {
                RebuildSlots();
                UnityEditor.EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        };
    }
#endif

    public void SetGridSize(int newColumns, int newRows, bool rebuild = true)
    {
        columns = Mathf.Clamp(newColumns, MinGridSize, MaxGridSize);
        rows = Mathf.Clamp(newRows, MinGridSize, MaxGridSize);
        if (rebuild) RebuildSlots();
        else ApplyFixedLayout();
    }

    [ContextMenu("Add Column")]
    public void AddColumn() => SetGridSize(columns + 1, rows);
    [ContextMenu("Add Row")]
    public void AddRow() => SetGridSize(columns, rows + 1);

    [ContextMenu("Rebuild Slots")]
    public void RebuildSlots()
    {
        if (!slotPrefab)
        {
            Debug.LogError("slotPrefab no asignado.");
            return;
        }

        columns = Mathf.Clamp(columns, MinGridSize, MaxGridSize);
        rows = Mathf.Clamp(rows, MinGridSize, MaxGridSize);
        ApplyFixedLayout();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child.gameObject);
            else Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }

        int total = columns * rows;
        for (int i = 0; i < total; i++)
        {
            var go = Instantiate(slotPrefab, transform);
            go.name = $"Slot_{i + 1:00}";
        }
    }

    private void ApplyFixedLayout()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Clamp(columns, MinGridSize, MaxGridSize);
        grid.cellSize = Vector2.one * slotSize;
        grid.spacing = Vector2.one * spacing;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            float width = columns * slotSize + (columns - 1) * spacing;
            float height = rows * slotSize + (rows - 1) * spacing;
            rectTransform.sizeDelta = new Vector2(width, height);
        }
    }
}
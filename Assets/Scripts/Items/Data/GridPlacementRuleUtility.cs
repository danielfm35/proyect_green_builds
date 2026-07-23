using System;
using System.Collections.Generic;
using UnityEngine;

public static class GridPlacementRuleUtility
{
    private static readonly Dictionary<GridPlacementRule, Sprite> SpriteCache = new();

    public static IReadOnlyList<GridPlacementRule> GetSelectableRules()
    {
        GridPlacementRule[] allRules = (GridPlacementRule[])Enum.GetValues(typeof(GridPlacementRule));
        List<GridPlacementRule> selectableRules = new();

        for (int i = 0; i < allRules.Length; i++)
        {
            GridPlacementRule rule = allRules[i];
            if (rule == GridPlacementRule.None || rule == GridPlacementRule.Adjacent)
                continue;

            if (LoadRuleSprite(rule) == null)
                continue;

            selectableRules.Add(rule);
        }

        return selectableRules;
    }

    public static IReadOnlyList<GridPlacementRule> GetEffectTargetRules()
    {
        GridPlacementRule[] allRules = (GridPlacementRule[])Enum.GetValues(typeof(GridPlacementRule));
        List<GridPlacementRule> selectableRules = new();

        for (int i = 0; i < allRules.Length; i++)
        {
            GridPlacementRule rule = allRules[i];
            if (rule == GridPlacementRule.None)
                continue;

            if (LoadRuleSprite(rule) == null)
                continue;

            selectableRules.Add(rule);
        }

        return selectableRules;
    }

    public static bool IsSlotAllowed(GridPlacementRule rule, int row, int column, int rowCount, int columnCount)
    {
        if (rule == GridPlacementRule.None)
            return true;

        if (rowCount <= 0 || columnCount <= 0)
            return true;

        int lastRow = rowCount - 1;
        int lastColumn = columnCount - 1;

        bool isTop = row == 0;
        bool isBottom = row == lastRow;
        bool isLeft = column == 0;
        bool isRight = column == lastColumn;
        bool isCorner = (isTop || isBottom) && (isLeft || isRight);
        bool isEdge = isTop || isBottom || isLeft || isRight;

        switch (rule)
        {
            case GridPlacementRule.Center:
                return !isEdge;
            case GridPlacementRule.Corners:
                return isCorner;
            case GridPlacementRule.Up:
                return isTop;
            case GridPlacementRule.Down:
                return isBottom;
            case GridPlacementRule.Left:
                return isLeft;
            case GridPlacementRule.Right:
                return isRight;
            case GridPlacementRule.LeftRight:
                return isLeft || isRight;
            case GridPlacementRule.UpDown:
                return isTop || isBottom;
            default:
                return true;
        }
    }

    public static bool IsSlotAffectedByRule(
        GridPlacementRule rule,
        int originRow,
        int originColumn,
        int targetRow,
        int targetColumn,
        int rowCount,
        int columnCount
    )
    {
        if (rule == GridPlacementRule.Adjacent)
        {
            int rowDistance = Mathf.Abs(targetRow - originRow);
            int columnDistance = Mathf.Abs(targetColumn - originColumn);
            return rowDistance + columnDistance == 1;
        }

        return IsSlotAllowed(rule, targetRow, targetColumn, rowCount, columnCount);
    }

    public static Sprite LoadRuleSprite(GridPlacementRule rule)
    {
        if (rule == GridPlacementRule.None)
            return null;

        if (SpriteCache.TryGetValue(rule, out Sprite cachedSprite))
            return cachedSprite;

        string resourceName = GetResourceName(rule);
        Sprite sprite = string.IsNullOrEmpty(resourceName)
            ? null
            : Resources.Load<Sprite>($"Images/UI/{resourceName}");

        SpriteCache[rule] = sprite;
        return sprite;
    }

    private static string GetResourceName(GridPlacementRule rule)
    {
        switch (rule)
        {
            case GridPlacementRule.Center: return "center";
            case GridPlacementRule.Corners: return "corners";
            case GridPlacementRule.Up: return "up";
            case GridPlacementRule.Down: return "down";
            case GridPlacementRule.Left: return "left";
            case GridPlacementRule.Right: return "right";
            case GridPlacementRule.LeftRight: return "left_right";
            case GridPlacementRule.UpDown: return "up_down";
            case GridPlacementRule.Adjacent: return "adjacent";
            default: return string.Empty;
        }
    }
}

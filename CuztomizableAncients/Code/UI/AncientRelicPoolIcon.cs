using CuztomizableAncients.Configuration;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace CuztomizableAncients.UI;

public static class AncientRelicPoolIcon
{
    private static readonly Vector2 CellSize = new(96f, 96f);
    private static readonly Vector2 IconSize = new(64f, 64f);
    private static readonly Vector2[] IconOffsets =
    [
        new(0f, 0f),
        new(14f, 14f),
        new(28f, 28f)
    ];

    public static Control Create(
        AncientRelicPoolDefinition pool,
        bool enabled = true,
        bool draggable = false,
        int? sourceOption = null,
        Action? onPressed = null)
    {
        Button cell = new()
        {
            CustomMinimumSize = CellSize,
            Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.5f),
            MouseFilter = Control.MouseFilterEnum.Pass
        };

        if (draggable)
        {
            AncientPoolDragControls.ConfigureSource(
                cell,
                pool.Id,
                sourceOption,
                () => Create(pool));
        }

        if (onPressed != null)
        {
            cell.Pressed += onPressed;
        }

        cell.Connect(Control.SignalName.MouseEntered, Callable.From(() =>
        {
            NHoverTipSet.CreateAndShow(cell, BuildHoverTip(pool), HoverTipAlignment.Right);
        }));
        cell.Connect(Control.SignalName.MouseExited, Callable.From(() => NHoverTipSet.Remove(cell)));

        List<RelicModel> relics = pool
            .GetRelicsForConfiguration()
            .Where(relic => !AncientRelicConfigService.ActiveConfig.DisabledRelics.Contains(relic.Id))
            .DistinctBy(relic => relic.Id)
            .OrderBy(relic => relic.Title.GetRawText())
            .Take(IconOffsets.Length)
            .ToList();

        List<NRelic> relicNodes = [];
        for (int i = 0; i < relics.Count; i++)
        {
            NRelic? icon = NRelic.Create(relics[i], NRelic.IconSize.Small);
            if (icon == null)
            {
                continue;
            }

            icon.Position = IconOffsets[i];
            icon.Scale = Vector2.One * 0.85f;
            icon.CustomMinimumSize = IconSize;
            icon.MouseFilter = Control.MouseFilterEnum.Ignore;
            cell.AddChild(icon);
            relicNodes.Add(icon);
        }

        cell.TreeEntered += () => Callable.From(() => CenterRelicStack(cell, relicNodes)).CallDeferred();
        return cell;
    }

    private static void CenterRelicStack(Control cell, IReadOnlyList<NRelic> relics)
    {
        if (relics.Count == 0 || relics.Any(relic => !relic.IsNodeReady()))
        {
            return;
        }

        Rect2 bounds = default;
        bool hasBounds = false;
        foreach (NRelic relic in relics)
        {
            Vector2 scale = relic.Scale * relic.Icon.Scale;
            Rect2 iconBounds = new(
                relic.Position + relic.Icon.Position * relic.Scale,
                relic.Icon.Size * scale);
            bounds = hasBounds ? bounds.Merge(iconBounds) : iconBounds;
            hasBounds = true;
        }

        Vector2 offset = (cell.Size - bounds.Size) * 0.5f - bounds.Position;
        foreach (NRelic relic in relics)
        {
            relic.Position += offset;
        }
    }

    private static HoverTip BuildHoverTip(AncientRelicPoolDefinition pool)
    {
        List<string> lines = [];
        if (!string.IsNullOrWhiteSpace(pool.Author))
        {
            lines.Add($"By {pool.Author}");
        }

        if (!string.IsNullOrWhiteSpace(pool.Description))
        {
            lines.Add(pool.Description);
        }

        object boxedTip = default(HoverTip);
        typeof(HoverTip).GetProperty(nameof(HoverTip.Title))!.SetValue(boxedTip, pool.DisplayName);
        typeof(HoverTip).GetProperty(nameof(HoverTip.Description))!.SetValue(boxedTip, string.Join("\n", lines));
        HoverTip tip = (HoverTip)boxedTip;
        tip.Id = $"CuztomizableAncients:Pool:{pool.Id}";
        return tip;
    }
}

using Godot;

namespace CuztomizableAncients.UI;

public readonly record struct AncientPoolDragData(string PoolId, int? SourceOption);

public static class AncientPoolDragControls
{
    private const string DragPrefix = "CuztomizableAncients:Pool:";

    public static AncientPoolDragData? ActiveDrag { get; private set; }

    public static event Action<AncientPoolDragData?>? DragStateChanged;

    public static void ConfigureSource(
        Control source,
        string poolId,
        int? sourceOption,
        Func<Control> previewFactory)
    {
        Variant GetDragData(Vector2 position)
        {
            AncientPoolDragData payload = new(poolId, sourceOption);
            source.SetDragPreview(previewFactory());
            SetActiveDrag(payload);
            _ = TrackDragEnd(source);
            return Variant.From(Encode(payload));
        }

        source.SetDragForwarding(
            Callable.From<Vector2, Variant>(GetDragData),
            default,
            default);
    }

    public static void ConfigureTarget(
        Control target,
        Func<AncientPoolDragData, bool> canDrop,
        Action<AncientPoolDragData> onDrop)
    {
        bool CanDropData(Vector2 position, Variant data)
        {
            return TryDecode(data, out AncientPoolDragData payload) && canDrop(payload);
        }

        void DropData(Vector2 position, Variant data)
        {
            if (TryDecode(data, out AncientPoolDragData payload) && canDrop(payload))
            {
                onDrop(payload);
            }

            SetActiveDrag(null);
        }

        target.SetDragForwarding(
            default,
            Callable.From<Vector2, Variant, bool>(CanDropData),
            Callable.From<Vector2, Variant>(DropData));
    }

    public static void BindDragState(Node owner, Action<AncientPoolDragData?> handler)
    {
        DragStateChanged += handler;
        owner.TreeExiting += () => DragStateChanged -= handler;
        handler(ActiveDrag);
    }

    private static async Task TrackDragEnd(Control source)
    {
        try
        {
            await source.ToSignal(source.GetTree(), SceneTree.SignalName.ProcessFrame);
            while (GodotObject.IsInstanceValid(source) && source.GetViewport().GuiIsDragging())
            {
                await source.ToSignal(source.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        finally
        {
            SetActiveDrag(null);
        }
    }

    private static void SetActiveDrag(AncientPoolDragData? payload)
    {
        if (ActiveDrag == payload)
        {
            return;
        }

        ActiveDrag = payload;
        DragStateChanged?.Invoke(payload);
    }

    private static string Encode(AncientPoolDragData payload)
    {
        return $"{DragPrefix}{payload.SourceOption ?? -1}:{payload.PoolId}";
    }

    private static bool TryDecode(Variant data, out AncientPoolDragData payload)
    {
        payload = default;
        if (data.VariantType != Variant.Type.String)
        {
            return false;
        }

        string value = data.AsString();
        if (!value.StartsWith(DragPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string encoded = value[DragPrefix.Length..];
        int separator = encoded.IndexOf(':');
        if (separator < 1 || !int.TryParse(encoded[..separator], out int sourceOption))
        {
            return false;
        }

        string poolId = encoded[(separator + 1)..];
        if (poolId.Length == 0)
        {
            return false;
        }

        payload = new AncientPoolDragData(poolId, sourceOption >= 0 ? sourceOption : null);
        return true;
    }
}

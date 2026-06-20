using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CuztomizableAncients.Configuration;

public sealed class AncientRelicPoolDefinition
{
    private readonly Func<IEnumerable<RelicModel>> _relics;

    public AncientRelicPoolDefinition(
        string id,
        string displayName,
        Func<IEnumerable<RelicModel>> relics,
        string? author = null,
        string? description = null,
        IEnumerable<ModelId>? nativeAncientIds = null,
        bool isModded = false,
        bool isUserCreated = false)
    {
        Id = id;
        DisplayName = displayName;
        _relics = relics;
        Author = author;
        Description = description;
        NativeAncientIds = nativeAncientIds?.ToHashSet() ?? [];
        IsModded = isModded;
        IsUserCreated = isUserCreated;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string? Author { get; }

    public string? Description { get; }

    public IReadOnlySet<ModelId> NativeAncientIds { get; }

    public bool IsModded { get; }

    public bool IsUserCreated { get; }

    public bool IsNativeTo(ModelId ancientId)
    {
        return NativeAncientIds.Contains(ancientId);
    }

    public IEnumerable<RelicModel> GetRelics(Player player)
    {
        return _relics();
    }

    public IEnumerable<RelicModel> GetRelicsForConfiguration()
    {
        return _relics();
    }
}

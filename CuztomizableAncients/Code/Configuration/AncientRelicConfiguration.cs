using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace CuztomizableAncients.Configuration;

public sealed class AncientRelicConfiguration
{
    public HashSet<ModelId> DisabledRelics { get; } = [];

    public Dictionary<ModelId, AncientRelicConfig> AncientConfigs { get; } = [];

    public Dictionary<string, CustomRelicPoolConfig> CustomPools { get; } = [];

    public AncientRelicConfig GetAncientConfig(ModelId ancientId)
    {
        if (!AncientConfigs.TryGetValue(ancientId, out AncientRelicConfig? config))
        {
            config = AncientRelicConfig.CreateDefault(ancientId);
            AncientConfigs.Add(ancientId, config);
        }

        config.EnsureValidShape();
        return config;
    }

    public AncientRelicConfiguration Clone()
    {
        AncientRelicConfiguration clone = new();
        clone.DisabledRelics.UnionWith(DisabledRelics);
        foreach ((string poolId, CustomRelicPoolConfig pool) in CustomPools)
        {
            clone.CustomPools.Add(poolId, pool.Clone());
        }

        foreach ((ModelId ancientId, AncientRelicConfig config) in AncientConfigs)
        {
            clone.AncientConfigs.Add(ancientId, config.Clone());
        }

        return clone;
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteFullModelIdList(DisabledRelics.ToList());
        writer.WriteInt(CustomPools.Count);
        foreach (CustomRelicPoolConfig pool in CustomPools.Values)
        {
            writer.WriteString(pool.Id);
            writer.WriteString(pool.DisplayName);
            writer.WriteString(pool.Author);
            writer.WriteString(pool.Description ?? string.Empty);
            writer.WriteFullModelIdList(pool.RelicIds.ToList());
        }

        writer.WriteInt(AncientConfigs.Count);
        foreach ((ModelId ancientId, AncientRelicConfig config) in AncientConfigs)
        {
            writer.WriteFullModelId(ancientId);
            writer.WriteBool(config.IsCustomized);
            writer.WriteInt(config.Options.Count);
            foreach (AncientOptionConfig slot in config.Options)
            {
                writer.WriteInt(slot.OptionIndex);
                writer.WriteInt(slot.PoolIds.Count);
                foreach (string poolId in slot.PoolIds)
                {
                    writer.WriteString(poolId);
                }
            }
        }
    }

    public static AncientRelicConfiguration Deserialize(PacketReader reader)
    {
        AncientRelicConfiguration config = new();
        config.DisabledRelics.UnionWith(reader.ReadFullModelIdList().Where(id => id != ModelId.none));

        int customPoolCount = reader.ReadInt();
        for (int i = 0; i < customPoolCount; i++)
        {
            string id = reader.ReadString();
            string displayName = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();
            List<ModelId> relicIds = reader.ReadFullModelIdList()
                .Where(relicId => relicId != ModelId.none)
                .Distinct()
                .ToList();
            config.CustomPools[id] = new CustomRelicPoolConfig(
                id,
                displayName,
                relicIds,
                author,
                description.Length == 0 ? null : description);
        }

        int ancientCount = reader.ReadInt();
        for (int i = 0; i < ancientCount; i++)
        {
            ModelId ancientId = reader.ReadFullModelId();
            AncientRelicConfig ancientConfig = new(ancientId)
            {
                IsCustomized = reader.ReadBool()
            };
            int optionCount = reader.ReadInt();
            for (int optionIndex = 0; optionIndex < optionCount; optionIndex++)
            {
                int index = reader.ReadInt();
                int poolCount = reader.ReadInt();
                List<string> poolIds = [];
                for (int poolIndex = 0; poolIndex < poolCount; poolIndex++)
                {
                    poolIds.Add(reader.ReadString());
                }

                ancientConfig.Options.Add(new AncientOptionConfig(index, poolIds));
            }

            ancientConfig.EnsureValidShape();
            config.AncientConfigs[ancientId] = ancientConfig;
        }

        return config;
    }

    public void EnsureValidShape()
    {
        foreach (AncientRelicConfig config in AncientConfigs.Values)
        {
            config.EnsureValidShape();
        }
    }
}

public sealed class AncientRelicConfig(ModelId ancientId)
{
    public ModelId AncientId { get; } = ancientId;

    public bool IsCustomized { get; set; }

    public List<AncientOptionConfig> Options { get; } = [];

    public static AncientRelicConfig CreateDefault(ModelId ancientId)
    {
        AncientRelicConfig config = new(ancientId);
        IReadOnlyList<IReadOnlyList<string>> defaultSlots = AncientRelicPools.GetDefaultOptionPoolIds(ancientId);
        for (int i = 0; i < defaultSlots.Count; i++)
        {
            config.Options.Add(new AncientOptionConfig(i, defaultSlots[i]));
        }

        config.IsCustomized = AncientRelicPools.HasDefaultPresets(ancientId);

        return config;
    }

    public AncientRelicConfig Clone()
    {
        AncientRelicConfig clone = new(AncientId)
        {
            IsCustomized = IsCustomized
        };
        clone.Options.AddRange(Options.Select(option => option.Clone()));
        return clone;
    }

    public void EnsureValidShape()
    {
        Options.Sort((left, right) => left.OptionIndex.CompareTo(right.OptionIndex));
        for (int i = 0; i < Options.Count; i++)
        {
            Options[i].OptionIndex = i;
        }
    }
}

public sealed class AncientOptionConfig
{
    public AncientOptionConfig(int optionIndex, IEnumerable<string> poolIds)
    {
        OptionIndex = optionIndex;
        PoolIds = poolIds.Distinct().ToList();
    }

    public int OptionIndex { get; set; }

    public List<string> PoolIds { get; }

    public AncientOptionConfig Clone()
    {
        return new AncientOptionConfig(OptionIndex, PoolIds);
    }
}

public sealed class CustomRelicPoolConfig
{
    public CustomRelicPoolConfig(
        string id,
        string displayName,
        IEnumerable<ModelId> relicIds,
        string author,
        string? description = null)
    {
        Id = id;
        DisplayName = displayName;
        RelicIds = relicIds.Distinct().ToList();
        Author = author;
        Description = description;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public List<ModelId> RelicIds { get; }

    public string Author { get; }

    public string? Description { get; }

    public CustomRelicPoolConfig Clone()
    {
        return new CustomRelicPoolConfig(Id, DisplayName, RelicIds, Author, Description);
    }
}

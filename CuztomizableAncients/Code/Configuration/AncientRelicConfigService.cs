using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace CuztomizableAncients.Configuration;

public static class AncientRelicConfigService
{
    private static AncientRelicConfiguration _activeConfig = new();

    public static AncientRelicConfiguration ActiveConfig
    {
        get => _activeConfig;
        set
        {
            _activeConfig = value.Clone();
            _activeConfig.EnsureValidShape();
        }
    }

    public static void ResetToDefault()
    {
        ActiveConfig = new AncientRelicConfiguration();
    }

    public static void ResetAncient(ModelId ancientId)
    {
        ActiveConfig.AncientConfigs.Remove(ancientId);
    }

    public static AncientRelicPoolDefinition CreateCustomPool(string displayName)
    {
        string id = $"custom:{Guid.NewGuid():N}";
        ActiveConfig.CustomPools[id] = new CustomRelicPoolConfig(id, displayName, [], "You");
        return AncientRelicPools.Get(id)!;
    }

    public static bool DeleteCustomPool(string poolId)
    {
        if (!ActiveConfig.CustomPools.Remove(poolId))
        {
            return false;
        }

        foreach (AncientRelicConfig ancientConfig in ActiveConfig.AncientConfigs.Values)
        {
            foreach (AncientOptionConfig slot in ancientConfig.Options)
            {
                slot.PoolIds.RemoveAll(id => id == poolId);
            }
        }

        return true;
    }

    public static AncientRelicPoolDefinition AddRelicToPool(
        ModelId ancientId,
        AncientRelicPoolDefinition pool,
        RelicModel relic)
    {
        if (ActiveConfig.CustomPools.TryGetValue(pool.Id, out CustomRelicPoolConfig? customPool))
        {
            if (!customPool.RelicIds.Contains(relic.Id))
            {
                customPool.RelicIds.Add(relic.Id);
            }

            return AncientRelicPools.Get(customPool.Id)!;
        }

        string id = $"custom:{Guid.NewGuid():N}";
        List<ModelId> relicIds = pool.GetRelicsForConfiguration()
            .Select(existing => existing.Id)
            .Append(relic.Id)
            .Distinct()
            .ToList();
        ActiveConfig.CustomPools[id] = new CustomRelicPoolConfig(
            id,
            $"{pool.DisplayName} (customized)",
            relicIds,
            string.IsNullOrWhiteSpace(pool.Author)
                ? "Modified by you"
                : $"{pool.Author}, modified by you",
            pool.Description);

        AncientRelicConfig ancientConfig = ActiveConfig.GetAncientConfig(ancientId);
        bool replacedAssignment = false;
        foreach (AncientOptionConfig slot in ancientConfig.Options)
        {
            for (int i = 0; i < slot.PoolIds.Count; i++)
            {
                if (slot.PoolIds[i] == pool.Id)
                {
                    slot.PoolIds[i] = id;
                    replacedAssignment = true;
                }
            }
        }

        if (replacedAssignment)
        {
            ancientConfig.IsCustomized = true;
        }

        return AncientRelicPools.Get(id)!;
    }

    public static void Broadcast(INetGameService netService)
    {
        if (netService.Type is NetGameType.Host or NetGameType.Singleplayer)
        {
            netService.SendMessage(new AncientRelicConfigMessage(ActiveConfig));
        }
    }

    public static IReadOnlyList<RelicModel> GetCandidatesForOption(
        ModelId ancientId,
        int optionIndex,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        AncientOptionConfig slot = ActiveConfig.GetAncientConfig(ancientId).Options
            .First(slot => slot.OptionIndex == optionIndex);

        IEnumerable<RelicModel> candidates = slot.PoolIds
            .Select(AncientRelicPools.Get)
            .OfType<AncientRelicPoolDefinition>()
            .SelectMany(pool => pool.GetRelics(player))
            .Where(relic => !ActiveConfig.DisabledRelics.Contains(relic.Id))
            .DistinctBy(relic => relic.Id);

        if (ancientId == ModelDb.GetId<MegaCrit.Sts2.Core.Models.Events.Neow>())
        {
            candidates = candidates.Where(relic => relic.IsAllowedAtNeow(player));
        }

        return candidates.ToList();
    }

    public static string GetNeowDonePageFor(RelicModel relic)
    {
        if (relic.Id.Entry == ModelDb.GetId<MegaCrit.Sts2.Core.Models.Relics.NeowsBones>().Entry)
        {
            return "NEOW.pages.DONE.POSITIVE.description";
        }

        bool curseRelic = AncientRelicPools.Get(AncientRelicPools.CursePoolId)!
            .GetRelicsForConfiguration()
            .Any(candidate => candidate.Id == relic.Id);

        return curseRelic ? "NEOW.pages.DONE.CURSED.description" : "NEOW.pages.DONE.POSITIVE.description";
    }
}

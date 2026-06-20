using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CuztomizableAncients.Configuration;

public static class AncientRelicPools
{
    public const string PositivePoolId = "vanilla_positive";
    public const string CursePoolId = "vanilla_curse";

    private static readonly List<AncientRelicPoolDefinition> Pools = [];

    private static readonly Dictionary<ModelId, List<DefaultPoolPlacement>> DefaultPlacements = [];

    private static bool _initialized;

    private static bool _initializing;

    public static IReadOnlyList<AncientRelicPoolDefinition> All
    {
        get
        {
            EnsureInitialized();
            return Pools;
        }
    }

    public static IReadOnlyList<AncientEventModel> AllAncients
    {
        get
        {
            HashSet<ModelId> actTwoAncients = ModelDb.Act<Hive>().AllAncients
                .Select(ancient => ancient.Id)
                .ToHashSet();
            HashSet<ModelId> actThreeAncients = ModelDb.Act<Glory>().AllAncients
                .Select(ancient => ancient.Id)
                .ToHashSet();

            return ModelDb.AllAbstractModelSubtypes
                .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(AncientEventModel)))
                .Select(type => ModelDb.GetById<AncientEventModel>(ModelDb.GetId(type)))
                .Where(ancient => ancient is not DeprecatedAncientEvent)
                .DistinctBy(ancient => ancient.Id)
                .OrderBy(ancient => GetAncientSortGroup(ancient, actTwoAncients, actThreeAncients))
                .ThenBy(ancient => ancient.Title.GetRawText())
                .ToList();
        }
    }

    public static void Register(
        AncientRelicPoolDefinition pool,
        params AncientPoolDefaultPreset[] defaultPresets)
    {
        if (Pools.Any(existing => existing.Id == pool.Id))
        {
            throw new InvalidOperationException($"Ancient relic pool '{pool.Id}' is already registered.");
        }

        Pools.Add(pool);
        RegisterDefaultPresets(pool.Id, defaultPresets);
    }

    public static void RegisterDefaultPresets<TPool>(params AncientPoolDefaultPreset[] defaultPresets)
        where TPool : RelicPoolModel
    {
        RegisterDefaultPresets(GetAutomaticPoolId(ModelDb.GetId<TPool>()), defaultPresets);
    }

    public static void RegisterDefaultPresets(
        string poolId,
        params AncientPoolDefaultPreset[] defaultPresets)
    {
        if (_initialized && Pools.All(pool => pool.Id != poolId))
        {
            throw new ArgumentException($"Relic pool '{poolId}' is not registered.", nameof(poolId));
        }

        foreach (AncientPoolDefaultPreset preset in defaultPresets)
        {
            if (!DefaultPlacements.TryGetValue(preset.AncientId, out List<DefaultPoolPlacement>? placements))
            {
                placements = [];
                DefaultPlacements.Add(preset.AncientId, placements);
            }

            foreach (int optionIndex in preset.OptionIndices)
            {
                if (placements.All(existing => existing.PoolId != poolId || existing.OptionIndex != optionIndex))
                {
                    placements.Add(new DefaultPoolPlacement(poolId, optionIndex));
                }
            }
        }
    }

    public static string GetAutomaticPoolId(ModelId relicPoolId)
    {
        return $"modpool:{relicPoolId}";
    }

    public static AncientRelicPoolDefinition? Get(string id)
    {
        AncientRelicPoolDefinition? builtIn = All.FirstOrDefault(pool => pool.Id == id);
        if (builtIn != null)
        {
            return builtIn;
        }

        return AncientRelicConfigService.ActiveConfig.CustomPools.TryGetValue(id, out CustomRelicPoolConfig? custom)
            ? CreateDefinition(custom)
            : null;
    }

    public static IReadOnlyList<AncientRelicPoolDefinition> GetAvailablePools(
        ModelId ancientId,
        bool showNonNative,
        bool showModded)
    {
        IEnumerable<AncientRelicPoolDefinition> customPools = AncientRelicConfigService.ActiveConfig.CustomPools.Values
            .Select(CreateDefinition);
        return All.Concat(customPools)
            .Where(pool => pool.IsUserCreated ||
                           ((pool.IsNativeTo(ancientId) || HasDefaultPlacement(pool.Id, ancientId) || showNonNative) &&
                            (!pool.IsModded || showModded)))
            .OrderBy(pool => pool.DisplayName)
            .ToList();
    }

    public static IReadOnlyList<IReadOnlyList<string>> GetDefaultOptionPoolIds(ModelId ancientId)
    {
        List<List<string>> slots;
        if (ancientId == ModelDb.GetId<Neow>())
        {
            slots = [[PositivePoolId], [PositivePoolId], [CursePoolId]];
        }
        else
        {
            string? nativePoolId = All.FirstOrDefault(pool => pool.IsNativeTo(ancientId))?.Id;
            slots = nativePoolId == null ? [] : [[nativePoolId], [nativePoolId], [nativePoolId]];
        }

        if (DefaultPlacements.TryGetValue(ancientId, out List<DefaultPoolPlacement>? placements))
        {
            foreach (DefaultPoolPlacement placement in placements)
            {
                while (slots.Count <= placement.OptionIndex)
                {
                    slots.Add([]);
                }

                if (!slots[placement.OptionIndex].Contains(placement.PoolId))
                {
                    slots[placement.OptionIndex].Add(placement.PoolId);
                }
            }
        }

        return slots;
    }

    public static bool HasDefaultPresets(ModelId ancientId)
    {
        return DefaultPlacements.TryGetValue(ancientId, out List<DefaultPoolPlacement>? placements) &&
               placements.Count > 0;
    }

    public static IEnumerable<RelicModel> GetAllConfiguredRelics()
    {
        IEnumerable<AncientRelicPoolDefinition> customPools = AncientRelicConfigService.ActiveConfig.CustomPools.Values
            .Select(CreateDefinition);
        return All.Concat(customPools)
            .SelectMany(pool => pool.GetRelicsForConfiguration())
            .DistinctBy(relic => relic.Id)
            .OrderBy(relic => relic.Title.GetRawText());
    }

    private static void EnsureInitialized()
    {
        if (_initialized || _initializing)
        {
            return;
        }

        _initializing = true;
        try
        {
            ModelId neowId = ModelDb.GetId<Neow>();
            AddDiscoveredPool(new AncientRelicPoolDefinition(
                PositivePoolId,
                "Neow Positive",
                GetVanillaPositiveRelics,
                "Mega Crit",
                nativeAncientIds: [neowId]));
            AddDiscoveredPool(new AncientRelicPoolDefinition(
                CursePoolId,
                "Neow Curse",
                GetVanillaCurseRelics,
                "Mega Crit",
                nativeAncientIds: [neowId]));

            foreach (AncientEventModel ancient in AllAncients.Where(ancient => ancient.Id != neowId))
            {
                AncientEventModel capturedAncient = ancient;
                AddDiscoveredPool(new AncientRelicPoolDefinition(
                    $"ancient:{ancient.Id}:native",
                    $"{ancient.Title.GetFormattedText()} Native",
                    () => GetAncientRelics(capturedAncient),
                    IsModdedModel(ancient) ? ancient.GetType().Assembly.GetName().Name : "Mega Crit",
                    nativeAncientIds: [ancient.Id],
                    isModded: IsModdedModel(ancient)));
            }

            foreach (RelicRarity rarity in Enum.GetValues<RelicRarity>().Where(rarity => rarity != RelicRarity.None))
            {
                RelicRarity capturedRarity = rarity;
                AddDiscoveredPool(new AncientRelicPoolDefinition(
                    $"rarity:{rarity.ToString().ToLowerInvariant()}",
                    $"{rarity} Relics",
                    () => ModelDb.AllRelics.Where(relic => relic.Rarity == capturedRarity),
                    "Mega Crit"));
            }

            IEnumerable<RelicPoolModel> moddedRelicPools = ModelDb.AllAbstractModelSubtypes
                .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(RelicPoolModel)))
                .Select(type => ModelDb.GetById<RelicPoolModel>(ModelDb.GetId(type)))
                .Where(IsModdedModel)
                .DistinctBy(pool => pool.Id);
            foreach (RelicPoolModel relicPool in moddedRelicPools)
            {
                RelicPoolModel capturedPool = relicPool;
                AddDiscoveredPool(new AncientRelicPoolDefinition(
                    GetAutomaticPoolId(relicPool.Id),
                    relicPool.Id.Entry.Replace('_', ' '),
                    () => capturedPool.AllRelics,
                    relicPool.GetType().Assembly.GetName().Name,
                    isModded: true));
            }

            RemoveInvalidDefaultPlacements();
            _initialized = true;
        }
        finally
        {
            _initializing = false;
        }
    }

    private static void AddDiscoveredPool(AncientRelicPoolDefinition pool)
    {
        if (Pools.All(existing => existing.Id != pool.Id))
        {
            Pools.Add(pool);
        }
    }

    private static void RemoveInvalidDefaultPlacements()
    {
        HashSet<string> poolIds = Pools.Select(pool => pool.Id).ToHashSet();
        foreach (List<DefaultPoolPlacement> placements in DefaultPlacements.Values)
        {
            foreach (string missingPoolId in placements
                         .Where(placement => !poolIds.Contains(placement.PoolId))
                         .Select(placement => placement.PoolId)
                         .Distinct())
            {
                CuztomizableAncientsMod.Logger.Warn(
                    $"Ignoring ancient default preset for unknown relic pool '{missingPoolId}'.");
            }

            placements.RemoveAll(placement => !poolIds.Contains(placement.PoolId));
        }
    }

    private static IEnumerable<RelicModel> GetAncientRelics(AncientEventModel ancient)
    {
        try
        {
            return ancient.AllPossibleOptions
                .Select(option => option.Relic)
                .OfType<RelicModel>()
                .Select(relic => relic.CanonicalInstance)
                .DistinctBy(relic => relic.Id)
                .ToList();
        }
        catch (Exception exception)
        {
            CuztomizableAncientsMod.Logger.Warn($"Could not enumerate native relics for ancient {ancient.Id}: {exception.Message}");
            return [];
        }
    }

    private static bool IsModdedModel(AbstractModel model)
    {
        return model.GetType().Assembly != typeof(AbstractModel).Assembly;
    }

    private static bool HasDefaultPlacement(string poolId, ModelId ancientId)
    {
        return DefaultPlacements.TryGetValue(ancientId, out List<DefaultPoolPlacement>? placements) &&
               placements.Any(placement => placement.PoolId == poolId);
    }

    private static int GetAncientSortGroup(
        AncientEventModel ancient,
        IReadOnlySet<ModelId> actTwoAncients,
        IReadOnlySet<ModelId> actThreeAncients)
    {
        if (ancient is Neow)
        {
            return 0;
        }

        if (actTwoAncients.Contains(ancient.Id))
        {
            return 1;
        }

        return actThreeAncients.Contains(ancient.Id) ? 2 : 3;
    }

    private static AncientRelicPoolDefinition CreateDefinition(CustomRelicPoolConfig custom)
    {
        return new AncientRelicPoolDefinition(
            custom.Id,
            custom.DisplayName,
            () => custom.RelicIds
                .Select(ModelDb.GetById<RelicModel>)
                .Where(relic => relic != null),
            custom.Author,
            custom.Description,
            isUserCreated: true);
    }

    private static IEnumerable<RelicModel> GetVanillaPositiveRelics()
    {
        return new RelicModel[]
        {
            ModelDb.Relic<ArcaneScroll>(),
            ModelDb.Relic<BoomingConch>(),
            ModelDb.Relic<FishingRod>(),
            ModelDb.Relic<GoldenPearl>(),
            ModelDb.Relic<Kaleidoscope>(),
            ModelDb.Relic<LeadPaperweight>(),
            ModelDb.Relic<LostCoffer>(),
            ModelDb.Relic<MassiveScroll>(),
            ModelDb.Relic<NeowsTorment>(),
            ModelDb.Relic<NewLeaf>(),
            ModelDb.Relic<PhialHolster>(),
            ModelDb.Relic<PreciseScissors>(),
            ModelDb.Relic<ScrollBoxes>(),
            ModelDb.Relic<WingedBoots>(),
            ModelDb.Relic<LavaRock>(),
            ModelDb.Relic<SmallCapsule>(),
            ModelDb.Relic<NutritiousOyster>(),
            ModelDb.Relic<StoneHumidifier>(),
            ModelDb.Relic<NeowsTalisman>(),
            ModelDb.Relic<Pomander>()
        };
    }

    private static IEnumerable<RelicModel> GetVanillaCurseRelics()
    {
        return new RelicModel[]
        {
            ModelDb.Relic<CursedPearl>(),
            ModelDb.Relic<HeftyTablet>(),
            ModelDb.Relic<LargeCapsule>(),
            ModelDb.Relic<LeafyPoultice>(),
            ModelDb.Relic<NeowsBones>(),
            ModelDb.Relic<PrecariousShears>(),
            ModelDb.Relic<SilkenTress>(),
            ModelDb.Relic<SilverCrucible>()
        };
    }

    private sealed record DefaultPoolPlacement(string PoolId, int OptionIndex);
}

using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace CuztomizableAncients.Configuration;

internal static class AncientRelicConfigPersistence
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static AncientRelicConfiguration Load()
    {
        try
        {
            string configPath = GetConfigPath();
            if (!File.Exists(configPath))
            {
                return new AncientRelicConfiguration();
            }

            PersistedSettings? persisted = JsonSerializer.Deserialize<PersistedSettings>(
                File.ReadAllText(configPath),
                JsonOptions);
            if (persisted == null || persisted.Version > CurrentVersion)
            {
                throw new JsonException($"Unsupported configuration version {persisted?.Version}.");
            }

            return FromPersisted(persisted);
        }
        catch (Exception exception)
        {
            CuztomizableAncientsMod.Logger.Warn(
                $"Could not load saved configuration: {exception.Message}");
            return new AncientRelicConfiguration();
        }
    }

    public static void Save(AncientRelicConfiguration config)
    {
        try
        {
            string configPath = GetConfigPath();
            string? directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = configPath + ".tmp";
            string json = JsonSerializer.Serialize(ToPersisted(config), JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, configPath, true);
        }
        catch (Exception exception)
        {
            CuztomizableAncientsMod.Logger.Warn(
                $"Could not save configuration: {exception.Message}");
        }
    }

    private static string GetConfigPath()
    {
        return Path.Combine(OS.GetUserDataDir(), "mods", CuztomizableAncientsMod.ModId, "config.json");
    }

    private static PersistedSettings ToPersisted(AncientRelicConfiguration config)
    {
        return new PersistedSettings
        {
            Version = CurrentVersion,
            DisabledRelics = config.DisabledRelics.Select(id => id.ToString()).ToList(),
            CustomPools = config.CustomPools.Values.Select(pool => new PersistedCustomPool
            {
                Id = pool.Id,
                DisplayName = pool.DisplayName,
                Author = pool.Author,
                Description = pool.Description,
                RelicIds = pool.RelicIds.Select(id => id.ToString()).ToList()
            }).ToList(),
            Ancients = config.AncientConfigs
                .Where(pair => pair.Value.IsCustomized)
                .Select(pair => new PersistedAncient
                {
                    AncientId = pair.Key.ToString(),
                    IsCustomized = pair.Value.IsCustomized,
                    Options = pair.Value.Options.Select(option => new PersistedOption
                    {
                        OptionIndex = option.OptionIndex,
                        PoolIds = option.PoolIds.ToList()
                    }).ToList()
                }).ToList()
        };
    }

    private static AncientRelicConfiguration FromPersisted(PersistedSettings persisted)
    {
        AncientRelicConfiguration config = new();

        foreach (string id in persisted.DisabledRelics ?? [])
        {
            if (TryParseModelId(id, out ModelId modelId))
            {
                config.DisabledRelics.Add(modelId);
            }
        }

        foreach (PersistedCustomPool pool in persisted.CustomPools ?? [])
        {
            if (string.IsNullOrWhiteSpace(pool.Id) || string.IsNullOrWhiteSpace(pool.DisplayName))
            {
                continue;
            }

            config.CustomPools[pool.Id] = new CustomRelicPoolConfig(
                pool.Id,
                pool.DisplayName,
                (pool.RelicIds ?? []).Select(ParseModelIdOrNone).Where(id => id != ModelId.none),
                pool.Author,
                pool.Description);
        }

        foreach (PersistedAncient ancient in persisted.Ancients ?? [])
        {
            if (!TryParseModelId(ancient.AncientId, out ModelId ancientId))
            {
                continue;
            }

            AncientRelicConfig ancientConfig = new(ancientId)
            {
                IsCustomized = ancient.IsCustomized
            };
            ancientConfig.Options.AddRange((ancient.Options ?? []).Select(option =>
                new AncientOptionConfig(option.OptionIndex, option.PoolIds ?? [])));
            ancientConfig.EnsureValidShape();
            config.AncientConfigs[ancientId] = ancientConfig;
        }

        config.EnsureValidShape();
        return config;
    }

    private static ModelId ParseModelIdOrNone(string value)
    {
        return TryParseModelId(value, out ModelId id) ? id : ModelId.none;
    }

    private static bool TryParseModelId(string value, out ModelId id)
    {
        try
        {
            id = ModelId.Deserialize(value);
            return id != ModelId.none;
        }
        catch
        {
            id = ModelId.none;
            return false;
        }
    }

    public sealed class PersistedSettings
    {
        public int Version { get; set; } = CurrentVersion;
        public List<string> DisabledRelics { get; set; } = [];
        public List<PersistedCustomPool> CustomPools { get; set; } = [];
        public List<PersistedAncient> Ancients { get; set; } = [];
    }

    public sealed class PersistedCustomPool
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Author { get; set; } = "You";
        public string? Description { get; set; }
        public List<string> RelicIds { get; set; } = [];
    }

    public sealed class PersistedAncient
    {
        public string AncientId { get; set; } = string.Empty;
        public bool IsCustomized { get; set; }
        public List<PersistedOption> Options { get; set; } = [];
    }

    public sealed class PersistedOption
    {
        public int OptionIndex { get; set; }
        public List<string> PoolIds { get; set; } = [];
    }
}

using BaseLib.Config;

namespace CuztomizableAncients.Configuration;

public enum LauncherCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public sealed class CuztomizableAncientsConfig : SimpleModConfig
{
    [ConfigSection("Menu button position")]
    public static LauncherCorner Corner { get; set; } = LauncherCorner.TopRight;

    [ConfigTextInput("\\d+")]
    public static string XOffset { get; set; } = "24";

    [ConfigTextInput("\\d+")]
    public static string YOffset { get; set; } = "24";

    public static event Action? PlacementChanged;

    public static void Register()
    {
        CuztomizableAncientsConfig config = new();
        ModConfigRegistry.Register(CuztomizableAncientsMod.ModId, config);
        config.ConfigChanged += (_, _) =>
        {
            config.SaveDebounced();
            PlacementChanged?.Invoke();
        };
        config.OnConfigReloaded += () => PlacementChanged?.Invoke();
    }
}

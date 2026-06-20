using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace CuztomizableAncients;

[ModInitializer(nameof(Initialize))]
public partial class CuztomizableAncientsMod : Node
{
    public const string ModId = "CuztomizableAncients";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }
}

using CuztomizableAncients.Configuration;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;

namespace CuztomizableAncients.Patches;

[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor, [typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int)])]
public static class StartRunLobbyPatch
{
    public static void Postfix(StartRunLobby __instance)
    {
        __instance.NetService.RegisterMessageHandler<AncientRelicConfigMessage>(OnConfigMessage);
    }

    private static void OnConfigMessage(AncientRelicConfigMessage message, ulong senderId)
    {
        AncientRelicConfigService.ActiveConfig = message.Config;
        CuztomizableAncientsMod.Logger.Info($"Received ancient relic config from {senderId}.");
    }
}

[HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
public static class StartRunLobbyBeginRunPatch
{
    public static void Prefix(StartRunLobby __instance)
    {
        AncientRelicConfigService.Broadcast(__instance.NetService);
    }
}

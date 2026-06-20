using System.Reflection;
using CuztomizableAncients.Configuration;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace CuztomizableAncients.Patches;

[HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
public static class AncientGenerateInitialOptionsPatch
{
    private static readonly MethodInfo RelicOptionMethod = AccessTools.Method(
        typeof(AncientEventModel),
        "RelicOption",
        [typeof(RelicModel), typeof(string), typeof(string)])!;

    private static readonly FieldInfo GeneratedOptionsField = AccessTools.Field(
        typeof(AncientEventModel),
        "_generatedOptions")!;

    private static readonly PropertyInfo CustomDonePageProperty = AccessTools.Property(
        typeof(AncientEventModel),
        "CustomDonePage")!;

    private static readonly MethodInfo DoneMethod = AccessTools.Method(
        typeof(AncientEventModel),
        "Done")!;

    public static void Postfix(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null || (__result.Count == 1 && __result[0].IsProceed))
        {
            return;
        }

        AncientRelicConfig config = AncientRelicConfigService.ActiveConfig.GetAncientConfig(__instance.Id);
        if (!config.IsCustomized)
        {
            return;
        }

        if (__instance is Neow && __instance.Owner.RunState.Modifiers.Count > 0)
        {
            return;
        }

        List<EventOption> options = [];
        HashSet<ModelId> chosenRelics = [];
        foreach (AncientOptionConfig slot in config.Options.OrderBy(slot => slot.OptionIndex))
        {
            List<RelicModel> candidates = AncientRelicConfigService
                .GetCandidatesForOption(__instance.Id, slot.OptionIndex, __instance.Owner)
                .Where(relic => !chosenRelics.Contains(relic.Id))
                .ToList();
            if (candidates.Count == 0)
            {
                CuztomizableAncientsMod.Logger.Warn($"No configured relic candidates for ancient {__instance.Id}, slot {slot.OptionIndex}.");
                continue;
            }

            RelicModel? selectedRelic = __instance.Rng!.NextItem(candidates);
            if (selectedRelic == null)
            {
                continue;
            }

            RelicModel relic = selectedRelic.IsMutable
                ? (RelicModel)selectedRelic.MutableClone()
                : selectedRelic.ToMutable();
            chosenRelics.Add(relic.Id);
            options.Add(CreateRelicOption(__instance, relic));
        }

        GeneratedOptionsField.SetValue(__instance, options);
        __result = options;
    }

    private static EventOption CreateRelicOption(AncientEventModel ancient, RelicModel relic)
    {
        string? donePage = ancient is Neow ? AncientRelicConfigService.GetNeowDonePageFor(relic) : null;
        if (ModelDb.AllRelicPools.Any(pool => pool.AllRelicIds.Contains(relic.Id)))
        {
            return (EventOption)RelicOptionMethod.Invoke(ancient, [relic, "INITIAL", donePage])!;
        }

        CuztomizableAncientsMod.Logger.Warn(
            $"Relic {relic.Id} is not in any game RelicPoolModel; using fallback ancient option rendering.");
        return CreatePoollessRelicOption(ancient, relic, donePage);
    }

    private static EventOption CreatePoollessRelicOption(
        AncientEventModel ancient,
        RelicModel relic,
        string? donePage)
    {
        LocString title = ancient.GetOptionTitle("INITIAL") ?? relic.Title;
        LocString description = ancient.GetOptionDescription("INITIAL") ??
                                LocString.GetIfExists("relics", $"{relic.Id.Entry}.eventDescription") ??
                                new LocString("relics", $"{relic.Id.Entry}.description");
        relic.DynamicVars.AddTo(description);
        description.Add("energyPrefix", ancient.Owner!.Character.CardPool.EnergyColorName);
        description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");

        async Task OnChosen()
        {
            await RelicCmd.Obtain(relic, ancient.Owner);
            CustomDonePageProperty.SetValue(ancient, donePage);
            DoneMethod.Invoke(ancient, null);
        }

        return new EventOption(
                ancient,
                OnChosen,
                title,
                description,
                "INITIAL",
                relic.HoverTipsExcludingRelic)
            .WithRelic(relic);
    }
}

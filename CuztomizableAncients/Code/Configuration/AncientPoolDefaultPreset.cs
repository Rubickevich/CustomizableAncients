using MegaCrit.Sts2.Core.Models;

namespace CuztomizableAncients.Configuration;

public sealed class AncientPoolDefaultPreset
{
    public AncientPoolDefaultPreset(ModelId ancientId, params int[] optionNumbers)
    {
        if (ancientId == ModelId.none)
        {
            throw new ArgumentException("Ancient ID cannot be none.", nameof(ancientId));
        }

        if (optionNumbers.Length == 0)
        {
            throw new ArgumentException("At least one option number is required.", nameof(optionNumbers));
        }

        if (optionNumbers.Any(number => number < 1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(optionNumbers),
                "Option numbers are one-based and must be at least 1.");
        }

        AncientId = ancientId;
        OptionIndices = optionNumbers
            .Distinct()
            .Select(number => number - 1)
            .ToList();
    }

    public ModelId AncientId { get; }

    public IReadOnlyList<int> OptionIndices { get; }

    public static AncientPoolDefaultPreset For<TAncient>(params int[] optionNumbers)
        where TAncient : AncientEventModel
    {
        return new AncientPoolDefaultPreset(ModelDb.GetId<TAncient>(), optionNumbers);
    }
}

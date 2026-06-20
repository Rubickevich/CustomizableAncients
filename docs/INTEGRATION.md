# Cuztomizable Ancients Integration

Cuztomizable Ancients automatically discovers modded relics, relic pool models, and ancients. Integration is only
needed when your pool should appear in specific ancient options by default.

Defaults do not lock configuration. Hosts may add, move, disable, or remove pools and options normally. Resetting
an ancient reapplies vanilla defaults plus all registered presets.

## Dependency

Add `CuztomizableAncients` as a dependency in your mod manifest and reference its DLL without copying it into your mod:

```json
"dependencies": [
  {
    "id": "CuztomizableAncients",
    "min_version": "v0.0.0"
  }
]
```

```xml
<Reference Include="CuztomizableAncients">
  <HintPath>$(ModsPath)CuztomizableAncients/CuztomizableAncients.dll</HintPath>
  <Private>false</Private>
</Reference>
```

Register presets from your mod initializer. Registration must happen before the character-select lobby opens.

## Existing RelicPoolModel

Every modded `RelicPoolModel` is discovered automatically. Attach defaults with the generic helper:

```csharp
using CuztomizableAncients.Configuration;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Events;

[ModInitializer(nameof(Initialize))]
public partial class MyModEntry : Node
{
    public static void Initialize()
    {
        AncientRelicPools.RegisterDefaultPresets<MyRelicPool>(
            AncientPoolDefaultPreset.For<Neow>(1, 2),
            AncientPoolDefaultPreset.For<MyAncient>(3));
    }
}
```

Option numbers are one-based, matching the UI. The example adds `MyRelicPool` to Neow options 1 and 2 and to
`MyAncient` option 3. Multiple pools may target the same option.

## Custom Logical Pool

Register a logical pool when no `RelicPoolModel` represents the exact set you need:

```csharp
using CuztomizableAncients.Configuration;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

ModelId neowId = ModelDb.GetId<Neow>();

AncientRelicPools.Register(
    new AncientRelicPoolDefinition(
        id: "MyMod:NeowRewards",
        displayName: "My Neow Rewards",
        relics: () =>
        [
            ModelDb.Relic<MyFirstRelic>(),
            ModelDb.Relic<MySecondRelic>()
        ],
        author: "My Mod Team",
        description: "Relics intended for Neow.",
        nativeAncientIds: [neowId],
        isModded: true),
    AncientPoolDefaultPreset.For<Neow>(1, 2));
```

Pool IDs must be stable and globally unique. Prefix them with your mod ID. The relic delegate is evaluated lazily,
so return current canonical models from `ModelDb` when possible.

## Runtime Behavior

- Presets append pools to existing defaults; they do not replace vanilla pools.
- A preset may target an option beyond the ancient's normal count. Missing options are created.
- An ancient with at least one preset uses Cuztomizable Ancients generation without requiring host interaction.
- Host changes remain authoritative and synchronize when the run starts.
- Customized copies never mutate the source `RelicPoolModel` or vanilla drop behavior.
- Registering the same pool ID twice throws an exception.
- Repeating the same pool, ancient, and option placement is ignored.

## Non-Generic Registration

When a pool ID is only known at runtime:

```csharp
string poolId = AncientRelicPools.GetAutomaticPoolId(relicPoolModel.Id);
AncientRelicPools.RegisterDefaultPresets(
    poolId,
    new AncientPoolDefaultPreset(ancientModel.Id, 1, 3));
```

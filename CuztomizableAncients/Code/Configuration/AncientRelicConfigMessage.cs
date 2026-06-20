using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuztomizableAncients.Configuration;

public struct AncientRelicConfigMessage : INetMessage
{
    public AncientRelicConfiguration Config;

    public AncientRelicConfigMessage(AncientRelicConfiguration config)
    {
        Config = config.Clone();
    }

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        Config.Serialize(writer);
    }

    public void Deserialize(PacketReader reader)
    {
        Config = AncientRelicConfiguration.Deserialize(reader);
    }
}

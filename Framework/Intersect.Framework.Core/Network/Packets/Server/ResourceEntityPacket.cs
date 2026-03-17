using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class ResourceEntityPacket : EntityPacket
{
    //Parameterless Constructor for MessagePack
    public ResourceEntityPacket()
    {
    }


    [Key(25)]
    public Guid ResourceId { get; set; }


    [Key(26)]
    public bool IsDead { get; set; }

}

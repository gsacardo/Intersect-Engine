using Intersect.Collections;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.GameObjects;
using Intersect.Utilities;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class CreateCharacterPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public CreateCharacterPacket()
    {
    }

    public CreateCharacterPacket(string name, Guid classId, int sprite, string hair, Color? hairColor = null)
    {
        Name = name;
        ClassId = classId;
        Sprite = sprite;
        Hair = hair;
        HairColor = hairColor ?? Color.White;
    }

    [Key(0)]
    public string Name { get; set; }

    [Key(1)]
    public Guid ClassId { get; set; }

    [Key(2)]
    public int Sprite { get; set; }

    [Key(3)]
    public string Hair { get; set; }

    [Key(4)]
    public Color HairColor { get; set; } = Color.White;

    public override Dictionary<string, SanitizedValue<object>> Sanitize()
    {
        base.Sanitize();

        var sanitizer = new Sanitizer();

        var classDescriptor = ClassDescriptor.Get(ClassId);
        if (classDescriptor != null)
        {
            var maxSpriteIndex = Math.Max(0, (classDescriptor.Sprites?.Count ?? 1) - 1);
            Sprite = sanitizer.Clamp(nameof(Sprite), Sprite, 0, maxSpriteIndex);
            if (!string.IsNullOrEmpty(Hair) && !(classDescriptor.Hairs?.Contains(Hair) ?? false))
            {
                Hair = string.Empty;
            }
        }

        return sanitizer.Sanitized;
    }

}

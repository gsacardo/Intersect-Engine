using System.Runtime.Serialization;
using Intersect.Framework.Annotations;
using Newtonsoft.Json;

namespace Intersect.Config;

public partial class PaperdollOptions
{
    [Ignore]
    [JsonIgnore]
    public List<string>[] Directions;

    public List<string> Down { get; set; } =
    [
        "Player",
        "Hair",
        "Armor",
        "Helmet",
        "Weapon",
        "Shield",
        "Boots",
    ];

    public List<string> Left { get; set; } =
    [
        "Player",
        "Hair",
        "Armor",
        "Helmet",
        "Weapon",
        "Shield",
        "Boots",
    ];

    public List<string> Right { get; set; } =
    [
        "Player",
        "Hair",
        "Armor",
        "Helmet",
        "Weapon",
        "Shield",
        "Boots",
    ];

    public List<string> Up { get; set; } =
    [
        "Player",
        "Hair",
        "Armor",
        "Helmet",
        "Weapon",
        "Shield",
        "Boots",
    ];

    public PaperdollOptions()
    {
        EnsureHairLayer(Up);
        EnsureHairLayer(Down);
        EnsureHairLayer(Left);
        EnsureHairLayer(Right);
        Directions =
        [
            Up,
            Down,
            Left,
            Right,
        ];
    }

    [OnDeserializing]
    internal void OnDeserializingMethod(StreamingContext context)
    {
        Up.Clear();
        Down.Clear();
        Left.Clear();
        Right.Clear();
    }

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        Up = NormalizeDirection(Up);
        Down = NormalizeDirection(Down);
        Left = NormalizeDirection(Left);
        Right = NormalizeDirection(Right);
        Directions =
        [
            Up,
            Down,
            Left,
            Right,
        ];
    }

    public void Validate(EquipmentOptions equipment)
    {
        foreach (var direction in Directions)
        {
            var hasPlayer = false;
            foreach (var item in direction)
            {
                if (item == "Player")
                {
                    hasPlayer = true;
                }

                if (!equipment.Slots.Contains(item) && item != "Player" && item != "Hair")
                {
                    throw new Exception($"Config Error: Paperdoll item {item} does not exist in equipment slots!");
                }
            }

            if (!hasPlayer)
            {
                throw new Exception($"Config Error: Paperdoll direction {direction} does not have Player listed!");
            }
        }
    }

    private static List<string> NormalizeDirection(List<string> direction)
    {
        direction ??= [];
        var normalized = direction.Distinct().ToList();
        EnsureHairLayer(normalized);
        return normalized;
    }

    private static void EnsureHairLayer(List<string> direction)
    {
        if (direction == null)
        {
            return;
        }

        direction.RemoveAll(item => item == "Hair");

        var playerIndex = direction.IndexOf("Player");
        if (playerIndex < 0)
        {
            direction.Insert(0, "Hair");
            return;
        }

        direction.Insert(playerIndex + 1, "Hair");
    }
}

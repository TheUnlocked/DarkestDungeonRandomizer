using DarkestDungeonRandomizer.DDFileTypes;

namespace DarkestDungeonRandomizer.DDTypes;

public record Monster(string Name)
{
    public int Size { get; init; } = 1;
    public string EnemyTypeId { get; init; } = "";
    public int Health { get; init; } = 0;

    public static Monster FromDarkest(string name, Darkest file)
    {
        return new Monster(name)
        {
            Name = name,
            Size = file.Entries["display"][0].Properties["size"][0].TryParseInt()!.Value,
            EnemyTypeId = file.Entries["enemy_type"][0].Properties["id"][0][1..^1],
            Health = file.Entries["stats"][0].Properties["hp"][0].TryParseInt()!.Value,
        };
    }
}

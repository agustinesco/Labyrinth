namespace Labyrinth.Items
{
    public enum ItemType
    {
        None,
        Speed,
        Light,
        Heal,
        Explosive,
        Key, // Key is special - collected immediately, not stored
        XP, // XP is collected immediately for leveling
        Pebbles, // Multi-use item that drops pebbles on the ground
        Invisibility, // Makes player undetectable by enemies for a duration
        Wisp, // Spawns an orb that pathfinds to the key, leaving a fading trail
        Caltrops, // Scatter caltrops that slow enemies (and damage player if stepped on)
        EchoStone // Sonar pulse that reveals enemy positions through walls
    }
}

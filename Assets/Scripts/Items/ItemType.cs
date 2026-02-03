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
        XP // XP is collected immediately for leveling
    }
}

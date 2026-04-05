using TogawaSakiko.TogawaSakikoCode.Core;

namespace TogawaSakiko.TogawaSakikoCode.Util;

public static class ResourcePaths
{
    public static string Image(string path) => Path.Join(MainFile.ModId, "images", path);
    public static string Audio(string path) => Path.Join(MainFile.ModId, "audio", path);

    public static string Card(string path) => Path.Join(MainFile.ModId, "images", "card_portraits", path);
    public static string BigCard(string path) => Path.Join(MainFile.ModId, "images", "card_portraits", "big", path);

    public static string Power(string path) => Path.Join(MainFile.ModId, "images", "powers", path);
    public static string BigPower(string path) => Path.Join(MainFile.ModId, "images", "powers", "big", path);

    public static string Relic(string path) => Path.Join(MainFile.ModId, "images", "relics", path);
    public static string BigRelic(string path) => Path.Join(MainFile.ModId, "images", "relics", "big", path);

    public static string CharacterUi(string path) => Path.Join(MainFile.ModId, "images", "charui", path);

    // New STS1-style aliases for future content organization.
    public static string CardsImage(string path) => Path.Join(MainFile.ModId, "images", "cards", path);
    public static string CharacterImage(string path) => Path.Join(MainFile.ModId, "images", "character", path);
    public static string PotionImage(string path) => Path.Join(MainFile.ModId, "images", "potions", path);
    public static string UiImage(string path) => Path.Join(MainFile.ModId, "images", "ui", path);
    public static string VfxImage(string path) => Path.Join(MainFile.ModId, "images", "vfx", path);
    public static string MapImage(string path) => Path.Join(MainFile.ModId, "images", "map", path);
    public static string ScreenImage(string path) => Path.Join(MainFile.ModId, "images", "screens", path);
}

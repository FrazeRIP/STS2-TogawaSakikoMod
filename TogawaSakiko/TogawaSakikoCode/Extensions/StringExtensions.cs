using TogawaSakiko.TogawaSakikoCode.Util;

namespace TogawaSakiko.TogawaSakikoCode.Extensions;

// Mostly utilities to get asset paths.
// Maintained as a compatibility shim while the repo migrates to STS1-style structure.
public static class StringExtensions
{
    public static string ImagePath(this string path) => ResourcePaths.Image(path);

    public static string CardImagePath(this string path) => ResourcePaths.Card(path);

    public static string BigCardImagePath(this string path) => ResourcePaths.BigCard(path);

    public static string PowerImagePath(this string path) => ResourcePaths.Power(path);

    public static string BigPowerImagePath(this string path) => ResourcePaths.BigPower(path);

    public static string RelicImagePath(this string path) => ResourcePaths.Relic(path);

    public static string BigRelicImagePath(this string path) => ResourcePaths.BigRelic(path);

    public static string CharacterUiPath(this string path) => ResourcePaths.CharacterUi(path);

    // New STS1-style folder aliases.
    public static string CardsImagePath(this string path) => ResourcePaths.CardsImage(path);
    public static string CharacterImagePath(this string path) => ResourcePaths.CharacterImage(path);
    public static string PotionImagePath(this string path) => ResourcePaths.PotionImage(path);
    public static string UiImagePath(this string path) => ResourcePaths.UiImage(path);
    public static string VfxImagePath(this string path) => ResourcePaths.VfxImage(path);
    public static string MapImagePath(this string path) => ResourcePaths.MapImage(path);
    public static string ScreenImagePath(this string path) => ResourcePaths.ScreenImage(path);
}

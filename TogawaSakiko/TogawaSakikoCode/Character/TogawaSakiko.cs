using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.TogawaSakikoCode.Cards.Basic;
using TogawaSakiko.TogawaSakikoCode.Extensions;
using TogawaSakiko.TogawaSakikoCode.Relics;

namespace TogawaSakiko.TogawaSakikoCode.Character;

public class TogawaSakiko : PlaceholderCharacterModel
{
    public const string CharacterId = "TogawaSakiko";

    public override string PlaceholderID => "ironclad";

    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Female;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<StarterRelicTogawaSakiko>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<TogawaSakikoCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TogawaSakikoRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TogawaSakikoPotionPool>();

    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets.
        These are just some of the simplest assets, given some placeholders to differentiate your character with.
        You don't have to, but you're suggested to rename these images. */
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}

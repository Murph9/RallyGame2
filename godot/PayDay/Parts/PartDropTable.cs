using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Utilities;
using System.Linq;

namespace murph9.RallyGame2.godot.PayDay.Parts;

public static class PartDropTable {
    /// <summary>
    /// Roll a random part from the player's current car and assign a rarity.
    /// For MVP, part stats are unchanged — rarity is cosmetic only.
    /// </summary>
    public static CollectedPart Generate(CarDetails carDetails, int dayNumber) {
        var allParts = carDetails.GetAllPartsInTree().ToList();
        var part = RandHelper.RandFromList(allParts);
        var rarity = PartRarityHelper.RollRarity(dayNumber);
        return new CollectedPart(part, rarity);
    }
}

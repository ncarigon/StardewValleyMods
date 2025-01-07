using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using Microsoft.Xna.Framework;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;

namespace BetterHoneyMead {
    internal static class CreateItems {
        public static Item OutputFlavoredMead(SObject machine, Item? ingredient, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady) {
            overrideMinutesUntilReady = null;
            var mead = new SObject("459", 1);
            var preservedItem = ItemRegistry.GetDataOrErrorItem((ingredient as SObject)?.preservedParentSheetIndex.Value);
            if (!(preservedItem?.InternalName?.Equals("ErrorItem") ?? true)) {
                var color = TailoringMenu.GetDyeColor(ingredient) ?? TailoringMenu.GetDyeColor(mead) ?? Color.Gold;
                if (ColoredObject.TrySetColor(mead, color, out var co)) {
                    mead = co;
                }
                mead.Name = preservedItem.InternalName + " Mead";
                if ((outputData?.CustomData?.TryGetValue("FlavoredDisplayName", out var name) ?? false) && !string.IsNullOrWhiteSpace(name)) {
                    mead.displayNameFormat = name;
                }
            }
            return mead;
        }
    }
}

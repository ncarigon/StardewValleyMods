using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace BetterHoneyMead.ModPatches {
    internal static class ItemConverter {
        public static void Register() {
            ModEntry.ModHarmony?.Patch(
                    original: AccessTools.Method(typeof(Farmer), "addItemToInventory", new Type[] { typeof(Item) }),
                    prefix: new HarmonyMethod(typeof(ItemConverter), nameof(Prefix_Farmer_addItemToInventory))
                );
        }

        private static void Prefix_Farmer_addItemToInventory(
            ref Item item
        ) {
            try {
                var uid = item?.ItemId ?? "";
                var qid = item?.QualifiedItemId ?? "";
                if ((qid.Equals("(O)459")
                    || qid.Equals("(O)340"))
                    && item is SObject oldObj && oldObj is not null
                ) {
                    Item? newItem = null;
                    var spriteIndex = Game1.objectData.Where(o => o.Key.Equals(uid) || o.Key.Equals(qid)).FirstOrDefault().Value?.SpriteIndex ?? -1;
                    var shouldUseColor = spriteIndex == 0;
                    var isUsingColor = oldObj.ParentSheetIndex == 0;
                    var isColored = oldObj is ColoredObject;
                    if (shouldUseColor) {
                        if (!isUsingColor) {
                            oldObj.ParentSheetIndex = 0;
                        }
                        if (!isColored) {
                            var ingredient = ItemRegistry.Create<SObject>(oldObj.preservedParentSheetIndex.Value, 1, 0, true);
                            var color = TailoringMenu.GetDyeColor(ingredient) ?? Color.Gold;
                            newItem = new ColoredObject(oldObj.ItemId, oldObj.Stack, color);
                        }
                    } else {
                        if (isUsingColor && int.TryParse(oldObj.ItemId, out var id)) {
                            oldObj.ParentSheetIndex = id;
                        }
                        if (isColored) {
                            newItem = ItemRegistry.Create(oldObj.QualifiedItemId, oldObj.Stack, oldObj.Quality);
                        }
                    }
                    if (newItem is not null && newItem is SObject newObj && newObj is not null) {
                        newObj.preservedParentSheetIndex.Value = oldObj.preservedParentSheetIndex.Value;
                        newObj.ParentSheetIndex = oldObj.ParentSheetIndex;
                        newObj.Price = oldObj.Price;
                        newObj.Name = oldObj.Name;
                        if (!string.IsNullOrWhiteSpace(newObj.preservedParentSheetIndex.Value)) {
                            newObj.displayNameFormat = "[LocalizedText Strings/Objects:NCarigon.BetterHoneyMeadCP_Flavor %PRESERVED_DISPLAY_NAME %DISPLAY_NAME]";
                        }
                        item = newItem;
                    }
                }
            } catch { }
        }
    }
}

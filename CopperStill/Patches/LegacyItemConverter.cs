using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace CopperStill.Patches {
    internal static class LegacyItemConverter {
        public static void Register() {
            ModEntry.Instance?.ModHarmony?.Patch(
                    original: AccessTools.Method(typeof(Farmer), "addItemToInventory", new Type[] { typeof(Item) }),
                    prefix: new HarmonyMethod(typeof(LegacyItemConverter), nameof(Prefix_Farmer_addItemToInventory))
                );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(ItemRegistry), "GetDataOrErrorItem"),
                prefix: new HarmonyMethod(typeof(LegacyItemConverter), nameof(Prefix_ItemRegistry_GetDataOrErrorItem))
            );
        }

        private class LegacyItemConversion {
            public string ItemType { get; private set; }
            public bool IsColored { get; private set; }
            public string CurrentId { get; private set; }
            public string[] LegacyIds { get; private set; }

            public LegacyItemConversion(string itemType, string currentId, bool isColored, params string[] legacyIds) {
                this.ItemType = itemType;
                this.IsColored = isColored;
                this.CurrentId = currentId;
                this.LegacyIds = new List<string>() { currentId }.Concat(legacyIds ?? Array.Empty<string>()).ToArray();
            }
        }

        private static readonly LegacyItemConversion[] CopperStillItems = new LegacyItemConversion [] {
            new LegacyItemConversion("(BC)", "Still", false),
            new LegacyItemConversion("(O)", "JuniperBerry", false, "Juniper_Berry"),
            new LegacyItemConversion("(O)", "Brandy", true),
            new LegacyItemConversion("(O)", "Vodka", true),
            new LegacyItemConversion("(O)", "Gin", true),
            new LegacyItemConversion("(O)", "Moonshine", false),
            new LegacyItemConversion("(O)", "Whiskey", false),
            new LegacyItemConversion("(O)", "Sake", false),
            new LegacyItemConversion("(O)", "Soju", false),
            new LegacyItemConversion("(O)", "TequilaBlanco", false, "Tequila_Blanco"),
            new LegacyItemConversion("(O)", "TequilaAnejo", false, "Tequila_Anejo"),
            new LegacyItemConversion("(O)", "RumWhite", false, "White_Rum"),
            new LegacyItemConversion("(O)", "RumDark", false, "Dark_Rum"),
            new LegacyItemConversion("(O)", "Mash", true, "NCarigon.MoreSensibleJuicesCP_Mash") // originally from MoreSensibleJuices mod
        };

        private static void Prefix_Farmer_addItemToInventory(
            ref Item item
        ) {
            var legacyObj = item as SObject;
            if (legacyObj is not null && TrySwapLegacyItem(legacyObj, out var currentObj) && currentObj is not null) {
                currentObj.preservedParentSheetIndex.Value = legacyObj.preservedParentSheetIndex.Value;
                currentObj.Quality = legacyObj.Quality;
                currentObj.Price = legacyObj.Price;
                currentObj.Stack = legacyObj.Stack;
                currentObj.Name = legacyObj.Name;
                if (!string.IsNullOrWhiteSpace(currentObj.preservedParentSheetIndex.Value)) {
                    currentObj.displayNameFormat = "[LocalizedText Strings/Objects:NCarigon.CopperStillCP_Flavor %PRESERVED_DISPLAY_NAME %DISPLAY_NAME]";
                }
                item = currentObj;
            }
        }

        private static void Prefix_ItemRegistry_GetDataOrErrorItem(
            ref string itemId
        ) {
            if (TrySwapLegacyItem(itemId, out var currentId) && currentId is not null) {
                itemId = currentId;
            }
        }

        private static bool TrySwapLegacyItem(string oldId, out string? newId) {
            foreach (var legacyItem in CopperStillItems) {
                foreach (var legacyId in legacyItem.LegacyIds) {
                    if (string.Compare(oldId, $"{legacyItem.ItemType}{legacyId}") == 0
                        || string.Compare(oldId, $"{legacyItem.ItemType}NCarigon.CopperStillJA_{legacyId}") == 0
                    ) {
                        newId = $"{legacyItem.ItemType}NCarigon.CopperStillCP_{legacyItem.CurrentId}";
                        return true;
                    }
                }
            }
            newId = null;
            return false;
        }

        private static bool TrySwapLegacyItem(SObject oldItem, out SObject? newItem) {
            foreach (var legacyItem in CopperStillItems) {
                foreach (var legacyId in legacyItem.LegacyIds) {
                    if (string.Compare(oldItem.QualifiedItemId, $"{legacyItem.ItemType}{legacyId}") == 0
                        || string.Compare(oldItem.QualifiedItemId, $"{legacyItem.ItemType}NCarigon.CopperStillJA_{legacyId}") == 0
                    ) {
                        newItem = legacyItem.IsColored
                            ? new ColoredObject($"NCarigon.CopperStillCP_{legacyItem.CurrentId}", 1, TailoringMenu.GetDyeColor(ItemRegistry.Create($"(O){oldItem.preservedParentSheetIndex.Value}")) ?? Color.White)
                            : ItemRegistry.Create($"{legacyItem.ItemType}NCarigon.CopperStillCP_{legacyItem.CurrentId}") as SObject;
                        return true;
                    }
                }
            }
            newItem = null;
            return false;
        }
    }
}

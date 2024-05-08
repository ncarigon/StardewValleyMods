using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace CopperStill.Patches {
    internal static class ItemSpawner {
        public static void Register() {
            if (ModEntry.Instance?.Helper?.ModRegistry.IsLoaded("CJBok.ItemSpawner") ?? false) {
                ModEntry.Instance?.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.Framework.ItemData.ItemRepository:GetFlavoredObjectVariants"),
                    postfix: new HarmonyMethod(typeof(ItemSpawner), nameof(Postfix_ItemRepository_GetFlavoredObjectVariants))
                );
            }
        }

        private static readonly Dictionary<int, string[]> ItemMap = new() {
            { -79, new string[] { "Brandy" } },
            { -75, new string[] { "Mash", "Vodka", "Gin" } }
        };

        private static IEnumerable<object> Postfix_ItemRepository_GetFlavoredObjectVariants(
            IEnumerable<object> values,
            object __instance,
            ObjectDataDefinition objectDataDefinition, SObject? item, IItemDataDefinition itemType

        ) {
            IList? results = null;
            if (item is not null) {
                var tryCreate = ModEntry.Instance?.Helper.Reflection.GetMethod(__instance, "TryCreate", false);
                if (tryCreate is not null && ItemMap.TryGetValue(item.Category, out var names)) {
                    foreach (var name in names) {
                        var newItem = tryCreate.Invoke<object>(new object[] { //also makes brandy
                            itemType.Identifier,
                            $"NCarigon.CopperStillCP_{name}/{item.ItemId}",
                            (object p) => CreateFlavoredType(item, name)
                        });
                        if (newItem is not null) {
                            if (results is null) {
                                var listType = typeof(List<>).MakeGenericType(newItem.GetType());
                                if (listType is not null) {
                                    results = Activator.CreateInstance(listType) as IList;
                                    if (results is not null) {
                                        foreach (var value in values) {
                                            results.Add(value);
                                        }
                                    }
                                }
                            }
                            results?.Add(newItem);
                        }
                    }
                }
            }
            return results as IEnumerable<object> ?? values;
        }

        private static SObject? CreateFlavoredType(SObject ingredient, string type) {
            var color = TailoringMenu.GetDyeColor(ingredient) ?? Color.White;
            var item = new ColoredObject($"NCarigon.CopperStillCP_{type}", 999, color);
            try {
                item.displayNameFormat = "%PRESERVED_DISPLAY_NAME %DISPLAY_NAME";
                item.preservedParentSheetIndex.Value = ingredient.ItemId;
                var price = AdjustPricing.CalcPrice(item.Name, ingredient.Name);
                if (price > 0) {
                    item.Price = price;
                }
                item.Name += $"_{ingredient.Name}";
            } catch { }
            return item;
        }
    }
}
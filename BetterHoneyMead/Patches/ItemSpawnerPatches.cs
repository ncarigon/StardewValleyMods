using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections;
using static StardewValley.GameData.QuantityModifier;
using SObject = StardewValley.Object;

namespace BetterHoneyMead.Patches {
    internal static class ItemSpawnerPatches {
        public static void Register() {
            if (ModEntry.Instance?.Helper?.ModRegistry.IsLoaded("CJBok.ItemSpawner") ?? false) {
                ModEntry.Instance?.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.Framework.ItemData.ItemRepository:GetFlavoredObjectVariants"),
                    postfix: new HarmonyMethod(typeof(ItemSpawnerPatches), nameof(Postfix_ItemRepository_GetFlavoredObjectVariants))
                );
            }
        }

        public static int GetPrice(string ingredient) =>
            Game1.objectData.FirstOrDefault(o => o.Value.Name.Equals(ingredient)).Value?.Price ?? 0;

        public static int CalcPrice(string inputName, string? ingredient = null) {
            var inputMult = DataLoader.Machines(Game1.content)?
                .GetValueOrDefault($"(BC)12")?
                .OutputRules?.FirstOrDefault(o => o?.Id?.Equals("Default_Honey") ?? false)?
                .OutputItem?.FirstOrDefault(o => o?.Id?.Equals("(O)459") ?? false)?
                .PriceModifiers?.FirstOrDefault(o => o?.Modification.Equals(ModificationType.Multiply) ?? false)?
                .Amount ?? 1.5;
            var baseAdd = DataLoader.Machines(Game1.content)?
                .GetValueOrDefault($"(BC)12")?
                .OutputRules?.FirstOrDefault(o => o?.Id?.Equals("Default_Honey") ?? false)?
                .OutputItem?.FirstOrDefault(o => o?.Id?.Equals("(O)459") ?? false)?
                .PriceModifiers?.FirstOrDefault(o => o?.Modification.Equals(ModificationType.Add) ?? false)?
                .Amount ?? 150;
            return inputName switch {
                "Honey" => GetPrice("Honey") + (ingredient is null ? 0 : GetPrice(ingredient) * 2),
                "Mead" => (int)(CalcPrice("Honey", ingredient) * inputMult + baseAdd),

                _ => 0,
            };
        }

        private static readonly Dictionary<int, string[]> ItemMap = new() {
            { -80, new[] { "459" } } // mead
        };

        private static IReflectedMethod? TryCreate;

        private static IEnumerable<object> Postfix_ItemRepository_GetFlavoredObjectVariants(
            IEnumerable<object> values,
            object __instance,
            SObject? item, IItemDataDefinition itemType
        ) {
            IList? results = null;
            if (item is not null) {
                TryCreate ??= ModEntry.Instance?.Helper.Reflection.GetMethod(__instance, "TryCreate", false);
                if (TryCreate is not null && ItemMap.TryGetValue(item.Category, out var names)) {
                    foreach (var name in names) {
                        var newItem = TryCreate.Invoke<object>(new object[] {
                            itemType.Identifier,
                            $"{name}/{item.ItemId}",
                            (object p) => CreateFlavoredType(item, name, Color.Gold)
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

        private static SObject? CreateFlavoredType(SObject ingredient, string type, Color defaultColor) {
            var color = TailoringMenu.GetDyeColor(ingredient) ?? defaultColor;
            var item = new ColoredObject(type, 999, color);
            try {
                item.displayNameFormat = $"[LocalizedText Strings\\Objects:{ModEntry.Instance?.ModHarmony?.Id}CP.Flavored{item.Name} %PRESERVED_DISPLAY_NAME %DISPLAY_NAME]";
                item.preservedParentSheetIndex.Value = ingredient.ItemId;
                var price = CalcPrice(item.Name, ingredient.Name);
                if (price > 0) {
                    item.Price = price;
                }
                item.Name = $"{ingredient.Name} {item.Name}";
            } catch { }
            return item;
        }
    }
}


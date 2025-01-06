using HarmonyLib;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace CopperStill.ModPatches {
    internal static class ItemSpawner {
        public static void Register() {
            if (ModEntry.Instance?.Helper?.ModRegistry.IsLoaded("CJBok.ItemSpawner") ?? false) {
                ModEntry.Instance?.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.Framework.ItemData.ItemRepository:GetFlavoredObjectVariants"),
                    postfix: new HarmonyMethod(typeof(ItemSpawner), nameof(Postfix_ItemRepository_GetFlavoredObjectVariants))
                );
                ModEntry.Instance?.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.ModEntry:BuildMenu"),
                    prefix: new HarmonyMethod(typeof(ItemSpawner), nameof(Prefix_ModEntry_BuildMenu))
                );
            }
        }

        private static readonly Dictionary<string, double> TypeMults = new();

        private static void Prefix_ModEntry_BuildMenu() {
            lock (TypeMults) {
                var typeMults = new Dictionary<string, double>() {
                        { "JuniperBerry", 10 },
                        { "Juice", 2.25 * 0.55},
                        { "Wine", 3 },
                        { "Mash", 2.25 },
                        { "Vodka", 1.0 },
                        { "Gin", 1.5 },
                        { "Moonshine", 1.0 },
                        { "Whiskey", 1.0 },
                        { "RumWhite", 1.0 },
                        { "RumDark", 1.0 },
                        { "TequilaBlanco", 1.0 },
                        { "TequilaAnejo", 1.0 },
                        { "Brandy", 4.25 },
                        { "Sake", 1.0 },
                        { "Soju", 1.0 }
                    };
                foreach (var mach in new Dictionary<string, string[]> {
                        { $"(BC){ModEntry.Instance?.ModHarmony?.Id}CP_Still", new[] { "TequilaBlanco", "Moonshine", "RumWhite", "Soju", "Vodka", "Gin" } },
                        { "(BC)12", new[] { "Whiskey", "TequilaAnejo", "RumDark", "Sake" }}
                    }) {
                    var md = DataLoader.Machines(Game1.content).GetValueOrDefault(mach.Key);
                    if (md is not null) {
                        foreach (var type in mach.Value) {
                            var amount = md
                                .OutputRules?.FirstOrDefault(o => o.Id == $"{ModEntry.Instance?.ModHarmony?.Id}CP_{type}")?
                                .OutputItem?.FirstOrDefault()?
                                .PriceModifiers?.FirstOrDefault()?
                                .Amount ?? 1.0;
                            typeMults[type] = amount;
                        }
                    }
                }
                foreach (var item in typeMults) {
                    TypeMults[item.Key] = item.Value;
                }
            }
        }

        public static int GetPrice(string ingredient) =>
            Game1.objectData.FirstOrDefault(o => o.Value.Name.Equals(ingredient)).Value?.Price ?? 0;

        public static int CalcPrice(string inputName, string? ingredient = null) {
            //INFO: doesn't account for UseNormal[*]Recipe settings
            if (!TypeMults.TryGetValue(inputName, out var mult) || mult <= 0)
                return GetPrice($"{ModEntry.Instance?.ModHarmony?.Id}CP_{inputName}");
            return inputName switch {
                "JuniperBerry" => (int)(GetPrice("Blackberry") + mult),

                "Juice" => ingredient is null ? GetPrice("Juice") : (int)(GetPrice(ingredient) * mult),
                "Wine" => ingredient is null ? GetPrice("Wine") : (int)(GetPrice(ingredient) * mult),
                "Mash" => (int)((ingredient is null ? GetPrice("Juice") : CalcPrice("Juice", ingredient)) * mult),

                "TequilaBlanco" => (int)(CalcPrice("Wine", "Cactus Fruit") * mult),
                "TequilaAnejo" => (int)(CalcPrice("TequilaBlanco") * mult),

                "Moonshine" => (int)(CalcPrice("Mash", "Corn") * mult),
                "Whiskey" => (int)(CalcPrice("Moonshine") * mult),

                "Vodka" => (int)(CalcPrice("Mash", ingredient) * mult),
                "Gin" => (int)(CalcPrice("Vodka", ingredient) * mult),

                "Brandy" => (int)(CalcPrice("Wine", ingredient) * mult),

                "RumWhite" => (int)(CalcPrice("Mash", "Beet") * mult),
                "RumDark" => (int)(CalcPrice("RumWhite") * mult),

                "Sake" => (int)(GetPrice("Unmilled Rice") * mult),
                "Soju" => (int)(CalcPrice("Sake") * mult),

                _ => 0,
            };
        }

        private static readonly Dictionary<int, string[]> ItemMap = new() {
            { -79, new string[] { "Brandy" } },
            { -75, new string[] { "Mash", "Vodka", "Gin" } }
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
                            $"{ModEntry.Instance?.ModHarmony?.Id}CP_{name}/{item.ItemId}",
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
            var item = new ColoredObject($"{ModEntry.Instance?.ModHarmony?.Id}CP_{type}", 999, color);
            try {
                item.Name = $"{ModEntry.Instance?.ModHarmony?.Id}CP_{type}_{ingredient.Name}";
                item.displayNameFormat = $"[LocalizedText Strings/Objects:{ModEntry.Instance?.ModHarmony?.Id}CP.{type}Flavored %PRESERVED_DISPLAY_NAME %DISPLAY_NAME]";
                item.preservedParentSheetIndex.Value = ingredient.ItemId;
                var price = CalcPrice(type, ingredient.Name);
                if (price > 0) {
                    item.Price = price;
                }
            } catch { }
            return item;
        }
    }
}
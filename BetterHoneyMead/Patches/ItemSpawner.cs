using HarmonyLib;
using StardewValley.ItemTypeDefinitions;
using System.Collections;
using SObject = StardewValley.Object;

namespace BetterHoneyMead.Patches {
    internal static class ItemSpawner {
        public static void Register() {
            if (ModEntry.Instance?.Helper?.ModRegistry.IsLoaded("CJBok.ItemSpawner") ?? false) {
                ModEntry.Instance?.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.Framework.ItemData.ItemRepository:GetFlavoredObjectVariants"),
                    postfix: new HarmonyMethod(typeof(ItemSpawner), nameof(Postfix_ItemRepository_GetFlavoredObjectVariants))
                );
            }
        }

        private static IEnumerable<object> Postfix_ItemRepository_GetFlavoredObjectVariants(
            IEnumerable<object> values,
            object __instance,
            SObject? item, IItemDataDefinition itemType

        ) {
            IList? results = null;
            if (item is not null) {
                var tryCreate = ModEntry.Instance?.Helper.Reflection.GetMethod(__instance, "TryCreate", false);
                if (tryCreate is not null) {
                    object? newItem = null;
                    if (item.Category == -80) { //item makes honey
                        newItem = tryCreate.Invoke<object>(new object[] { //also makes mead
                            itemType.Identifier,
                            $"459/{item.ItemId}",
                            (object p) => CreateItems.CreateFlavoredMead(item)
                        });
                    } else if (item.QualifiedItemId.Equals("(O)340")) { //item is honey
                        newItem = tryCreate.Invoke<object>(new object[] { //include non-wild, non-flavored
                            itemType.Identifier,
                            $"340/{item.ItemId}",
                            (object p) => CreateItems.CreateFlavoredHoney(null)
                        });
                    }
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
            return results as IEnumerable<object> ?? values;
        }
    }
}
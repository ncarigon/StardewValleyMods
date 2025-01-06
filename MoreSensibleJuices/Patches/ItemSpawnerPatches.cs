using HarmonyLib;
using StardewValley.ItemTypeDefinitions;
using System.Collections;
using SObject = StardewValley.Object;

namespace MoreSensibleJuices.Patches {
    internal static class ItemSpawnerPatches {
        public static void Register() {
            if (ModEntry.Instance?.Helper?.ModRegistry.IsLoaded("CJBok.ItemSpawner") ?? false) {
                ModEntry.Instance?.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.Framework.ItemData.ItemRepository:GetFlavoredObjectVariants"),
                    postfix: new HarmonyMethod(typeof(ItemSpawnerPatches), nameof(Postfix_ItemRepository_GetFlavoredObjectVariants))
                );
            }
        }

        private static IEnumerable<object> Postfix_ItemRepository_GetFlavoredObjectVariants(
            IEnumerable<object> values,
            object __instance,
            ObjectDataDefinition objectDataDefinition, SObject? item, IItemDataDefinition itemType

        ) {
            IList? results = null;
            switch (item?.Category) {
                //// is vegetable; already handled by Item Spawner
                //case -75:

                // is fruit
                case -79:

                // is edible, non-mushroom forage
                case -81 when item.HasContextTag("category_greens") && !item.HasContextTag("edible_mushroom") && item.Edibility > 0:
                    var newItem = ModEntry.Instance?.Helper.Reflection.GetMethod(__instance, "TryCreate", false)?
                        .Invoke<object>(new object[] {
                        itemType.Identifier,
                        $"350/{item.ItemId}",
                        (object p) => objectDataDefinition.CreateFlavoredJuice(item)
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
                    break;
            }
            return results as IEnumerable<object> ?? values;
        }
    }
}
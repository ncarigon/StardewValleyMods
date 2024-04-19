using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace BetterHoneyMead.ModPatches {
    internal static class LookupAnything {
        public static void Register() {
            if (ModEntry.Instance?.Helper?.ModRegistry.IsLoaded("CJBok.ItemSpawner") ?? false) {
                ModEntry.ModHarmony?.Patch(
                    original: AccessTools.Method(typeof(StardewValley.ItemTypeDefinitions.ObjectDataDefinition), "CreateFlavoredHoney"),
                    postfix: new HarmonyMethod(typeof(LookupAnything), nameof(Postfix_ObjectDataDefinition_CreateFlavoredHoney))
                );
                ModEntry.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.ModEntry:BuildMenu"),
                    prefix: new HarmonyMethod(typeof(LookupAnything), nameof(Prefix_ModEntry_BuildMenu)),
                    postfix: new HarmonyMethod(typeof(LookupAnything), nameof(Postfix_ModEntry_BuildMenu))
                );
                ModEntry.ModHarmony?.Patch(
                    original: AccessTools.Method("CJBItemSpawner.Framework.ItemMenu:ResetItemView"),
                    postfix: new HarmonyMethod(typeof(LookupAnything), nameof(Postfix_ItemSpawner_ResetItemView))
                );
            }
        }

        private static bool IsLoading = false;
        private static readonly List<Item> CachedItems = new();

        private static void Prefix_ModEntry_BuildMenu() {
            IsLoading = true;
            CachedItems.Clear();
        }

        private static void Postfix_ModEntry_BuildMenu() {
            IsLoading = false;
        }

        private static void Postfix_ObjectDataDefinition_CreateFlavoredHoney(
            SObject ingredient
        ) {
            if (IsLoading) {
                var item = CreateFlavoredMead(ingredient);
                if (item is not null) {
                    CachedItems.Add(item);
                }
            }
        }

        private static void Postfix_ItemSpawner_ResetItemView(
            IList<Item> ___ItemsInView, TextBox ___SearchBox
        ) {
            var search = ___SearchBox?.Text?.Trim();
            foreach (var item in CachedItems) {
                if (string.IsNullOrWhiteSpace(search)
                    || (item.Name?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false)
                    || (item.DisplayName?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false)
                ) {
                    ___ItemsInView.Add(item);
                }
            } 
        }

        private static SObject? CreateFlavoredMead(SObject ingredient) {
            var color = TailoringMenu.GetDyeColor(ingredient);
            if (color is null) {
                return null;
            }
            var item = new ColoredObject($"459", 999, color.Value);
            try {
                item.displayNameFormat = "%PRESERVED_DISPLAY_NAME %DISPLAY_NAME";
                item.preservedParentSheetIndex.Value = ingredient.ItemId;
                var price = (GetPrice("340") + (ingredient is null ? 0 : ingredient.Price * 2)) * 2;
                if (price > 0) {
                    item.Price = price;
                }
                if (ingredient?.Name is not null) {
                    item.Name = $"{item.QualifiedItemId}_{ingredient.Name} Honey";
                }
            } catch { }
            return item;
        }

        private static int GetPrice(string ItemId) =>
            Game1.objectData.FirstOrDefault(o => o.Key.Equals(ItemId)).Value?.Price ?? 0;
    }
}

using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;
using StardewValley.ItemTypeDefinitions;

namespace MoreSensibleJuices.Patches {
    internal static class SDVPatches {
        public static void Register() {
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(Item), "_PopulateContextTags"),
                postfix: new HarmonyMethod(typeof(SDVPatches), nameof(Postfix_Item__PopulateContextTags))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(ObjectDataDefinition), "CreateFlavoredJuice"),
                postfix: new HarmonyMethod(typeof(SDVPatches), nameof(Postfix_ObjectDataDefinition_CreateFlavoredJuice))
            );
        }

        private static void Postfix_Item__PopulateContextTags(
            Item __instance, HashSet<string> tags
        ) {
            if (__instance?.QualifiedItemId?.Equals("(O)350") ?? false) { // is juice
                switch (new SObject((__instance as SObject)?.preservedParentSheetIndex?.Value ?? "-1", 1)?.Category) { // has preserved index
                    case -79: // from fruit
                        tags?.Add("juice_fruit_item");
                        break;
                    case -75: // from vegetable
                        tags?.Add("juice_vegetable_item");
                        break;
                    case -81: // from forage
                        tags?.Add("juice_forage_item");
                        break;
                }
            }
        }

        private static void Postfix_ObjectDataDefinition_CreateFlavoredJuice(
            ref SObject __result
        ) {
            if (__result is not null) {
                //INFO: lower multipliers to balance original price/edibility to newer speed
                //      handling in code instead of CP to keep compat with Item Spawner mod
                __result.Price = (int)(__result.Price * 0.55);
                __result.Edibility = (int)(__result.Edibility * 0.55);
            }
        }
    }
}
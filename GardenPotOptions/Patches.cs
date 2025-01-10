using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace GardenPotOptions {
    internal static class Patches {
        public static void Register() {
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(Pickaxe), "DoFunction"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Pickaxe_DoFunction))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(Axe), "DoFunction"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Axe_DoFunction))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(Hoe), "DoFunction"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Hoe_DoFunction))
            );
            //ModEntry.Instance?.ModHarmony?.Patch(
            //    original: AccessTools.Method(typeof(SObject), "placementAction"),
            //    postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_placementAction))
            //);
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(Utility), "tryToPlaceItem"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Utility_tryToPlaceItem))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(Bush), "performUseAction"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Bush_performUseAction))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(Tree), "performUseAction"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Tree_performUseAction))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(FruitTree), "performUseAction"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_FruitTree_performUseAction))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(SObject), "loadDisplayName"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_loadDisplayName))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(SObject), "maximumStackSize"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_maximumStackSize))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(SObject), "ApplySprinkler"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_ApplySprinkler))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(PlantableRule), "ShouldApplyWhen"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_PlantableRule_ShouldApplyWhen))
            );
        }

        private static void Prefix_Pickaxe_DoFunction(GameLocation location, int x, int y, Farmer who) {
            if (ModEntry.Instance?.ModConfig?.KeepContents == true && "Pickaxe" == ModEntry.Instance?.ModConfig?.SafeTool) {
                location.TryUsePot(x, y, null, who);
            }
        }

        private static void Prefix_Axe_DoFunction(GameLocation location, int x, int y, Farmer who) {
            if (ModEntry.Instance?.ModConfig?.KeepContents == true && "Axe" == ModEntry.Instance?.ModConfig?.SafeTool) {
                location.TryUsePot(x, y, null, who);
            }
        }

        private static void Prefix_Hoe_DoFunction(GameLocation location, int x, int y, Farmer who) {
            if (ModEntry.Instance?.ModConfig?.KeepContents == true && "Hoe" == ModEntry.Instance?.ModConfig?.SafeTool) {
                location.TryUsePot(x, y, null, who);
            }
        }

        //private static void Postfix_Object_placementAction(
        //    SObject __instance, ref bool __result,
        //    GameLocation location, int x, int y
        //) {
        //    var tile = new Vector2(x / 64, y / 64);
        //    if (__result &&
        //        (ModEntry.Instance?.ModConfig?.KeepContents ?? false)
        //        && __instance.IsGardenPot() && __instance.HoldsItem()
        //        && (location?.Objects?.TryGetValue(tile, out var p) ?? false)
        //        && p is IndoorPot newPot && newPot is not null
        //        && TryRestore(__instance, newPot)
        //    ) {
        //        __instance.TrySetItem(null); // clear modData
        //    }
        //}

        private static void Postfix_Bush_performUseAction(Bush __instance, ref bool __result) {
            __result = __instance.TryToPot();
        }

        private static void Postfix_Tree_performUseAction(Tree __instance, ref bool __result) {
            __result = __instance.TryToPot();
        }

        private static void Postfix_FruitTree_performUseAction(FruitTree __instance, ref bool __result) {
            __result = __instance.TryToPot();
        }

        private static void Prefix_Utility_tryToPlaceItem(
            GameLocation location, ref Item? item, int x, int y
        ) {
            if (item.IsGardenPot(out var contents)
                && (location.TryUsePot(x, y, item, Game1.player) // perform any allowed action
                    || !string.IsNullOrWhiteSpace(contents)) // skip default action if pot has saved content
            ) {
                // early escape from original call
                item = null;
            }
        }

        private static void Postfix_Object_loadDisplayName(SObject __instance, ref string __result) {
            try {
                if (ModEntry.Instance?.ModConfig?.KeepContents == true
                    && __instance.IsGardenPot(out var suffix)
                    && !string.IsNullOrWhiteSpace(suffix)
                ) {
                    __result += $" ({suffix})";
                }
            } catch { }
        }

        private static void Postfix_Object_maximumStackSize(SObject __instance, ref int __result) {
            try {
                if (ModEntry.Instance?.ModConfig?.KeepContents == true
                    && __instance.IsGardenPot(out var content)
                    && !string.IsNullOrWhiteSpace(content)
                ) {
                    __result = 1;
                }
            } catch { }
        }

        private static void Postfix_Object_ApplySprinkler(SObject __instance, Vector2 tile) {
            try {
                if (ModEntry.Instance?.ModConfig?.EnableSprinklers == true
                    && __instance?.Location?.Objects?.TryGetValue(tile, out var obj) == true && obj is IndoorPot pot
                    && (pot?.hoeDirt?.Value?.state?.Value ?? 2) != 2
                ) {
                    pot?.Water();
                }
            } catch { }
        }

        private static void Prefix_PlantableRule_ShouldApplyWhen(PlantableRule __instance, ref bool isGardenPot) {
            try {
                if (ModEntry.Instance?.ModConfig?.AllowAncientSeeds == true
                    && (__instance?.Id ?? "") == "NoGardenPots" && isGardenPot
                    && (__instance!.DeniedMessage ?? "").Contains("AncientFruit")
                ) {
                    isGardenPot = false;
                }
            } catch { }
        }
    }
}

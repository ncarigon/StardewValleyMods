using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace GardenPotOptions {
    internal static class Patches {
        private static Config? Config = null!;

        public static void Register(Harmony harmony, Config config) {
            Config = config;
            harmony.Patch(
                original: AccessTools.Method(typeof(Pickaxe), "DoFunction"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Pickaxe_DoFunction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "placementAction"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_placementAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "loadDisplayName"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_loadDisplayName))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "maximumStackSize"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_maximumStackSize))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "ApplySprinkler"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_ApplySprinkler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(PlantableRule), "ShouldApplyWhen"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_PlantableRule_ShouldApplyWhen))
            );
        }

        private static bool IsValidPot(SObject obj, out IndoorPot? pot) =>
            (pot = obj as IndoorPot) is not null
            && (pot.hoeDirt.Value?.crop is not null
            || pot.bush.Value is not null
            || pot.hoeDirt.Value?.fertilizer.Value is not null
            || pot.heldObject.Value is not null);

        private static void Prefix_Pickaxe_DoFunction(GameLocation location, int x, int y, Farmer who) {
            if (Config?.KeepContents ?? false) {
                var tile = new Vector2(x / 64, y / 64);
                if (location.Objects.TryGetValue(tile, out var obj)
                     && IsValidPot(obj, out var pot)
                ) {
                    location.debris.Add(new Debris(pot, who.GetToolLocation(), who.GetBoundingBox().Center.ToVector2()));
                    pot!.performRemoveAction();
                    location.Objects.Remove(tile);
                }
            }
        }

        private static void Postfix_Object_placementAction(SObject __instance, GameLocation location, int x, int y) {
            if ((Config?.KeepContents ?? false) && IsValidPot(__instance, out var pot)
                && location.Objects.TryGetValue(new Vector2(x / 64, y / 64), out var p) && p is IndoorPot newPot
            ) {
                newPot.bush.Value = pot!.bush.Value;
                newPot.hoeDirt.Value = pot.hoeDirt.Value;
                newPot.heldObject.Value = pot.heldObject.Value;
            }
        }

        private static void Postfix_Object_loadDisplayName(SObject __instance, ref string __result) {
            if ((Config?.KeepContents ?? false) && IsValidPot(__instance, out var pot)) {
                if (pot!.hoeDirt.Value?.crop is not null) {
                    __result += $" ({new SObject(pot.hoeDirt.Value.crop.indexOfHarvest.Value, 1).DisplayName})";
                } else if (pot.bush?.Value is not null) {
                    __result += $" ({(pot.bush.Value.size.Value == 3 ? new SObject("251", 1).DisplayName : "Bush")})";
                } else if (pot.hoeDirt.Value?.fertilizer.Value is not null) {
                    __result += $" ({new SObject(pot.hoeDirt.Value.fertilizer.Value.Replace("(O)", ""), 1).DisplayName})";
                } else if (pot.heldObject.Value is not null) {
                    __result += " (" + pot.heldObject.Value.DisplayName + ")";
                }
            }
        }

        private static void Postfix_Object_maximumStackSize(SObject __instance, ref int __result) {
            if ((Config?.KeepContents ?? false) && IsValidPot(__instance, out _)) {
                __result = 1;
            }
        }

        private static void Postfix_Object_ApplySprinkler(SObject __instance, Vector2 tile) {
            if ((Config?.EnableSprinklers ?? false)
                && __instance.Location.Objects.TryGetValue(tile, out var obj) && obj is IndoorPot pot
                && pot.hoeDirt.Value.state.Value != 2
            ) {
                pot.hoeDirt.Value.state.Value = 1;
                pot.showNextIndex.Value = true;
            }
        }

        private static void Prefix_PlantableRule_ShouldApplyWhen(PlantableRule __instance, ref bool isGardenPot) {
            if ((Config?.AllowAncientSeeds ?? false)
                && __instance.Id == "NoGardenPots" && isGardenPot
                && __instance.DeniedMessage.Contains("AncientFruit")
            ) {
                isGardenPot = false;
            }
        }
    }
}

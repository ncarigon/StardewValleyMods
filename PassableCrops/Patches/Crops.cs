using System;
using HarmonyLib;
using StardewValley.TerrainFeatures;
using StardewValley;

namespace PassableCrops.Patches {
    internal static class Crops {
        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;
            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.isPassable), new Type[] { typeof(Character) }),
                postfix: new HarmonyMethod(typeof(Crops), nameof(Postfix_HoeDirt_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.doCollisionAction)),
                prefix: new HarmonyMethod(typeof(Crops), nameof(Prefix_HoeDirt_doCollisionAction)),
                postfix: new HarmonyMethod(typeof(Crops), nameof(Postfix_HoeDirt_doCollisionAction))
            );
        }

        private static void Postfix_HoeDirt_isPassable(
            HoeDirt __instance, ref bool __result,
            Character c
        ) {
            if (Mod?.Config?.PassableCrops == true) {
                var farmer = c as Farmer;
                if (farmer is not null || Mod?.Config?.PassableByAll == true) {
                    __result |= __instance?.crop is not null;
                }
            }
        }

        private static void Prefix_HoeDirt_doCollisionAction(
            HoeDirt __instance
        ) {
            if (Mod?.Config?.PassableCrops == true) {
                // only handle raised seed crops, which should still shake when newly planted
                if (__instance?.crop?.raisedSeeds?.Value == true) {
                    if (__instance?.crop?.currentPhase?.Value == 0) {
                        // if newly planted, set to invalid phase to allow shake
                        __instance.crop.currentPhase.Value = -1;
                    }
                }
            }
        }

        private static void Postfix_HoeDirt_doCollisionAction(
            HoeDirt __instance,
            ref float ___maxShake,
            Character who
        ) {
            if (__instance?.crop?.currentPhase?.Value == -1) {
                // revert invalid phase to original value
                __instance.crop.currentPhase.Value = 0;
                // dampen the shake a bit
                ___maxShake *= 0.75f;
                TerrainFeatures.TerrainFeature_doCollisionAction(__instance, who);
            }
        }
    }
}

using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using System;

namespace PassableCrops.Patches {
    internal static class FruitTrees {
        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;
            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(FruitTree), nameof(FruitTree.isPassable), new Type[] { typeof(Character) }),
                postfix: new HarmonyMethod(typeof(FruitTrees), nameof(Postfix_FruitTree_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(FruitTree), nameof(FruitTree.getBoundingBox)),
                postfix: new HarmonyMethod(typeof(FruitTrees), nameof(Postfix_FruitTree_getBoundingBox))
            );
        }

        public static bool AnyPassable(FruitTree tree) {
            return Mod?.Config?.PassableFruitTreeGrowth >= (tree?.growthStage?.Value >= 5 ? 5 : tree?.growthStage?.Value);
        }

        private static void Postfix_FruitTree_isPassable(
            FruitTree __instance,
            ref bool __result, ref float ___maxShake, ref NetBool ___shakeLeft,
            Character c
        ) {
            if (AnyPassable(__instance)) {
                var farmer = c as Farmer;
                if (farmer is not null || Mod?.Config?.PassableByAll == true) {
                    __result = true;
                    if (Mod?.Config?.ShakeWhenPassing == true && c is not null && ___maxShake == 0f) {
                        ___shakeLeft.Value = c.StandingPixel.X > (__instance.Tile.X + 0.5f) * 64f || (c.Tile.X == __instance.Tile.X && Game1.random.NextBool());
                        ___maxShake = (float)(Math.PI / 64.0);
                    }
                }
            }
        }

        private static void Postfix_FruitTree_getBoundingBox(
            FruitTree __instance,
            ref Rectangle __result
        ) {
            if (AnyPassable(__instance)) {
                var skew = __instance.growthStage.Value switch {
                    0 => -46,
                    1 => -46,
                    2 => -34,
                    3 => -30,
                    4 => -30,
                    _ => -30
                };
                __result = new Rectangle(__result.X, __result.Y + skew, __result.Width, __result.Height);
            }
        }
    }
}


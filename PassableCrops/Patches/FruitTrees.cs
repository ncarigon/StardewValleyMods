﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
                original: AccessTools.Method(typeof(FruitTree), nameof(FruitTree.draw), new Type[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(FruitTrees), nameof(Prefix_FruitTree_draw))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(FruitTree), nameof(FruitTree.getBoundingBox)),
                postfix: new HarmonyMethod(typeof(FruitTrees), nameof(Postfix_FruitTree_getBoundingBox))
            );
        }

        private static bool AnyPassable(FruitTree tree) {
            return Mod?.Config is not null && Mod.Config.PassableFruitTreeGrowth >= (tree?.growthStage?.Value >= 5 ? 5 : tree?.growthStage?.Value);
        }

        private static void Postfix_FruitTree_isPassable(
            FruitTree __instance,
            ref bool __result, ref float ___maxShake, ref NetBool ___shakeLeft,
            Character c
        ) {
            try {
                if (AnyPassable(__instance)) {
                    var farmer = c as Farmer;
                    if (farmer is not null || Mod?.Config?.PassableByAll == true) {
                        __result = true;
                        if (farmer is not null && Mod?.Config?.SlowDownWhenPassing == true) {
                            farmer.temporarySpeedBuff = farmer.stats.Get("Book_Grass") == 0 ? -1f : -0.33f;
                        }
                        if (Mod?.Config?.ShakeWhenPassing == true && c is not null && ___maxShake == 0f) {
                            ___shakeLeft.Value = c.StandingPixel.X > (__instance.Tile.X + 0.5f) * 64f || (c.Tile.X == __instance.Tile.X && Game1.random.NextBool());
                            ___maxShake = (float)(Math.PI / 64.0);
                            if (c is not FarmAnimal && Utility.isOnScreen(new Point((int)__instance.Tile.X, (int)__instance.Tile.Y), 2, __instance.Location)) {
                                Mod?.PlayRustleSound(__instance.Tile, __instance.Location);
                            }
                        }
                    }
                }
            } catch { }
        }

        private static bool isDrawing = false;

        private static void Prefix_FruitTree_draw(
            FruitTree __instance
        ) {
            if (AnyPassable(__instance)) {
                isDrawing = true;
            }
        }

        private static void Postfix_FruitTree_getBoundingBox(
            FruitTree __instance,
            ref Rectangle __result
        ) {
            if (isDrawing) {
                isDrawing = false;
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


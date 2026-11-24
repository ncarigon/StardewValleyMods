using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;

namespace PassableCrops.Patches {
    internal static class Trees {
        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;
            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), nameof(Tree.isPassable), new Type[] { typeof(Character) }),
                postfix: new HarmonyMethod(typeof(Trees), nameof(Postfix_Tree_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), nameof(Tree.getBoundingBox)),
                postfix: new HarmonyMethod(typeof(Trees), nameof(Postfix_Tree_getBoundingBox))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), nameof(Tree.draw), new Type[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(Trees), nameof(Prefix_Tree_draw)),
                postfix: new HarmonyMethod(typeof(Trees), nameof(Postfix_Tree_draw))
            );
        }

        private static bool IsDrawing = false;
        private static void Prefix_Tree_draw() => IsDrawing = true;
        private static void Postfix_Tree_draw() => IsDrawing = false;

        private static int GetTreeGrowth(Tree tree) {
            return (tree?.treeType?.Value) switch {
                null => 0,
                "6" => tree.growthStage.Value >= 3 ? 5 : tree.growthStage.Value, // palms mature at stage 3
                _ => tree.growthStage.Value >= 5 ? 5 : tree.growthStage.Value, // others mature at stage 5
            };
        }

        public static bool AnyPassable(Tree tree) {
            return Mod?.Config?.PassableTreeGrowth >= GetTreeGrowth(tree);
        }

        private static void Postfix_Tree_isPassable(
            Tree __instance,
            ref bool __result, ref float ___maxShake, ref NetBool ___shakeLeft,
            Character c
        ) {
            if (AnyPassable(__instance)) {
                var farmer = c as Farmer;
                if (farmer is not null || Mod?.Config?.PassableByAll == true) {
                    __result = true;
                    if (Mod?.Config?.ShakeWhenPassing == true && c is not null && ___maxShake == 0f && __instance.growthStage.Value > 0) {
                        ___shakeLeft.Value = c.StandingPixel.X > (__instance.Tile.X + 0.5f) * 64f || (c.Tile.X == __instance.Tile.X && Game1.random.NextBool());
                        ___maxShake = (float)(Math.PI / 64.0);
                    }
                }
            }
        }

        private static void Postfix_Tree_getBoundingBox(
            Tree __instance,
            ref Rectangle __result
        ) {
            if (IsDrawing && AnyPassable(__instance)) {
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


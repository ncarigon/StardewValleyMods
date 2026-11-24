using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;

namespace PassableCrops.Patches {
    internal static class Bushes {
        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;
            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), nameof(Bush.isPassable)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), nameof(Bush.getBoundingBox)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_getBoundingBox))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), nameof(Bush.draw), new Type[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(Bushes), nameof(Prefix_Bush_draw)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_draw))
            );
        }

        private static bool IsDrawing = false;
        private static void Prefix_Bush_draw() => IsDrawing = true;
        private static void Postfix_Bush_draw() => IsDrawing = false;

        public static bool AnyPassable(Bush bush) {
            return Mod?.Config?.PassableTeaBushes == true && bush?.size?.Value == 3 && bush?.inPot?.Value != true;
        }

        private static void Postfix_Bush_isPassable(
            Bush __instance,
            ref bool __result, ref float ___maxShake, ref bool ___shakeLeft,
            Character c
        ) {
            if (AnyPassable(__instance)) {
                var farmer = c as Farmer;
                if (farmer is not null || Mod?.Config?.PassableByAll == true) {
                    __result = true;
                    // need to manually set shake info or it won't happen
                    if (Mod?.Config?.ShakeWhenPassing == true && c is not null && ___maxShake == 0f) {
                        ___shakeLeft = c.Tile.X > __instance!.Tile.X || (c.Tile.X == __instance.Tile.X && Game1.random.NextBool());
                        // using a wider, longer shake to seem less rigid
                        // compared to bush.shake()
                        ___maxShake = (float)Math.PI / 40f;
                        __instance.shakeTimer = 1000f;
                        __instance.NeedsUpdate = true;
                    }
                }
            }
        }

        private static void Postfix_Bush_getBoundingBox(
            Bush __instance,
            ref Rectangle __result
        ) {
            if (IsDrawing && AnyPassable(__instance)) {
                var skew = -46;
                __result = new Rectangle(__result.X, __result.Y + skew, __result.Width, __result.Height);
            }
        }
    }
}


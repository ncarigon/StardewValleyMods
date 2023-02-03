using System;
using Microsoft.Xna.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.TerrainFeatures;
using StardewValley;
using SpaceShared.APIs;
using SObject = StardewValley.Object;

namespace CopperStill.ModPatches {
    internal static class JuniperBerry {
        private static int ItemId;

        public static void Register(IModHelper helper) {
            helper.Events.GameLoop.SaveLoaded += (s, e) => {
                ItemId = helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets")?.GetObjectId("Juniper Berry") ?? -1;
            };

            var harmony = new Harmony(helper.ModContent.ModID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "inBloom"),
                postfix: new HarmonyMethod(typeof(JuniperBerry), nameof(InBloom))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "shake"),
                prefix: new HarmonyMethod(typeof(JuniperBerry), nameof(Shake))
            );
        }

        private static void InBloom(Bush __instance, ref bool __result, string season, int dayOfMonth) {
            // make bushes bloom in late fall
            try {
                if (!__result && // not already in bloom
                    __instance.size.Value != 3 // not a tea bush
                    && __instance.size.Value != 4) // not a golden walnut bush
                {
                    if (__instance.overrideSeason.Value != -1) {
                        season = Utility.getSeasonNameFromNumber(__instance.overrideSeason.Value);
                    }
                    if (season.Equals("fall") && dayOfMonth > 20 && dayOfMonth < 25) // is juniper berry season
                        __result = true;
                }
            } catch { }
        }

        private static bool Shake(Bush __instance, ref float ___maxShake, ref bool ___shakeLeft, Vector2 tileLocation, bool doEvenIfStillShaking) {
            try {
                var season = Game1.GetSeasonForLocation(__instance.currentLocation);
                var inBloom = false;
                InBloom(__instance, ref inBloom, season, Game1.dayOfMonth);
                if (!inBloom)
                    return true; // not juniper berry season, run normal logic

                if (!(___maxShake == 0f || doEvenIfStillShaking))
                    return false; // normal logic checks this, so I will too

                ___shakeLeft = Game1.player.getTileLocation().X > tileLocation.X || Game1.player.getTileLocation().X == tileLocation.X && Game1.random.NextDouble() < 0.5;
                ___maxShake = (float)Math.PI / 128f;
                if (!__instance.townBush.Value && __instance.tileSheetOffset.Value == 1) {
                    __instance.tileSheetOffset.Value = 0;
                    __instance.setUpSourceRect();
                    Random r = new((int)tileLocation.X + (int)tileLocation.Y * 5000 + (int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed);
                    int count = r.Next(1, 2) + Game1.player.ForagingLevel / 4;
                    for (int j = 0; j < count; j++) {
                        Game1.createItemDebris(new SObject(ItemId, 1, false, -1, Game1.player.professions.Contains(16) ? 4 : 0), Utility.PointToVector2(__instance.getBoundingBox().Center), Game1.random.Next(1, 4));
                    }
                    DelayedAction.playSoundAfterDelay("leafrustle", 100);
                }
                return false; // juniper berry handled, if applicable
            } catch { }
            return true; // default to normal logic
        }
    }
}

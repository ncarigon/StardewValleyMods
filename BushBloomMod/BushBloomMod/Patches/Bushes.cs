using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BushBloomMod.Patches {
    internal static class Bushes {
        public static void Register() {
            var harmony = new Harmony(ModEntry.Instance.Helper.ModContent.ModID);
            if (ModEntry.Instance.Helper.ModRegistry.IsLoaded("CJBok.CheatsMenu")) {
                harmony.Patch(
                    original: AccessTools.Method("CJBCheatsMenu.Framework.Cheats.Time.BaseDateCheat:SafelySetDate"),
                    postfix: new HarmonyMethod(typeof(Bushes), nameof(UpdateAllBushes))
                );
            }
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "DayUpdate"),
                prefix: new HarmonyMethod(typeof(Bushes), nameof(Prefix_GameLocation_DayUpdate)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_GameLocation_DayUpdate))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "dayUpdate"),
                prefix: new HarmonyMethod(typeof(Bushes), nameof(Prefix_Bush_dayUpdate)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_dayUpdate))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "seasonUpdate"),
                prefix: new HarmonyMethod(typeof(Bushes), nameof(Prefix_Bush_seasonUpdate)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_seasonUpdate))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "inBloom"),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_inBloom))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "GetShakeOffItem"),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_GetShakeOffItem))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "shake"),
                prefix: new HarmonyMethod(typeof(Bushes), nameof(Prefix_Bush_shake)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_shake))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "setUpSourceRect"),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_setUpSourceRect))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "draw", new Type[] { typeof(SpriteBatch) }),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_draw))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), "get_ForagingLevel"),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Farmer_ForagingLevel))
            );
        }

        public static void UpdateAllBushes() {
            if (Game1.IsMasterGame) {
                Game1.locations.SelectMany(l => l.largeTerrainFeatures)
                    .Select(t => t as Bush).ToList()
                    .ForEach(b => b?.dayUpdate());
            }
        }

        private static bool IsBaseGameUpdate;

        private static void Prefix_GameLocation_DayUpdate() => IsBaseGameUpdate = true;

        private static void Postfix_GameLocation_DayUpdate() => IsBaseGameUpdate = false;

        private static void Prefix_Bush_dayUpdate(
            Bush __instance,
            ref Tuple<int, Rectangle> __state
        ) {
            if (Game1.IsMasterGame && __instance.IsAbleToBloom()) {
                if (IsBaseGameUpdate) {
                    // save offest and rect to prevent overwrite during base game logic
                    __state = new(__instance.tileSheetOffset.Value, __instance.sourceRect.Value);
                } else {
                    // this sets up the schedule for the bush for the current day
                    __instance.IsBloomingToday();
                }
            }
        }

        private static void Postfix_Bush_dayUpdate(
            Bush __instance,
            ref Tuple<int, Rectangle> __state
        ) {
            if (Game1.IsMasterGame && __instance.IsAbleToBloom()) {
                if (IsBaseGameUpdate) {
                    // restore offest to prevent overwrite during base game logic
                    __instance.tileSheetOffset.Value = __state.Item1;
                    __instance.sourceRect.Value = __state.Item2;
                } else {
                    // use our blooming logic to setup bush texture
                    __instance.tileSheetOffset.Value = __instance.HasBloomedToday() ? 1 : 0;
                    __instance.setUpSourceRect();
                }
            }
        }

        private static void Prefix_Bush_seasonUpdate(
            Bush __instance,
            ref int __state
        ) {
            if (Game1.IsMasterGame && __instance.IsAbleToBloom()) {
                // save offset to prevent overwrite during season change
                __state = __instance.tileSheetOffset.Value;
            }
        }

        private static void Postfix_Bush_seasonUpdate(
            Bush __instance,
            int __state
        ) {
            if (Game1.IsMasterGame && __instance.IsAbleToBloom()) {
                // restore offset to allow custom blooming logic to work
                __instance.tileSheetOffset.Value = __state;
            }
        }

        private static void Postfix_Bush_inBloom(
            Bush __instance,
            ref bool __result
        ) {
            if (!IsBaseGameUpdate && __instance.IsAbleToBloom()) {
                // use our own blooming logic
                __result = __instance.HasBloomedToday();
            }
        }

        private static void Postfix_Bush_GetShakeOffItem(
            Bush __instance,
            ref string __result
        ) {
            if (__instance.IsAbleToBloom()) {
                // overwrite with our item shake logic
                __result = __instance.GetShakeOffId();
            }
        }

        private static int LastShakeSize = 1;

        private static void Prefix_Bush_shake(
            Bush __instance
        ) => LastShakeSize = __instance?.size?.Value ?? 1;

        private static void Postfix_Bush_shake(
            Bush __instance
        ) {
            LastShakeSize = 1;
            if (__instance.IsAbleToBloom()) {
                // clear schedule after shaking
                __instance.DataSetSchedule(null);
                __instance.tileSheetOffset.Value = 0;
                __instance.setUpSourceRect();
            }
        }

        private static void Postfix_Farmer_ForagingLevel(
            ref int __result
        ) {
            if (LastShakeSize == 0) {
                __result /= 2;
            } else if (LastShakeSize == 2) {
                __result *= 2;
            }
        }

        private static void Postfix_Bush_setUpSourceRect(
            Bush __instance
        ) {
            if (!IsBaseGameUpdate && __instance.IsAbleToBloom()) {
                var season = __instance.IsSheltered() ? Season.Spring : __instance.Location.GetSeason();
                // switch summer texture to spring, if configured
                if (season == Season.Summer && ModEntry.Instance.Config.UseSpringBushForSummer) {
                    season = Season.Spring;
                }
                var sheetOffset = __instance.tileSheetOffset.Value;
                if (__instance.tileSheetOffset.Value == 1) {
                    var size = __instance.size.Value;
                    // if blooming, cache any custom blooming texture
                    var t = (Schedule.TryGetExistingSchedule(__instance, out var schedule) ? (
                        size == 1 ? schedule.Texture
                        : size == 0 ? schedule.TextureSmall
                        : schedule.TextureLarge)
                        : null)
                        // use our default winter berry if no other is specified
                        ?? (season == Season.Winter && ModEntry.Instance.Config.UseCustomWinterBerrySprite ? (
                            size == 1 ? Schedule.WinterBerry
                            : size == 0 ? Schedule.WinterBerrySmall
                            : Schedule.WinterBerryLarge)
                        : null)
                        // use our default small/large spring/summer berry
                        ?? (season == Season.Spring || season == Season.Summer ? (
                            size == 1 ? null
                            : size == 0 ? Schedule.SpringBerrySmall
                            : Schedule.SpringBerryLarge)
                        : null)
                        // use our default small/large fall berry
                        ?? (season == Season.Fall ? (
                            size == 1 ? null
                            : size == 0 ? Schedule.FallBerrySmall
                            : Schedule.FallBerryLarge)
                        : null);
                    // switch to non-blooming texture if using a custom texture or small/large bushes
                    var hasTexture = false;
                    if (t is not null || size == 0 || size == 2) {
                        hasTexture = true;
                        sheetOffset = 0;
                    }
                    __instance.DataSetTexture(hasTexture);
                }
                var xOffset = ((int)season * 64) + (sheetOffset * 32);
                if (__instance.size.Value == 0) {
                    var sr = __instance.sourceRect.Value;
                    xOffset = __instance.tileSheetOffset.Value == 1 ? 16 : 0;
                    __instance.sourceRect.Value = new Rectangle(sr.X - xOffset, sr.Y, sr.Width, sr.Height);
                } else if (__instance.size.Value == 1) {
                    __instance.sourceRect.Value = new Rectangle(xOffset % Bush.texture.Value.Bounds.Width, xOffset / Bush.texture.Value.Bounds.Width * 3 * 16, 32, 48);
                }
            }
        }

        private static void Postfix_Bush_draw(
            Bush __instance,
            SpriteBatch spriteBatch,
            float ___yDrawOffset, float ___shakeRotation
        ) {
            if (__instance.IsAbleToBloom()) {
                var size = __instance.size.Value;
                // if blooming and using a custom texture
                if (__instance.tileSheetOffset.Value == 1
                    && (__instance.DataHasTexture() || size != 1)
                    && Schedule.TryGetExistingSchedule(__instance, out var schedule)
                ) {
                    var season = __instance.Location.GetSeason();
                    if (size == 1) {
                        CustomBerryDraw(spriteBatch, schedule.Texture ?? Schedule.WinterBerry, __instance.Tile.X, __instance.Tile.Y, 32, 48, __instance.getBoundingBox().Center.Y, ___yDrawOffset, ___shakeRotation, __instance.flipped.Value);
                    } else if (size == 0) {
                        CustomBerryDraw(spriteBatch, schedule.TextureSmall ?? (season == Season.Spring || season == Season.Summer ? Schedule.SpringBerrySmall : season == Season.Fall ? Schedule.FallBerrySmall : Schedule.WinterBerrySmall), __instance.Tile.X, __instance.Tile.Y, 16, 32, __instance.getBoundingBox().Center.Y, ___yDrawOffset + 64, ___shakeRotation, __instance.flipped.Value);
                    } else if (size == 2) {
                        CustomBerryDraw(spriteBatch, schedule.TextureLarge ?? (season == Season.Spring || season == Season.Summer ? Schedule.SpringBerryLarge : season == Season.Fall ? Schedule.FallBerryLarge : Schedule.WinterBerryLarge), __instance.Tile.X, __instance.Tile.Y, 48, 48, __instance.getBoundingBox().Center.Y, ___yDrawOffset, ___shakeRotation, __instance.flipped.Value);
                    }
                }
            }
        }

        private static void CustomBerryDraw(SpriteBatch spriteBatch, Texture2D texture, float x, float y, int width, int height, int centerY, float yOffset, float rotation, bool flipped) {
            spriteBatch.Draw(
                texture: texture,
                position: Game1.GlobalToLocal(Game1.viewport, new Vector2((x * 64f) + 64, ((y + 1f) * 64f) - 64 + yOffset)),
                sourceRectangle: new(0, 0, width, height),
                color: Color.White,
                rotation: rotation,
                origin: new Vector2(16, 32f),
                scale: 4f,
                effects: flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                // make sure to add small extra here so the sprite doesn't flicker
                layerDepth: ((centerY + 48) / 10000f) - (x / 1000000f) + 0.0001f
            );
        }
    }
}

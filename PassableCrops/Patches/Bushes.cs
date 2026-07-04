using System;
using HarmonyLib;
using StardewValley.TerrainFeatures;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;

namespace PassableCrops.Patches {
    internal static class Bushes {
        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;

            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), "isPassable", new Type[] { typeof(Character) }),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), nameof(Bush.draw), new Type[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(Bushes), nameof(Prefix_Bush_draw))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Bush), nameof(Bush.getBoundingBox)),
                postfix: new HarmonyMethod(typeof(Bushes), nameof(Postfix_Bush_getBoundingBox))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "isCollidingPosition", new Type[] { typeof(Rectangle), typeof(xTile.Dimensions.Rectangle), typeof(bool), typeof(int), typeof(bool), typeof(Character), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(Bushes), nameof(Prefix_GameLocation_isCollidingPosition)),
                finalizer: new HarmonyMethod(typeof(Bushes), nameof(Finalizer_GameLocation_isCollidingPosition))
            );
        }

        private static bool TeaPassable(Bush bush) {
            return Mod?.Config is not null && Mod.Config.PassableTeaBushes && bush?.size.Value == Bush.greenTeaBush && bush?.inPot.Value != true;
        }

        private static bool SmallPassable(Bush bush) {
            return Mod?.Config is not null && Mod.Config.PassableBushes && bush?.size.Value == Bush.smallBush && bush?.inPot.Value != true;
        }

        private static bool AnyPassable(Bush bush) {
            return TeaPassable(bush) || SmallPassable(bush);
        }

        private static void ApplyPassingEffects(Bush bush, Character? c, ref float maxShake, ref bool shakeLeft) {
            if (c is Farmer farmer && Mod?.Config?.SlowDownWhenPassing == true) {
                farmer.temporarySpeedBuff = farmer.stats.Get("Book_Grass") == 0 ? -1f : -0.33f;
            }
            if (Mod?.Config?.ShakeWhenPassing == true && c is not null && maxShake == 0f) {
                shakeLeft = c.Tile.X > bush.Tile.X || (c.Tile.X == bush.Tile.X && Game1.random.NextBool());
                maxShake = (float)Math.PI / 40f;
                bush.shakeTimer = 1000f;
                bush.NeedsUpdate = true;
                if (c is not FarmAnimal && Utility.isOnScreen(new Point((int)bush.Tile.X, (int)bush.Tile.Y), 2, bush.Location)) {
                    Mod?.PlayRustleSound(bush.Tile, bush.Location);
                }
            }
        }

        private static void Postfix_Bush_isPassable(
            Bush __instance,
            ref bool __result, ref float ___maxShake, ref bool ___shakeLeft,
            Character c) {
            try {
                if (AnyPassable(__instance)) {
                    if (c is Farmer || Mod?.Config?.PassableByAll == true) {
                        __result = true;
                        ApplyPassingEffects(__instance, c, ref ___maxShake, ref ___shakeLeft);
                    }
                }
            } catch { }
        }

        private static bool isDrawing = false;

        private static void Prefix_Bush_draw(
            Bush __instance
        ) {
            if (AnyPassable(__instance)) {
                isDrawing = true;
            }
        }

        private struct CollisionContext {
            public bool Active;
            public Rectangle Position;
            public Character? Character;
            public bool Pathfinding;
            public bool Projectile;
        }

        private static CollisionContext collision;

        private static void Prefix_GameLocation_isCollidingPosition(
            Rectangle position, bool glider, Character? character, bool pathfinding, bool projectile,
            out CollisionContext __state
        ) {
            __state = collision;
            collision = new CollisionContext {
                Active = !glider,
                Position = position,
                Character = character,
                Pathfinding = pathfinding,
                Projectile = projectile,
            };
        }
        private static void Finalizer_GameLocation_isCollidingPosition(CollisionContext __state) {
            collision = __state;
        }

        private static void Postfix_Bush_getBoundingBox(
            Bush __instance,
            ref Rectangle __result, ref float ___maxShake, ref bool ___shakeLeft
        ) {
            if (isDrawing) {
                isDrawing = false;
                var skew = -46;
                __result = new Rectangle(__result.X, __result.Y + skew, __result.Width, __result.Height);
                return;
            }
            try {
                if (collision.Active && SmallPassable(__instance)) {
                    var c = collision.Character;
                    if (c is Farmer || Mod?.Config?.PassableByAll == true) {
                        if (!collision.Pathfinding && !collision.Projectile && c is not null
                            && __result.Intersects(collision.Position)
                            && !__result.Intersects(c.GetBoundingBox())) {
                            ApplyPassingEffects(__instance, c, ref ___maxShake, ref ___shakeLeft);
                        }
                        __result = Rectangle.Empty;
                    }
                }
            } catch { }
        }
    }
}

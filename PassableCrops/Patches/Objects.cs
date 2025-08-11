using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Linq;
using SObject = StardewValley.Object;

namespace PassableCrops.Patches {
    internal static class Objects {
        private static string KeyDataShake = null!;

        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;
            KeyDataShake = $"{Mod?.ModManifest?.UniqueID}/shake";
            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "isCollidingPosition", new Type[] { typeof(Rectangle), typeof(xTile.Dimensions.Rectangle), typeof(bool), typeof(int), typeof(bool), typeof(Character), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_GameLocation_isCollidingPosition)),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_GameLocation_isCollidingPosition))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "checkAction"),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_GameLocation_checkAction)),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_GameLocation_checkAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "isPassable"),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_Object_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "draw", new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_Object_draw)),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_Object_draw))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SpriteBatch), "Draw", new Type[] { typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(SpriteEffects), typeof(float) }),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_SpriteBatch_Draw1))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SpriteBatch), "Draw", new Type[] { typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color), typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects), typeof(float) }),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_SpriteBatch_Draw2))
            );
        }

        private static Character? LastCharacter = null;

        private static void Prefix_GameLocation_isCollidingPosition(Character character) => LastCharacter = character;

        private static void Postfix_GameLocation_isCollidingPosition() => LastCharacter = null;

        private static void Prefix_GameLocation_checkAction(Farmer who) => LastCharacter = who;

        private static void Postfix_GameLocation_checkAction() => LastCharacter = null;

        private const float shakeRate = (float)Math.PI / 100f;
        private const float maxShake_normal = (float)Math.PI / 12f;
        private const float maxShake_stiff = (float)Math.PI / 16f;

        private enum ObjType {
            None = 0,
            Scarecrow = 1,
            Sprinkler = 2,
            Forage = 3,
            Weed = 4,
            Custom = 5
        }

        private static ObjType GetObjType(SObject o) {
            if (o is null || Mod?.Config is null || Mod.Config.ExcludeObjects?.Any(i => string.Compare(i, o.Name, true) == 0) == true || Mod.Config.ExcludeObjects?.Any(i => string.Compare(i, o.QualifiedItemId, true) == 0) == true) {
                return ObjType.None;
            }
            if (Mod.Config.PassableSprinklers && o.IsSprinkler())
                return ObjType.Sprinkler;
            if (Mod.Config.PassableScarecrows && o.IsScarecrow())
                return ObjType.Scarecrow;
            if (Mod.Config.PassableForage && o.isForage() && o.Category != -9 && o.ParentSheetIndex != 590)
                return ObjType.Forage;
            if (Mod.Config.PassableWeeds
                && (o.GetContextTags()?.Any(c => c.Contains("item_weeds") || c.Contains("item_greenrainweeds")) == true)
                && !(new int[] { 319, 320, 321 }.Any(c => c == o.ParentSheetIndex)) //Ice Crystals are labeled as weeds so ignore them
            ) {
                return ObjType.Weed;
            }
            if (Mod.Config.IncludeObjects?.Any(i => string.Compare(i, o.Name, true) == 0) == true || Mod.Config.IncludeObjects?.Any(i => string.Compare(i, o.QualifiedItemId, true) == 0) == true) {
                return ObjType.Custom;
            }
            return ObjType.None;
        }

        private struct TempShakeData {
            public bool Passable;
            public ObjType ObjType;
            public string TypeDef;
            public float ShakeRotation;
        }

        private static TempShakeData LastShakeData;

        private static bool AnyPassable() =>
            Mod?.Config is not null
            && (Mod.Config.PassableScarecrows || Mod.Config.PassableSprinklers || Mod.Config.PassableForage || Mod.Config.PassableWeeds || Mod.Config.IncludeObjects.Any());

        private static void Postfix_Object_isPassable(
           SObject __instance,
           ref bool __result
       ) {
            if (!AnyPassable())
                return;
            var ot = GetObjType(__instance);
            if (ot != ObjType.None) {
                var farmer = LastCharacter as Farmer;
                if (farmer is not null || Mod?.Config?.PassableByAll == true) {
                    __result = true;
                    if (Mod?.Config?.SlowDownWhenPassing == true && farmer is not null) {
                        farmer.temporarySpeedBuff = farmer.stats.Get("Book_Grass") == 0 ? -1f : -0.33f;
                    }
                    LastShakeData.Passable = __result;
                    if (LastCharacter is not null) {
                        var location = __instance.Location;
                        if (location != Game1.currentLocation) {
                            return;
                        }
                        var positionOfCollider = LastCharacter.GetBoundingBox();
                        var speedOfCollision = LastCharacter.speed + LastCharacter.addedSpeed;
                        if (speedOfCollision > 0f
                            && (!__instance.modData.TryGetValue(KeyDataShake, out var data) || !float.TryParse((data ?? "").Split(';')[0], out var maxShake) || maxShake == 0f)
                            && positionOfCollider.Intersects(__instance.GetBoundingBox())
                        ) {
                            maxShake = (ot == ObjType.Scarecrow || ot == ObjType.Sprinkler ? maxShake_stiff : maxShake_normal) / Math.Min(1f, 5f / speedOfCollision);
                            var shakeLeft = positionOfCollider.Center.X > __instance.TileLocation.X * 64f + 32f;
                            var shakeRotation = 0f;
                            __instance.modData[KeyDataShake] = $"{maxShake};{shakeRotation};{shakeLeft}";
                            if (LastCharacter is not FarmAnimal && Utility.isOnScreen(new Point((int)__instance.TileLocation.X, (int)__instance.TileLocation.Y), 2, location)) {
                                Mod?.PlayRustleSound(__instance.TileLocation, __instance.Location);
                            }
                        }
                    }
                }
            }
        }

        private static void Prefix_Object_draw(
            SObject __instance
        ) {
            if (!Mod?.Config?.UseCustomDrawing == true || !AnyPassable()) {
                return;
            }
            var ot = GetObjType(__instance);
            if (ot != ObjType.None
                && __instance!.modData.TryGetValue(KeyDataShake, out var data)
            ) {
                var s = (data ?? "").Split(';');
                if (s.Length == 3
                    && float.TryParse(s[0], out var maxShake)
                    && float.TryParse(s[1], out var shakeRotation)
                    && bool.TryParse(s[2], out var shakeLeft)) {
                    // calc new shake data
                    if (maxShake > 0f) {
                        if (shakeLeft) {
                            shakeRotation -= shakeRate;
                            if (Math.Abs(shakeRotation) >= maxShake) {
                                shakeLeft = false;
                            }
                        } else {
                            shakeRotation += shakeRate;
                            if (shakeRotation >= maxShake) {
                                shakeLeft = true;
                                shakeRotation -= shakeRate;
                            }
                        }
                        maxShake = Math.Max(0f, maxShake - (float)Math.PI / 300f);
                    } else {
                        shakeRotation /= 2f;
                        if (shakeRotation <= 0.01f) {
                            shakeRotation = 0f;
                        }
                    }
                    // update tracking values
                    __instance.modData[KeyDataShake] = $"{maxShake};{shakeRotation};{shakeLeft}";
                    LastShakeData.TypeDef = __instance.TypeDefinitionId;
                    LastShakeData.ObjType = ot;
                    LastShakeData.ShakeRotation = shakeRotation;
                }
            }
        }

        private static void Postfix_Object_draw() => LastShakeData.ObjType = ObjType.None;

        private static void MoveRotation(ref Rectangle destinationRectangle, ref Vector2 origin, Vector2 move) {
            destinationRectangle = new Rectangle(destinationRectangle.Location.X + (int)move.X * 4, destinationRectangle.Location.Y + (int)move.Y * 4, destinationRectangle.Width, destinationRectangle.Height);
            origin += move;
        }

        private static void MoveRotation(ref Vector2 position, ref Vector2 origin, Vector2 move) {
            position += move * 4;
            origin += move;
        }

        private static void Prefix_SpriteBatch_Draw1(
            ref Rectangle destinationRectangle, ref float rotation, ref Vector2 origin
        ) {
            if (LastShakeData.ObjType != ObjType.None) {
                if (Mod?.Config?.ShakeWhenPassing == true) {
                    rotation = LastShakeData.ShakeRotation;
                }
                if (LastShakeData.ObjType == ObjType.Scarecrow) {
                    // move rotation to center bottom of post
                    MoveRotation(ref destinationRectangle, ref origin, new Vector2(8f, 30f));
                } else if (LastShakeData.TypeDef?.Equals("(BC)") == true) {
                    // move rotation to center bottom-ish of object
                    MoveRotation(ref destinationRectangle, ref origin, new Vector2(8f, 24f));
                }
            }
        }

        private static void Prefix_SpriteBatch_Draw2(
            ref Vector2 position, ref float rotation, ref Vector2 origin, ref float layerDepth
        ) {
            if (LastShakeData.ObjType != ObjType.None) {
                if (Mod?.Config?.ShakeWhenPassing == true) {
                    rotation = LastShakeData.ShakeRotation;
                }
                switch (LastShakeData.ObjType) {
                    case ObjType.Weed:
                        layerDepth += (LastShakeData.Passable ? 24 : -40) / 10000f;
                        // move rotation near bottom
                        MoveRotation(ref position, ref origin, new Vector2(0f, 8f));
                        break;
                    case ObjType.Sprinkler:
                        layerDepth += (LastShakeData.Passable ? 45 : -19) / 10000f;
                        // rotation stays centered
                        break;
                    case ObjType.Forage:
                        layerDepth += (LastShakeData.Passable ? 32 : -32) / 10000f;
                        // move rotation near bottom
                        MoveRotation(ref position, ref origin, new Vector2(0f, 8f));
                        break;
                }
            }
        }
    }
}
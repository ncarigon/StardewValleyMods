using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using static StardewValley.Minigames.MineCart;
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
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.isCollidingPosition), new Type[] { typeof(Rectangle), typeof(xTile.Dimensions.Rectangle), typeof(bool), typeof(int), typeof(bool), typeof(Character), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_GameLocation_isCollidingPosition))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.checkAction)),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_GameLocation_checkAction)),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_GameLocation_checkAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.isPassable)),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_Object_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.GetBoundingBoxAt)),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_Object_GetBoundingBoxAt))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.draw), new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_Object_draw)),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_Object_draw))
            );
        }

        private static Character? LastCharacter;

        private static void Prefix_GameLocation_isCollidingPosition(
            Character character
        ) {
            LastCharacter = character;
        }
        
        private static void Postfix_GameLocation_isCollidingPosition() {
            LastCharacter = null;
        }

        private static void Prefix_GameLocation_checkAction(Farmer who) => LastCharacter = who;

        private static void Postfix_GameLocation_checkAction() => LastCharacter = null;

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
            if (Mod.Config.PassableSprinklers && o.IsSprinkler()) {
                return ObjType.Sprinkler;
            }
            if (Mod.Config.PassableScarecrows && o.IsScarecrow()) {
                return ObjType.Scarecrow;
            }
            if (Mod.Config.PassableForage && o.isForage() && o.Category != -9 && o.ParentSheetIndex != 590) {
                return ObjType.Forage;
            }
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

        private static bool AnyPassable() =>
            Mod?.Config is not null
            && (Mod.Config.PassableScarecrows || Mod.Config.PassableSprinklers || Mod.Config.PassableForage || Mod.Config.PassableWeeds || Mod.Config.IncludeObjects.Any());

        private static void Postfix_Object_isPassable(
           SObject __instance,
           ref bool __result
       ) {
            if (AnyPassable() && LastCharacter is not null && (LastCharacter is Farmer || Mod?.Config?.PassableByAll == true)
            ) {
                var ot = GetObjType(__instance);
                if (ot != ObjType.None) {
                    __result = true;
                    if (LastCharacter.GetBoundingBox().Intersects(__instance.GetBoundingBox())) {
                        var pos = $"{LastCharacter.position.X}:{LastCharacter.position.Y}";
                        if (!__instance.modData.TryGetValue(KeyDataShake, out var data) || data != pos) {
                            __instance.shakeTimer = 200;
                            __instance.modData[KeyDataShake] = pos;
                        }
                    }
                }
            }
        }

        private static ObjType LastObjType;

        private static void Prefix_Object_draw(
            SObject __instance
        ) {
            if (!__instance.hovering && AnyPassable()) {
                var ot = GetObjType(__instance);
                if (ot != ObjType.None) {
                    LastObjType = ot;
                    if (Mod?.Config?.SlowDownWhenPassing == true && Game1.player.GetBoundingBox().Intersects(__instance.GetBoundingBox())) {
                        var isBig = ot == ObjType.Scarecrow;
                        var amount = (isBig && Mod?.Config?.SlowDownMore == true ? -3f : -1f) * (Game1.player.stats.Get("Book_Grass") != 0 ? 0.33f : 1f);
                        Game1.player.temporarySpeedBuff = amount;
                        if (__instance.shakeTimer > 0 && (ot == ObjType.Scarecrow || ot == ObjType.Weed || ot == ObjType.Forage)) {
                            Mod?.PlayRustleSound(__instance.TileLocation, __instance.Location);
                        }
                    }
                }
            }
        }

        private static void Postfix_Object_draw() {
            LastObjType = ObjType.None;
        }

        private static void Postfix_Object_GetBoundingBoxAt(
            ref Rectangle __result
        ) {
            switch (LastObjType) {
                case ObjType.Weed:
                    __result = new Rectangle(__result.X, __result.Y + 24, __result.Width, __result.Height);
                    break;
                case ObjType.Sprinkler:
                    __result = new Rectangle(__result.X, __result.Y + 45, __result.Width, __result.Height);
                    break;
                case ObjType.Forage:
                    __result = new Rectangle(__result.X, __result.Y + 32, __result.Width, __result.Height);
                    break;
            }
        }
    }
}
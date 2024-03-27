using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;

namespace PassableCrops.Patches {
    internal static class Objects {
        private static string
            KeyDataShake = null!;

        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;

            KeyDataShake = $"{Mod?.ModManifest?.UniqueID}/shake";

            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "isPassable"),
                postfix: new HarmonyMethod(typeof(Objects), nameof(Postfix_Object_isPassable))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "draw", new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(typeof(Objects), nameof(Prefix_Object_draw)),
                transpiler: new HarmonyMethod(typeof(Objects), nameof(Transpile_Object_Draw))
            );
        }

        private const float shakeRate = (float)Math.PI / 100f;
        private const float maxShake_normal = (float)Math.PI / 12f;
        private const float maxShake_stiff = (float)Math.PI / 16f;

        private enum ObjType {
            None = 0,
            Scarecrow = 1,
            Sprinkler = 2,
            Forage = 3,
            Weed = 4
        }

        private static ObjType GetObjType(SObject o) {
            if ((Mod?.Config?.PassableScarecrows ?? false) && (o?.IsScarecrow() ?? false))
                return ObjType.Scarecrow;
            if ((Mod?.Config?.PassableSprinklers ?? false) && (o?.IsSprinkler() ?? false))
                return ObjType.Sprinkler;
            if ((Mod?.Config?.PassableForage ?? false) && (o?.isForage() ?? false) && (o?.Category != -9) && (o?.ParentSheetIndex != 590))
                return ObjType.Forage;
            if ((Mod?.Config?.PassableWeeds ?? false)
                && (o?.HasContextTag("item_weeds") ?? false)
                && (o.ParentSheetIndex < 319 || o.ParentSheetIndex > 321) //Ice Crystals are labeled as weeds so ignore them
            ) {
                return ObjType.Weed;
            }
            return ObjType.None;
        }

        private static bool AnyPassable() {
            return Mod?.Config is not null && (Mod.Config.PassableScarecrows || Mod.Config.PassableSprinklers || Mod.Config.PassableForage || Mod.Config.PassableWeeds);
        }

        private static void Postfix_Object_isPassable(
            // instance and result
            SObject __instance, ref bool __result
        ) {
            try {
                if (!AnyPassable())
                    return;
                var bb = Game1.player.GetBoundingBox();
                bb.Inflate(16, 16); // slightly larger box to allow intersection before actually touching it
                // this extra check is needed to stop objects from shaking on their own when loading a new map
                var ib = __instance.GetBoundingBoxAt((int)__instance.TileLocation.X, (int)__instance.TileLocation.Y);
                if (!ib.Intersects(bb))
                    return;
                var ot = GetObjType(__instance);
                if (ot != ObjType.None) {
                    // makes it passable
                    __result = true;
                    if (Mod?.Config?.SlowDownWhenPassing ?? false)
                        Game1.player.temporarySpeedBuff = Game1.player.stats.Get("Book_Grass") == 0 ? -1f : -0.33f;
                    // makes it shake when passed
                    if ((ot == ObjType.Scarecrow || Game1.player.movedDuringLastTick()) // don't keep retriggering
                        && ib.Intersects(Game1.player.GetBoundingBox()) // don't use the inflated one from earlier
                        && (!__instance!.modData.TryGetValue(KeyDataShake, out var data)
                        || !float.TryParse((data ?? "").Split(';')[0], out var maxShake)
                        || maxShake <= 0f)
                    ) {
                        maxShake = ot == ObjType.Scarecrow || ot == ObjType.Sprinkler ? maxShake_stiff : maxShake_normal;
                        bool shakeLeft = Game1.player.GetBoundingBox().Center.X > __instance.TileLocation.X * 64f + 32f;
                        float shakeRotation = 0f;
                        __instance.modData[KeyDataShake] = $"{maxShake};{shakeRotation};{shakeLeft}";
                    }
                }
            } catch { }
        }

        private static IEnumerable<CodeInstruction> Transpile_Object_Draw(IEnumerable<CodeInstruction> instructions) {
            foreach (var instruction in instructions) {
                if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == AccessTools.Method(typeof(SpriteBatch), "Draw", new Type[] {
                    typeof(Texture2D), typeof(Rectangle), typeof(Rectangle?), typeof(Color),
                    typeof(float), typeof(Vector2), typeof(SpriteEffects), typeof(float) })
                   ) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // add object instance to end of arguments list
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Objects), nameof(Draw1)));
                } else if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == AccessTools.Method(typeof(SpriteBatch), "Draw", new Type[] {
                    typeof(Texture2D), typeof(Vector2), typeof(Rectangle?), typeof(Color),
                    typeof(float), typeof(Vector2), typeof(float), typeof(SpriteEffects), typeof(float) })
                ) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // add object instance to end of arguments list
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Objects), nameof(Draw2)));
                } else {
                    yield return instruction;
                }
            }
        }

        private static void Prefix_Object_draw(
            SObject __instance
        ) {
            try {
                if (!(Mod?.Config?.UseCustomDrawing ?? false) || !AnyPassable())
                    return;
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
                    }
                }
            } catch { }
        }

        private static void MoveRotation(ref Rectangle destinationRectangle, ref Vector2 origin, Vector2 move) {
            destinationRectangle = new Rectangle(destinationRectangle.Location.X + (int)move.X * 4, destinationRectangle.Location.Y + (int)move.Y * 4, destinationRectangle.Width, destinationRectangle.Height);
            origin += move;
        }

        private static void MoveRotation(ref Vector2 position, ref Vector2 origin, Vector2 move) {
            position += move * 4;
            origin += move;
        }

        private static void Draw1(SpriteBatch spriteBatch, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth, SObject obj) {
            if ((Mod?.Config?.UseCustomDrawing ?? false) && AnyPassable()) {
                var ot = GetObjType(obj);
                if (ot != ObjType.None
                    && rotation == 0f
                    //&& obj!.modData.TryGetValue(KeyShakeRotation, out var data)
                    && obj!.modData.TryGetValue(KeyDataShake, out var data)
                    && float.TryParse(((data ?? "") + ";").Split(';')[1], out var r)
                ) {
                    rotation = r;
                    switch (ot) {
                        case ObjType.Scarecrow:
                            // move rotation point to bottom of post
                            MoveRotation(ref destinationRectangle, ref origin, new Vector2(8f, 30f));
                            break;
                    }
                }
            }
            spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth);
        }

        private static void Draw2(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth, SObject obj) {
            if ((Mod?.Config?.UseCustomDrawing ?? false) && AnyPassable()) {
                var ot = GetObjType(obj);
                if (ot != ObjType.None
                    && rotation == 0f
                    //&& obj!.modData.TryGetValue(KeyShakeRotation, out var data)
                    && obj!.modData.TryGetValue(KeyDataShake, out var data)
                    && float.TryParse(((data ?? "") + ";").Split(';')[1], out var r)
                ) {
                    rotation = r;
                    switch (ot) {
                        case ObjType.Weed:
                            layerDepth += 24 / 10000f;
                            MoveRotation(ref position, ref origin, new Vector2(0f, 8f));
                            break;
                        case ObjType.Sprinkler:
                            layerDepth += 36 / 10000f;
                            break;
                        case ObjType.Forage:
                            layerDepth += 36 / 10000f;
                            MoveRotation(ref position, ref origin, new Vector2(0f, 8f));
                            break;
                    }
                }
            }
            spriteBatch.Draw(texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth);
        }
    }
}

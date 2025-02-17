using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace AdjustableSprinklers {
    internal static class Patches {
        public static void Register(ModEntry mod) {
            var harmony = new Harmony(mod.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "GetBaseRadiusForSprinkler"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_GetBaseRadiusForSprinkler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "GetModifiedRadiusForSprinkler"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_GetModifiedRadiusForSprinkler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "GetSprinklerTiles"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_GetSprinklerTiles))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "drawPlacementBounds"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_drawPlacementBounds))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "ApplySprinkler"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_ApplySprinkler))
            );
            mod.Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
        }

        private static readonly Random R = new();

        [HarmonyPriority(Priority.VeryLow)]
        private static void Postfix_Object_GetBaseRadiusForSprinkler(
            SObject __instance, ref int __result
        ) {
            if (__instance is not null && __result != -1) {
                var radius = Config.Instance.BaseRadius;
                var increase = Config.Instance.TierIncrease;
                __result = __instance.QualifiedItemId switch {
                    "(O)599" => radius,
                    "(O)621" => radius + increase,
                    "(O)645" => radius + (increase * 2),
                    "(O)1113" => radius + (increase * 3),
                    _ => -1
                };
                if (__result == -1) {
                    __result = __instance.Name switch {
                        "Sprinkler" => radius,
                        "Quality Sprinkler" => radius + increase,
                        "Iridium Sprinkler" => radius + (increase * 2),
                        "Prismatic Sprinkler" => radius + (increase * 3),
                        _ => -1
                    };
                }
            }
        }

        [HarmonyPriority(Priority.VeryLow)]
        private static void Postfix_Object_GetModifiedRadiusForSprinkler(
            SObject __instance, ref int __result
        ) {
            if (__instance is not null && __result != -1) {
                if (__instance.heldObject.Value != null && __instance.heldObject.Value.QualifiedItemId == "(O)915") {
                    // need to -1 here to remove vanilla game logic and adjust to the configured value
                    __result += Config.Instance.TierIncrease - 1;
                }
            }
        }

        [HarmonyPriority(Priority.VeryLow)]
        private static void Postfix_Object_GetSprinklerTiles(
            SObject __instance, ref List<Vector2> __result
        ) {
            if (Config.Instance.CircularArea == true && __instance?.IsSprinkler() == true) {
                __result = GetTiles(__instance.TileLocation, __instance.GetModifiedRadiusForSprinkler(), true);
            }
        }

        private static void Postfix_Object_drawPlacementBounds(
            SObject __instance, SpriteBatch spriteBatch
        ) {
            if (__instance is not null && spriteBatch is not null
                && (Config.Instance.ShowSprinklerArea == true || Config.Instance.ShowScarecrowArea == true)
            ) {
                List<Vector2>? tiles = null;
                if (Config.Instance.ShowSprinklerArea == true && __instance.IsSprinkler()) {
                    tiles = __instance.GetSprinklerTiles();
                } else if (Config.Instance.ShowScarecrowArea == true && __instance.IsScarecrow()) {
                    tiles = GetTiles(__instance.TileLocation, __instance.GetRadiusForScarecrow(), false);
                }
                if (tiles is not null) {
                    foreach (Vector2 v in tiles) {
                        spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(new Vector2((int)v.X * 64, (int)v.Y * 64)), new Rectangle(194, 388, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.01f);
                    }
                }
            }
        }

        private static List<Vector2> GetTiles(Vector2 center, int radius, bool round) {
            var tiles = new List<Vector2>();
            for (var x = -radius; x <= radius; x++) {
                for (var y = -radius; y <= radius; y++) {
                    var tile = center + new Vector2(x, y);
                    if (Vector2.Distance(center, tile) < radius + (round ? 0.5 : 0.0)) {
                        tiles.Add(tile);
                    }
                }
            }
            return tiles;
        }

        private static void Input_ButtonPressed(object? sender, StardewModdingAPI.Events.ButtonPressedEventArgs e) {
            if (Config.Instance.ActivateWhenClicked == true
                && e?.Button.IsActionButton() == true
                && Game1.player?.currentLocation is not null
            ) {
                var obj = Game1.player.currentLocation.getObjectAtTile((int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y);
                if (obj?.IsSprinkler() == true) {
                    obj.ApplySprinklerAnimation();
                    foreach (var tile in obj.GetSprinklerTiles()) {
                        Task.Delay(R.Next(500, 2000)) // INFO: adding a delay here for a more natural look
                            .ContinueWith(_ => obj.ApplySprinkler(tile));
                    }
                }
            }
        }

        private static void Postfix_Object_ApplySprinkler(
            SObject __instance, Vector2 tile
        ) {
            if (Config.Instance.WaterGardenPots == true
                && __instance?.Location?.Objects?.TryGetValue(tile, out var t) == true
                && t is IndoorPot pot && pot is not null
            ) {
                Task.Delay(R.Next(500, 2000)) // INFO: adding a delay here for a more natural look
                            .ContinueWith(_ => pot?.Water());
            }
            if (Config.Instance.WaterPetBowls == true) {
                __instance?.Location?.buildings?
                    .Where(b => b.occupiesTile(tile) && b is PetBowl)?
                    .Select(b => b as PetBowl)
                    .Where(b => b is not null)
                    .Do(b => Task.Delay(R.Next(500, 2000)) // INFO: adding a delay here for a more natural look
                            .ContinueWith(_ => b!.watered.Value = true)
                    );
            }
            if (Config.Instance.WaterSlimeHutch == true
                && __instance?.Location is SlimeHutch hutch
            ) {
                Task.Delay(R.Next(500, 2000)) // INFO: adding a delay here for a more natural look
                            .ContinueWith(_ => hutch.performToolAction(default(WateringCan), (int)tile.X, (int)tile.Y));
            }
        }
    }
}
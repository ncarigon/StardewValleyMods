using System;
using System.Threading;
using Force.DeepCloner;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace GardenPotAutomate {
    internal static class Patches {
        private static IMonitor Monitor = null!;
        private static Config Config = null!;

        public static void Register(IModHelper helper, IMonitor monitor, Config config) {
            Monitor = monitor;
            Config = config;
            var harmony = new Harmony(helper.ModContent.ModID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), "gainExperience"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Farmer_gainExperience))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), "addItemToInventoryBool"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Farmer_addItemToInventoryBool))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "createItemDebris"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Game1_createItemDebris))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "get_player"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Game1_get_player))
            );

            //harmony.Patch(
            //    original: AccessTools.Method(typeof(Pickaxe), "DoFunction"),
            //    prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Pickaxe_DoFunction))
            //);
            //harmony.Patch(
            //    original: AccessTools.Method(typeof(StardewValley.Object), "placementAction"),
            //    postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_placementAction))
            //);
            //harmony.Patch(
            //    original: AccessTools.Method(typeof(StardewValley.Object), "maximumStackSize"),
            //    postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_maximumStackSize))
            //);
            //harmony.Patch(
            //    original: AccessTools.Method(typeof(StardewValley.Object), "loadDisplayName"),
            //    postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_loadDisplayName))
            //);
        }

        //private static void Prefix_Pickaxe_DoFunction(GameLocation location, int x, int y, Farmer who) {
        //    var toolTilePosition = new Vector2(x / 64, y / 64);
        //    if (location.Objects.TryGetValue(toolTilePosition, out var obj)
        //        && obj is IndoorPot pot && IsPotModified(pot)
        //    ) {
        //        location.debris.Add(new Debris(pot, who.GetToolLocation(), who.GetBoundingBox().Center.ToVector2()));
        //        pot.performRemoveAction();
        //        location.Objects.Remove(toolTilePosition);
        //    }
        //}

        //private static void Postfix_Object_placementAction(
        //    StardewValley.Object __instance,
        //    GameLocation location, int x, int y
        //) {
        //    if (__instance is IndoorPot pot && IsPotModified(pot)
        //        && location.Objects[new Vector2(x / 64, y / 64)] is IndoorPot newPot
        //        && newPot is not null
        //    ) {
        //        newPot.bush.Value = pot.bush.Value;
        //        newPot.hoeDirt.Value = pot.hoeDirt.Value;
        //    }
        //}

        //private static void Postfix_Object_maximumStackSize(StardewValley.Object __instance, ref int __result) {
        //    if (__instance is IndoorPot pot && IsPotModified(pot)) {
        //        __result = 1;
        //    }
        //}

        //private static void Postfix_Object_loadDisplayName(StardewValley.Object __instance, ref string __result) {
        //    if (__instance is IndoorPot pot && IsPotModified(pot)) {
        //        if (pot.hoeDirt?.Value?.crop is not null) {
        //            __result += " (" + new StardewValley.Object(pot.hoeDirt.Value.crop.indexOfHarvest.Value, 1).DisplayName + ")";
        //        } else if (pot.bush?.Value is not null) {
        //            __result += " (" + pot.bush.Value.GetType().Name + ")";
        //        } else if (pot.hoeDirt?.Value?.fertilizer?.Value is not null) {
        //            __result += " (" + new StardewValley.Object(pot.hoeDirt.Value.fertilizer.Value.Replace("(O)", ""), 1).DisplayName + ")";
        //        }
        //    }
        //}

        //private static bool IsPotModified(IndoorPot indoorPot) =>
        //    indoorPot?.hoeDirt?.Value?.crop is not null
        //    || indoorPot?.bush?.Value is not null
        //    || indoorPot?.hoeDirt.Value?.fertilizer?.Value is not null;

        // track fake player
        internal static Farmer Harvester = null!;

        // swaps in the fake player when harvesting from garden pot routines
        private static void Postfix_Game1_get_player(ref Farmer __result) {
            if (Harvester is null)
                return;
            __result = Harvester;
        }

        // track owner
        internal static Farmer Owner = null!;

        private static void Prefix_Farmer_gainExperience(Farmer __instance, int which, int howMuch) {
            try {
                // in a harvest routine, so the fake player ran the normal logic already
                if (Config.GainExperience && Owner is not null) {
                    if (__instance.UniqueMultiplayerID != Owner.UniqueMultiplayerID) {
                        Owner.gainExperience(which, howMuch);
                    }
                }
            } catch (Exception ex) { Monitor.Log($"Failed to gainExperience. Message: {ex.Message}", LogLevel.Error); }
        }

        // gather harvested items
        internal static Action<Item> Items = null!;

        private static bool Prefix_Farmer_addItemToInventoryBool(ref bool __result, Item item) {
            try {
                if (Items is not null) {
                    Items(item);
                    __result = true;
                    return false;
                }
            } catch (Exception ex) { Monitor.Log($"Failed to addItemToInventoryBool. Message: {ex.Message}", LogLevel.Error); }
            return true;
        }

        private static bool Prefix_Game1_createItemDebris(ref Debris __result, Item item) {
            try {
                if (Items is not null) {
                    Items(item);
                    __result = null!;
                    return false;
                }
            } catch (Exception ex) { Monitor.Log($"Failed to createItemDebris. Message: {ex.Message}", LogLevel.Error); }
            return true;
        }
    }
}

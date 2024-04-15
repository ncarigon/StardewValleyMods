using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace GardenPotOptions {
    internal static class Patches {
        public static void Register(IModHelper helper) {
            var harmony = new Harmony(helper.ModContent.ModID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Pickaxe), "DoFunction"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Pickaxe_DoFunction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Axe), "DoFunction"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Axe_DoFunction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Hoe), "DoFunction"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_Hoe_DoFunction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "placementAction"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_placementAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "loadDisplayName"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_loadDisplayName))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "maximumStackSize"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_maximumStackSize))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), "ApplySprinkler"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_Object_ApplySprinkler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(PlantableRule), "ShouldApplyWhen"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_PlantableRule_ShouldApplyWhen))
            );
        }

        private static bool IsValidPot(SObject obj, out IndoorPot? pot) => (pot = obj as IndoorPot) is not null
            && (pot.hoeDirt?.Value?.crop is not null
            || pot.bush?.Value is not null
            || pot.hoeDirt?.Value?.fertilizer?.Value is not null
            || pot.heldObject?.Value is not null);

        private static void Prefix_Tool_DoFunction(GameLocation location, int x, int y, Farmer who) {
            try {
                if (ModEntry.ModConfig?.KeepContents ?? false) {
                    var tile = new Vector2(x / 64, y / 64);
                    if ((location?.Objects?.TryGetValue(tile, out var obj) ?? false)
                         && IsValidPot(obj, out var pot)
                         && who is not null
                    ) {
                        location.debris?.Add(new Debris(pot, who.GetToolLocation(), who.GetBoundingBox().Center.ToVector2()));
                        pot!.performRemoveAction();
                        location.Objects.Remove(tile);
                    }
                }
            } catch { }
        }

        private static void Prefix_Pickaxe_DoFunction(GameLocation location, int x, int y, Farmer who) {
            if ((ModEntry.ModConfig?.KeepContents ?? false) && "Pickaxe" == ModEntry.ModConfig?.SafeTool) {
                Prefix_Tool_DoFunction(location, x, y, who);
            }
        }

        private static void Prefix_Axe_DoFunction(GameLocation location, int x, int y, Farmer who) {
            if ((ModEntry.ModConfig?.KeepContents ?? false) && "Axe" == ModEntry.ModConfig?.SafeTool) {
                Prefix_Tool_DoFunction(location, x, y, who);
            }
        }

        private static void Prefix_Hoe_DoFunction(GameLocation location, int x, int y, Farmer who) {
            if ((ModEntry.ModConfig?.KeepContents ?? false) && "Hoe" == ModEntry.ModConfig?.SafeTool) {
                Prefix_Tool_DoFunction(location, x, y, who);
            }
        }

        private static void Postfix_Object_placementAction(SObject __instance, GameLocation location, int x, int y) {
            try {
                var tile = new Vector2(x / 64, y / 64);
                if ((ModEntry.ModConfig?.KeepContents ?? false) && IsValidPot(__instance, out var pot)
                    && (location?.Objects?.TryGetValue(tile, out var p) ?? false)
                    && p is IndoorPot newPot && newPot is not null
                ) {
                    // INFO: directly swapping does not seem to work after the first placement
                    //location.objects.Remove(tile);
                    //location.objects.Add(tile, __instance);

                    // INFO: so we transplant to the new pot instead
                    try { newPot.bush.Value = pot.bush.Value; } catch { }
                    try { newPot.hoeDirt.Value = pot.hoeDirt.Value; } catch { }
                    try { newPot.heldObject.Value = pot.heldObject.Value; } catch { }
                    try { newPot.showNextIndex.Value = pot.showNextIndex.Value; } catch { }

                    // INFO: disassociate with the original pot
                    try { pot.bush.Value = null; } catch { }
                    try { pot.hoeDirt.Value = null; } catch { }
                    try { pot.heldObject.Value = null; } catch { }

                    // INFO: update locations
                    try { newPot.bush.Value.Location = location; } catch { }
                    try { newPot.hoeDirt.Value.Location = location; } catch { }
                    try { newPot.hoeDirt.Value.crop.currentLocation = location; } catch { }
                    try { newPot.heldObject.Value.Location = location; } catch { }

                    // INFO: update tiles
                    try { newPot.bush.Value.Tile = tile; } catch { }
                    try { newPot.hoeDirt.Value.Tile = tile; } catch { }
                    try { newPot.hoeDirt.Value.crop.tilePosition = tile; } catch { }
                    try { newPot.heldObject.Value.TileLocation = tile; } catch { }
                }
            } catch { }
        }

        private static void Postfix_Object_loadDisplayName(SObject __instance, ref string __result) {
            try {
                if ((ModEntry.ModConfig?.KeepContents ?? false) && IsValidPot(__instance, out var pot)) {
                    if (pot?.hoeDirt?.Value?.crop?.indexOfHarvest?.Value is not null) {
                        __result += $" ({new SObject(pot.hoeDirt.Value.crop.indexOfHarvest.Value, 1).DisplayName})";
                    } else if (pot?.bush?.Value is not null) {
                        __result += $" ({(pot.bush.Value.size.Value == 3 ? new SObject("251", 1).DisplayName : "Bush")})";
                    } else if (pot?.hoeDirt?.Value?.fertilizer?.Value is not null) {
                        __result += $" ({new SObject(pot.hoeDirt.Value.fertilizer.Value.Replace("(O)", ""), 1).DisplayName})";
                    } else if (pot?.heldObject?.Value?.DisplayName is not null) {
                        __result += " (" + pot.heldObject.Value.DisplayName + ")";
                    }
                }
            } catch { }
        }

        private static void Postfix_Object_maximumStackSize(SObject __instance, ref int __result) {
            try {
                if ((ModEntry.ModConfig?.KeepContents ?? false) && IsValidPot(__instance, out _)) {
                    __result = 1;
                }
            } catch { }
        }

        private static void Postfix_Object_ApplySprinkler(SObject __instance, Vector2 tile) {
            try {
                if ((ModEntry.ModConfig?.EnableSprinklers ?? false)
                    && (__instance?.Location?.Objects?.TryGetValue(tile, out var obj) ?? false) && obj is IndoorPot pot
                    && (pot?.hoeDirt?.Value?.state?.Value ?? 2) != 2
                ) {
                    pot!.hoeDirt.Value.state.Value = 1;
                    pot.showNextIndex.Value = true;
                }
            } catch { }
        }

        private static void Prefix_PlantableRule_ShouldApplyWhen(PlantableRule __instance, ref bool isGardenPot) {
            try {
                if ((ModEntry.ModConfig?.AllowAncientSeeds ?? false)
                    && (__instance?.Id ?? "") == "NoGardenPots" && isGardenPot
                    && (__instance!.DeniedMessage ?? "").Contains("AncientFruit")
                ) {
                    isGardenPot = false;
                }
            } catch { }
        }
    }
}

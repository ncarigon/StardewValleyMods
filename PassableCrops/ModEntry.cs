using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.TerrainFeatures;
using StardewValley;

namespace PassbleCrops {
    public class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(HoeDirt), "isPassable", new Type[] { typeof(Character) }),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(PassableIfFarmer))
            );
        }

        private static void PassableIfFarmer(HoeDirt __instance, Character c, ref bool __result) {
            try {
                __result |= __instance.crop != null && c is Farmer;
            } catch { }
        }
    }
}

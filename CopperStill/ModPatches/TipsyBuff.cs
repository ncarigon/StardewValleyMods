using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;

namespace CopperStill.ModPatches {
    internal static class TipsyBuff {
        public static void Register(IModHelper helper) {
            var harmony = new Harmony(helper.ModContent.ModID);
            harmony.Patch(
                original: AccessTools.Method(typeof(BuffManager), "Apply"),
                prefix: new HarmonyMethod(typeof(TipsyBuff), nameof(Prefix_BuffManager_Apply))
            );
        }

        private static bool Prefix_BuffManager_Apply(
            BuffManager __instance, Buff buff
        ) {
            if (__instance is not null && buff?.source is not null && buff.id != Buff.tipsy &&
                (buff.source.Contains("Brandy") || buff.source.Contains("Vodka") || buff.source.Contains("Gin")
                || buff.source.Contains("Tequila") || buff.source.Contains("Moonshine") || buff.source.Contains("Whiskey")
                || buff.source.Contains("Rum") || buff.source.Contains("Soju") || buff.source.Contains("Sake"))
            ) {
                __instance.Apply(new Buff(Buff.tipsy));
                return false;
            }
            return true;
        }
    }
}

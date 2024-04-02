using HarmonyLib;
using StardewModdingAPI;

namespace GardenPotOptions {
    internal class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            var config = Config.Register(helper);
            var harmony = new Harmony(helper.ModContent.ModID);
            Patches.Register(harmony, config);
        }
    }
}

using HarmonyLib;
using StardewModdingAPI;

namespace BetterFishPonds {
    internal sealed class ModEntry : Mod {
        internal static ModEntry? Instance;
        internal Harmony? ModHarmony;

        public override void Entry(IModHelper helper) {
            Instance = this;
            this.ModHarmony = new Harmony(helper.ModContent.ModID);
            Config.Register();
            Patches.Register();
        }
    }
}
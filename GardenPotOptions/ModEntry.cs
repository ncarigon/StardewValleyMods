using HarmonyLib;
using StardewModdingAPI;

namespace GardenPotOptions {
    internal class ModEntry : Mod {
        public static ModEntry? Instance { get; private set; }

        public Harmony? ModHarmony { get; private set; }
        public Config? ModConfig;

        public override void Entry(IModHelper helper) {
            Instance = this;
            ModHarmony = new Harmony(helper.ModContent.ModID);
            Config.Register();
            Patches.Register();
            Events.Register();
        }
    }
}

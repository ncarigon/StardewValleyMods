using StardewModdingAPI;

namespace GardenPotOptions {
    internal class ModEntry : Mod {
        public static ModEntry? Instance;

        public Config? ModConfig;

        public override void Entry(IModHelper helper) {
            Instance = this;
            Config.Register();
            Patches.Register();
            Events.Register();
        }
    }
}

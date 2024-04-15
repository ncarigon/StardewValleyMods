using StardewModdingAPI;

namespace GardenPotOptions {
    internal class ModEntry : Mod {
        public static Config? ModConfig;

        public override void Entry(IModHelper helper) {
            ModConfig = Config.Register(helper);
            Patches.Register(helper);
        }
    }
}

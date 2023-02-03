using StardewModdingAPI;

namespace CopperStill {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            var config = ModConfig.Register(helper);
            ModPatches.AdjustPricing.Register(helper, config);
            ModPatches.JuniperBerry.Register(helper);
        }
    }
}

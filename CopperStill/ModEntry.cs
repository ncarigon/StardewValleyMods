using StardewModdingAPI;

namespace CopperStill {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            ModPatches.AdjustPricing.Register(helper);
            ModPatches.JuniperBerry.Register(helper);
        }
    }
}

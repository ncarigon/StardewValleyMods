using StardewModdingAPI;

namespace GardenPotAutomate {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            var config = ModConfig.Register(helper);
            AutomationFactory.Register(helper, config);
        }
    }
}

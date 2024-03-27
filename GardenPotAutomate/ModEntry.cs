using StardewModdingAPI;

namespace GardenPotAutomate {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            var config = Config.Register(helper);
            Patches.Register(helper, this.Monitor, config);
            AutomationFactory.Register(helper, config);
        }
    }
}

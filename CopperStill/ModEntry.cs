using StardewModdingAPI;

namespace CopperStill {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            var config = Config.Register(helper, this.Monitor);
            ModPatches.AdjustPricing.Register(helper, this.Monitor);
            ModPatches.ModifyBundle.Register(helper, this.Monitor, config);
            ModPatches.TipsyBuff.Register(helper);
        }
    }
}

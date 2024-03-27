using StardewModdingAPI;

namespace CopperStill {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            ModPatches.AdjustPricing.Register(helper, Monitor);
            ModPatches.ModifyBundle.Register(helper, Monitor);
            ModPatches.TipsyBuff.Register(helper);
        }
    }
}

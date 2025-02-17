using StardewModdingAPI;

namespace AdjustableSprinklers {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            Config.Register(this);
            Patches.Register(this);
        }
    }
}
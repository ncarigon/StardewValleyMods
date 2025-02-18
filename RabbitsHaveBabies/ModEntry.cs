using StardewModdingAPI;

namespace RabbitsHaveBabies {
    internal sealed class ModEntry : Mod {
        internal static ModEntry? Instance = null;

        public override void Entry(IModHelper helper) {
            Instance = this;
            Config.Register();
            Data.Edit();
            Patches.Patch();
        }
    }
}

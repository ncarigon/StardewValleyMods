using StardewModdingAPI;

namespace PassableCrops {
    internal class ModEntry : Mod {
        public Config? Config { get; private set; }

        public override void Entry(IModHelper helper) {
            this.Config = Config.Register(helper);
            Patches.Crops.Register(this);
            Patches.Objects.Register(this);
            Patches.Bushes.Register(this);
            Patches.Trees.Register(this);
            Patches.FruitTrees.Register(this);
        }
    }
}

using HarmonyLib;
using StardewModdingAPI;

namespace CopperStill {
    internal sealed class ModEntry : Mod {
        public static ModEntry? Instance { get; private set; }

        public Harmony? ModHarmony { get; private set; }

        public override void Entry(IModHelper helper) {
            Instance = this;
            this.ModHarmony = new Harmony(helper.ModContent.ModID);
            Config.Register();
            ModPatches.ModifyBundle.Register();
            ModPatches.SDVPatches.Register();
            ModPatches.ItemSpawner.Register();
        }
    }
}

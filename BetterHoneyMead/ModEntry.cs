using HarmonyLib;
using StardewModdingAPI;

namespace BetterHoneyMead {
    internal sealed class ModEntry : Mod {
        public static ModEntry? Instance { get; private set; }

        public Harmony? ModHarmony { get; private set; }

        public override void Entry(IModHelper helper) {
            Instance = this;
            this.ModHarmony = new Harmony(helper.ModContent.ModID);
            Patches.MachineDataPatches.Register();
            Patches.ItemSpawnerPatches.Register();
        }
    }
}

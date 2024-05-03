using HarmonyLib;
using StardewModdingAPI;

namespace BetterHoneyMead {
    internal sealed class ModEntry : Mod {
        public static ModEntry? Instance { get; private set; }

        public static Harmony? ModHarmony { get; private set; }

        public override void Entry(IModHelper helper) {
            Instance = this;
            ModHarmony = new Harmony(helper.ModContent.ModID);
            ModPatches.MachineData.Register();
            ModPatches.ItemSpawner.Register();
        }
    }
}

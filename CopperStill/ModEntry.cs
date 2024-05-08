using HarmonyLib;
using StardewModdingAPI;

namespace CopperStill {
    internal sealed class ModEntry : Mod {
        public static ModEntry? Instance { get; private set; }

        public Harmony? ModHarmony { get; private set; }

        public override void Entry(IModHelper helper) {
            Instance = this;
            ModHarmony = new Harmony(helper.ModContent.ModID);
            Config.Register();
            Patches.ModifyBundle.Register();
            Patches.MachineData.Register();
            Patches.ItemSpawner.Register();
            Patches.LegacyItemConverter.Register();
            Patches.ContentPatcherTokens.Register();
        }
    }
}

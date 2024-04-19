using HarmonyLib;
using StardewModdingAPI;

namespace CopperStill {
    internal sealed class ModEntry : Mod {
        public static ModEntry? Instance { get; private set; }

        public static Harmony? ModHarmony { get; private set; }

        public override void Entry(IModHelper helper) {
            Instance = this;
            ModHarmony = new Harmony(helper.ModContent.ModID);
            Config.Register();
            ModPatches.AdjustPricing.Register();
            ModPatches.ModifyBundle.Register();
            ModPatches.MachineData.Register();
            ModPatches.LookupAnything.Register();
            ModPatches.LegacyItemConverter.Register();
        }
    }
}

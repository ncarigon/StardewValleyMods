using HarmonyLib;
using StardewModdingAPI;
using MoreSensibleJuices.Patches;

namespace MoreSensibleJuices {
    internal sealed class ModEntry : Mod {
        public static ModEntry? Instance;

        public Harmony? ModHarmony { get; private set; }

        public override void Entry(IModHelper helper) {
            Instance = this;
            ModHarmony = new Harmony(helper.ModContent.ModID);
            SDVPatches.Register();
            ItemSpawnerPatches.Register();
        }
    }
}
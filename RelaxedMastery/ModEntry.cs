using HarmonyLib;
using StardewModdingAPI;
using System.Reflection;

namespace RelaxedMastery {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            new Harmony(helper.ModContent.ModID).PatchAll(Assembly.GetExecutingAssembly());
            Config.Register(this);
        }
    }
}
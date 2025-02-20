using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;

namespace UsableHayHoppers {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            if (helper is not null) {
                Config.Register(helper, this.ModManifest);
                Data.Edit(helper);
                new Harmony(helper.ModContent.ModID).PatchAll(Assembly.GetExecutingAssembly());
            }
        }
    }
}

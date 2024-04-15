using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace CopperStill.ModPatches {
    internal static class ModifyBundle {
        public static void Register(IModHelper helper, IMonitor monitor, Config config) {
            helper.Events.GameLoop.SaveLoaded += (s, e) => UpdateBundle(monitor, config?.ModifyDefaultBundle ?? false);
        }

        public static void UpdateBundle(IMonitor monitor, bool include) {
            var id = Game1.objectData
                    .Where(o => o.Value.Name.Contains("Brandy", System.StringComparison.OrdinalIgnoreCase))
                    .Select(o => ItemRegistry.GetMetadata(o.Key).QualifiedItemId).FirstOrDefault();
            var bundles = Game1.netWorldState.Value.BundleData;
            foreach (var key in bundles.Keys.ToArray()) {
                if (key == "Abandoned Joja Mart/36") {
                    var val = bundles[key];
                    if (val == "The Missing//348 1 1 807 1 0 74 1 0 454 5 2 795 1 2 445 1 0/1/5//The Missing") { // original bundle
                        if (include) {
                            monitor?.Log("Found default bundle, updating to current.", LogLevel.Info);
                            bundles[key] = val.Replace("//348 1 1 ", $"//{id} 1 1 ");
                        } else {
                            monitor?.Log("Found default bundle, nothing to do.", LogLevel.Info);
                        }
                    } else if (val == "The Missing//(O)Brandy 1 1 807 1 0 74 1 0 454 5 2 795 1 2 445 1 0/1/5//The Missing") { // older item ID
                        if (include) {
                            monitor?.Log("Found legacy bundle, updating to current.", LogLevel.Info);
                            bundles[key] = val.Replace("//(O)Brandy 1 1 ", $"//{id} 1 1 ");
                        } else {
                            monitor?.Log("Found legacy bundle, resetting to default.", LogLevel.Info);
                            bundles[key] = val.Replace("//(O)Brandy 1 1 ", $"//348 1 1 ");
                        }
                    } else if (val == "The Missing//(O)NCarigon.CopperStillJA_Brandy 1 1 807 1 0 74 1 0 454 5 2 795 1 2 445 1 0/1/5//The Missing") { // current item ID
                        if (include) {
                            monitor?.Log("Found updated bundle, nothing to do.", LogLevel.Info);
                        } else {
                            monitor?.Log("Found updated bundle, resetting to default.", LogLevel.Info);
                            bundles[key] = val.Replace("//(O)NCarigon.CopperStillJA_Brandy 1 1 ", $"//348 1 1 ");
                        }
                    }
                    if (val != bundles[key]) {
                        Game1.netWorldState.Value.SetBundleData(bundles);
                    }
                    break;
                }
            }
        }
    }
}

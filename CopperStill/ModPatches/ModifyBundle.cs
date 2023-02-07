using System.Linq;
using StardewModdingAPI;
using StardewValley;
using SpaceShared.APIs;

namespace CopperStill.ModPatches {
    internal static class ModifyBundle {
        public static void Register(IModHelper helper) {
            helper.Events.GameLoop.SaveLoaded += (s, e) => {
                var id = helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets")?.GetObjectId("Brandy") ?? -1;
                if (id > 0) {
                    var found = false;
                    var bundles = Game1.netWorldState.Value.BundleData;
                    foreach (var key in bundles.Keys.ToArray()) {
                        var val = bundles[key];
                        if (found = key == "Abandoned Joja Mart/36" && val == "The Missing//348 1 1 807 1 0 74 1 0 454 5 2 795 1 2 445 1 0/1/5") {
                            bundles[key] = val.Replace("//348 1 1 ", $"//{id} 1 1 ");
                            break;
                        }
                    }
                    if (found)
                        Game1.netWorldState.Value.SetBundleData(bundles);
                }
            };
        }
    }
}

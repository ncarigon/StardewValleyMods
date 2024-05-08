using ContentPatcher;

namespace CopperStill.Patches {
    internal static class ContentPatcherTokens {
        public static void Register() {
            if (ModEntry.Instance?.Helper is not null) {
                ModEntry.Instance.Helper.Events.GameLoop.GameLaunched += (_, _) => {
                    var cpApi = ModEntry.Instance.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
                    if (cpApi is not null) {
                        cpApi.RegisterToken(ModEntry.Instance.ModManifest,
                            "ArePricesBalanced",
                            () => new[] { AdjustPricing.ArePricesBalanced().ToString().ToLower() }
                        );
                    }
                };
                ModEntry.Instance.Helper.Events.GameLoop.SaveLoaded += (_, _) => {
                    AdjustPricing.ChangePrices();
                    ModEntry.Instance?.Helper.GameContent.InvalidateCache("Data/Machines");
                };
            }
        }
    }
}

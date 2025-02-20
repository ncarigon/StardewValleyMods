using StardewModdingAPI;

namespace UsableHayHoppers {
    internal class Config {
        public bool CanScytheHayWithoutSilo = true;
        public bool CanPickupHayHoppers = true;
        public bool SoldByMarnie = false;
        public int HayHopperCapacity = 24;

        public static Config Instance = new();

        public static void Register(IModHelper helper, IManifest manifest) {
            if (helper is not null && manifest is not null) {
                Instance = helper.ReadConfig<Config>();
                helper.Events.GameLoop.GameLaunched += (_, _) => {
                    if (helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu")) {
                        var menu = helper.ModRegistry.GetApi<GenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                        if (menu is not null) {
                            menu.Register(
                                mod: manifest,
                                reset: () => Instance = new(),
                                save: () => {
                                    helper.WriteConfig(Instance);
                                    helper.GameContent.InvalidateCache("Data/Shops");
                                    helper.GameContent.InvalidateCache("Data/Buildings");
                            });

                            menu.AddBoolOption(
                                mod: manifest,
                                getValue: () => Instance.CanScytheHayWithoutSilo,
                                setValue: value => Instance.CanScytheHayWithoutSilo = value,
                                name: () => "Can Scythe Hay Without Silo",
                                tooltip: () => "Allow players to scythe hay from grass without a silo."
                            );

                            menu.AddBoolOption(
                                mod: manifest,
                                getValue: () => Instance.CanPickupHayHoppers,
                                setValue: value => Instance.CanPickupHayHoppers = value,
                                name: () => "Can Pickup Hay Hoppers",
                                tooltip: () => "Allow players to pickup and place hay hoppers."
                            );

                            menu.AddBoolOption(
                                mod: manifest,
                                getValue: () => Instance.SoldByMarnie,
                                setValue: value => Instance.SoldByMarnie = value,
                                name: () => "Sold By Marnie",
                                tooltip: () => "If enabled, hay hoppers are sold by Marnie instead of being included with the building."
                            );

                            menu.AddNumberOption(
                                mod: manifest,
                                getValue: () => Instance.HayHopperCapacity,
                                setValue: value => Instance.HayHopperCapacity = value,
                                name: () => "Hay Hopper Capacity",
                                tooltip: () => "Set how much hay a hopper can hold.",
                                min: 0,
                                max: 100
                            );
                        }
                    }
                };
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External library")]
    public interface GenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null!, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
    }
}

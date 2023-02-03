using System;
using StardewModdingAPI;

namespace CopperStill {
    internal class ModConfig {
        public bool AdjustForBalancedPrices { get; set; } = false;

        public delegate void ModifyDelegate();
        internal ModifyDelegate OnModify = null!;

        public static ModConfig Register(IModHelper helper) {
            var config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += (s, e) => {
                var configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (configMenu is null)
                    return;

                var manifest = helper.ModRegistry.Get(helper.ModContent.ModID)!.Manifest;
                configMenu.Register(
                    mod: manifest,
                    titleScreenOnly: true,
                    reset: () => config = new ModConfig(),
                    save: () => {
                        helper.WriteConfig(config);
                        config.OnModify?.Invoke();
                    }
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Adjust for Balanced Prices",
                    tooltip: () => "Enable this when using a mod that balances crop prices.",
                    getValue: () => config.AdjustForBalancedPrices,
                    setValue: value => config.AdjustForBalancedPrices = value
                );
            };

            return config;
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}

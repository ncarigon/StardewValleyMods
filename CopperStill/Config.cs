using System;
using CopperStill.ModPatches;
using StardewModdingAPI;

namespace CopperStill {
    public class Config {
        public bool ModifyDefaultBundle { get; set; } = true;

        internal static Config Register(IModHelper helper, IMonitor monitor) {
            var config = helper.ReadConfig<Config>();

            helper.Events.GameLoop.GameLaunched += (s, e) => {
                var configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (configMenu is null)
                    return;

                var manifest = helper.ModRegistry.Get(helper.ModContent.ModID)!.Manifest;
                configMenu.Register(
                    mod: manifest,
                    reset: () => config = new Config(),
                    save: () => helper.WriteConfig(config)
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Modify Default Bundle",
                    tooltip: () => "Swap silver wine for silver brandy in one default bundle",
                    getValue: () => config.ModifyDefaultBundle,
                    setValue: value => {
                        config.ModifyDefaultBundle = value;
                        ModifyBundle.UpdateBundle(monitor, value);
                    }
                );
            };

            return config;
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}

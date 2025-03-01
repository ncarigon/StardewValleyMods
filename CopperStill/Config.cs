using System;
using CopperStill.ModPatches;
using StardewModdingAPI;

namespace CopperStill {
    public class Config {
        public bool ModifyDefaultBundle { get; set; } = true;
        //public bool ModifyPamSpecialOrder { get; set; } = true;

        public static Config? Instance { get; private set; }

        internal static void Register() {
            Instance = ModEntry.Instance?.Helper?.ReadConfig<Config>();
            if (ModEntry.Instance?.Helper is not null && Instance is not null) {
                ModEntry.Instance.Helper.Events.GameLoop.GameLaunched += (s, e) => {
                    var configMenu = ModEntry.Instance.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                    if (configMenu is null)
                        return;
                    configMenu.Register(
                        mod: ModEntry.Instance.ModManifest,
                        reset: () => Instance = new Config(),
                        save: () => ModEntry.Instance.Helper.WriteConfig(Instance)
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Modify Default Bundle",
                        tooltip: () => "Swap silver wine for silver brandy in one default bundle",
                        getValue: () => Instance.ModifyDefaultBundle,
                        setValue: value => {
                            Instance.ModifyDefaultBundle = value;
                            ModifyBundle.UpdateBundle(value);
                        }
                    );
                    //configMenu.AddBoolOption(
                    //    mod: ModEntry.Instance.ModManifest,
                    //    name: () => "Modify Pam's Special Order",
                    //    tooltip: () => "Require vodka instead of juice, with appropriate quantity.",
                    //    getValue: () => Instance.ModifyPamSpecialOrder,
                    //    setValue: value => Instance.ModifyPamSpecialOrder = value
                    //);
                };
            }
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}

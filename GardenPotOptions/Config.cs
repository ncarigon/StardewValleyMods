using StardewModdingAPI;
using StardewValley;

namespace GardenPotOptions {
    public class Config {
        public bool KeepContents { get; set; } = true;
        public bool EnableSprinklers { get; set; } = true;
        public bool AllowAncientSeeds { get; set; } = false;
        public string SafeTool { get; set; } = "Pickaxe";

        internal static Config Register(IModHelper helper) {
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
                    name: () => "Keep Contents",
                    tooltip: () => "Prevents garden pot contents from being destroyed when picked up.",
                    getValue: () => config.KeepContents,
                    setValue: value => config.KeepContents = value
                );
                configMenu.AddTextOption(
                    mod: manifest,
                    name: () => "Safe Tool",
                    tooltip: () => "Select which tool can pick up garden pots without breaking them.",
                    getValue: () => config.SafeTool,
                    setValue: value => config.SafeTool = value,
                    allowedValues: new string[] { "Pickaxe", "Axe", "Hoe" },
                    formatAllowedValue: (v) => ItemRegistry.GetMetadata($"(T){v}").CreateItem().DisplayName
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Enable Sprinklers",
                    tooltip: () => "Enable sprinklers to water garden pots.",
                    getValue: () => config.EnableSprinklers,
                    setValue: value => config.EnableSprinklers = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Allow Ancient Seeds",
                    tooltip: () => "Allow ancient seeds to be planted in garden pots.",
                    getValue: () => config.AllowAncientSeeds,
                    setValue: value => config.AllowAncientSeeds = value
                );
            };
            return config;
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null!, string fieldId = null!);

        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null!, string[] allowedValues = null!, Func<string, string> formatAllowedValue = null!, string fieldId = null!);
    }
}

using StardewModdingAPI;
using StardewValley;

namespace GardenPotOptions {
    public class Config {
        public bool KeepContents { get; set; } = true;
        public bool EnableSprinklers { get; set; } = true;
        public bool AllowAncientSeeds { get; set; } = false;
        public string SafeTool { get; set; } = "Pickaxe";
        public bool AllowTransplant { get; set; } = true;

        internal static void Register() {
            var config = ModEntry.Instance?.Helper?.ReadConfig<Config>();
            if (config is not null) {
                ModEntry.Instance!.ModConfig = config;
                ModEntry.Instance!.Helper.Events.GameLoop.GameLaunched += (s, e) => {
                    var configMenu = ModEntry.Instance!.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                    if (configMenu is null)
                        return;
                    configMenu.Register(
                        mod: ModEntry.Instance!.ModManifest,
                        reset: () => ModEntry.Instance!.ModConfig = new Config(),
                        save: () => ModEntry.Instance!.Helper.WriteConfig(ModEntry.Instance!.ModConfig ?? new Config())
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance!.ModManifest,
                        name: () => "Keep Contents",
                        tooltip: () => "Prevents garden pot contents from being destroyed when picked up.",
                        getValue: () => ModEntry.Instance!.ModConfig.KeepContents,
                        setValue: value => ModEntry.Instance!.ModConfig.KeepContents = value
                    );
                    configMenu.AddTextOption(
                        mod: ModEntry.Instance!.ModManifest,
                        name: () => "Safe Tool",
                        tooltip: () => "Select which tool can pick up garden pots without breaking them.",
                        getValue: () => ModEntry.Instance!.ModConfig.SafeTool,
                        setValue: value => ModEntry.Instance!.ModConfig.SafeTool = value,
                        allowedValues: new string[] { "Pickaxe", "Axe", "Hoe" },
                        formatAllowedValue: (v) => ItemRegistry.GetMetadata($"(T){v}").CreateItem().DisplayName
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance!.ModManifest,
                        name: () => "Enable Sprinklers",
                        tooltip: () => "Enable sprinklers to water garden pots.",
                        getValue: () => ModEntry.Instance!.ModConfig.EnableSprinklers,
                        setValue: value => ModEntry.Instance!.ModConfig.EnableSprinklers = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance!.ModManifest,
                        name: () => "Allow Ancient Seeds",
                        tooltip: () => "Allow ancient seeds to be planted in garden pots.",
                        getValue: () => ModEntry.Instance!.ModConfig.AllowAncientSeeds,
                        setValue: value => ModEntry.Instance!.ModConfig.AllowAncientSeeds = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance!.ModManifest,
                        name: () => "Allow Transplant",
                        tooltip: () => "Allow garden pots to pickup and put down crops from hoed dirt.",
                        getValue: () => ModEntry.Instance!.ModConfig.AllowTransplant,
                        setValue: value => ModEntry.Instance!.ModConfig.AllowTransplant = value
                    );
                };
            }
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null!, string fieldId = null!);

        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null!, string[] allowedValues = null!, Func<string, string> formatAllowedValue = null!, string fieldId = null!);
    }
}

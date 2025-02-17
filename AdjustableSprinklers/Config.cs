using StardewModdingAPI;

namespace AdjustableSprinklers {
    public class Config {
        public static Config Instance { get; private set; } = new();

        public int BaseRadius { get; set; } = 1;
        public int TierIncrease { get; set; } = 1;
        public bool CircularArea { get; set; } = true;
        public bool ShowSprinklerArea { get; set; } = true;
        public bool ShowScarecrowArea { get; set; } = true;
        public bool ActivateWhenClicked { get; set; } = true;
        public bool WaterGardenPots { get; set; } = true;
        public bool WaterPetBowls { get; set; } = true;
        public bool WaterSlimeHutch { get; set; } = true;

        internal static void Register(ModEntry mod) {
            Instance = mod.Helper.ReadConfig<Config>();
            mod.Helper.Events.GameLoop.GameLaunched += (s, e) => {
                var configMenu = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (configMenu is null)
                    return;
                configMenu.Register(
                    mod: mod.ModManifest,
                    reset: () => Instance = new Config(),
                    save: () => mod.Helper.WriteConfig(Instance)
                );
                configMenu.AddNumberOption(
                    mod: mod.ModManifest,
                    name: () => "Base Radius",
                    tooltip: () => "Set the starting radius for sprinkler watering. Default Game Value: 1",
                    getValue: () => Instance.BaseRadius,
                    setValue: value => Instance.BaseRadius = value,
                    min: 0,
                    max: 10
                );
                configMenu.AddNumberOption(
                    mod: mod.ModManifest,
                    name: () => "Tier Increase",
                    tooltip: () => "Set the radius increase each sprinkler upgrade applies. Default Game Value: 1",
                    getValue: () => Instance.TierIncrease,
                    setValue: value => Instance.TierIncrease = value,
                    min: 1,
                    max: 10
                );
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Circular Sprinkler Area",
                    tooltip: () => "Watered tiles by a sprinkler use a circular area instead of square.",
                    getValue: () => Instance.CircularArea,
                    setValue: value => Instance.CircularArea = value
                );
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Show Sprinkler Area",
                    tooltip: () => "Shows tiles to be watered by a sprinkler on placement.",
                    getValue: () => Instance.ShowSprinklerArea,
                    setValue: value => Instance.ShowSprinklerArea = value
                );
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Show Scarecrow Area",
                    tooltip: () => "Shows tiles to be protected by a scarecrow on placement.",
                    getValue: () => Instance.ShowScarecrowArea,
                    setValue: value => Instance.ShowScarecrowArea = value
                );
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Activate When Clicked",
                    tooltip: () => "Activates a sprinkler when you click on it.",
                    getValue: () => Instance.ActivateWhenClicked,
                    setValue: value => Instance.ActivateWhenClicked = value
                );
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Water Garden Pots",
                    tooltip: () => "Allow sprinklers to water garden pots.",
                    getValue: () => Instance.WaterGardenPots,
                    setValue: value => Instance.WaterGardenPots = value
                );
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Water Pet Bowls",
                    tooltip: () => "Allow sprinklers to water pet bowls.",
                    getValue: () => Instance.WaterPetBowls,
                    setValue: value => Instance.WaterPetBowls = value
                );
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Water Slime Hutch Troughs",
                    tooltip: () => "Allow sprinklers to water slime hutch troughs.",
                    getValue: () => Instance.WaterSlimeHutch,
                    setValue: value => Instance.WaterSlimeHutch = value
                );
            };
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null!, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
    }
}

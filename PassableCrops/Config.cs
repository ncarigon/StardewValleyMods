using System;
using System.Linq;
using StardewModdingAPI;

namespace PassableCrops {
    public class Config {
        public bool PassableCrops { get; set; } = true;
        public bool PassableScarecrows { get; set; } = true;
        public bool PassableSprinklers { get; set; } = true;
        public bool PassableForage { get; set; } = true;
        public bool PassableTeaBushes { get; set; } = true;
        public int PassableTreeGrowth { get; set; } = 3;
        public int PassableFruitTreeGrowth { get; set; } = 2;
        public bool PassableWeeds { get; set; } = true;
        public bool PassableByAll { get; set; } = false;
        public bool SlowDownWhenPassing { get; set; } = true;
        public bool ShakeWhenPassing { get; set; } = true;
        public bool PlaySoundWhenPassing { get; set; } = true;
        public bool UseCustomDrawing { get; set; } = true;
        public string[] IncludeObjects { get; set; } = new string[0];
        public string[] ExcludeObjects { get; set; } = new string[0];

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
                    name: () => "Crops",
                    tooltip: () => "Allow farmers to walk through all crops.",
                    getValue: () => config.PassableCrops,
                    setValue: value => config.PassableCrops = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Scarecrows",
                    tooltip: () => "Allow farmers to walk through scarecrows.",
                    getValue: () => config.PassableScarecrows,
                    setValue: value => config.PassableScarecrows = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Sprinklers",
                    tooltip: () => "Allow farmers to walk through sprinklers.",
                    getValue: () => config.PassableSprinklers,
                    setValue: value => config.PassableSprinklers = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Forage",
                    tooltip: () => "Allow farmers to walk through forage items.",
                    getValue: () => config.PassableForage,
                    setValue: value => config.PassableForage = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Tea Bushes",
                    tooltip: () => "Allow farmers to walk through tea bushes.",
                    getValue: () => config.PassableTeaBushes,
                    setValue: value => config.PassableTeaBushes = value
                );
                configMenu.AddNumberOption(
                    mod: manifest,
                    name: () => "Trees - Growth Stage",
                    tooltip: () => "Allow farmers to walk through tree saplings, up to the given growth stage.",
                    getValue: () => config.PassableTreeGrowth,
                    setValue: value => config.PassableTreeGrowth = value,
                    min: 0,
                    max: 5
                );
                configMenu.AddNumberOption(
                    mod: manifest,
                    name: () => "Fruit Trees - Growth Stage",
                    tooltip: () => "Allow farmers to walk through fruit tree saplings, up to the given growth stage.",
                    getValue: () => config.PassableFruitTreeGrowth,
                    setValue: value => config.PassableFruitTreeGrowth = value,
                    min: -1,
                    max: 5
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Weeds",
                    tooltip: () => "Allow farmers to walk through weeds.",
                    getValue: () => config.PassableWeeds,
                    setValue: value => config.PassableWeeds = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Passable by all",
                    tooltip: () => "Makes selected obstacles passable to all entities, not just farmers. This notably effects monsters walking through weeds.",
                    getValue: () => config.PassableByAll,
                    setValue: value => config.PassableByAll = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Slow down when passing",
                    tooltip: () => "Farmers will walk slightly slower through objects, just like in tall grass.",
                    getValue: () => config.SlowDownWhenPassing,
                    setValue: value => config.SlowDownWhenPassing = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Shake when passing",
                    tooltip: () => "Makes non-crop objects shake when passing by (crops automatically shake).",
                    getValue: () => config.ShakeWhenPassing,
                    setValue: value => config.ShakeWhenPassing = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Also make rustling sound",
                    tooltip: () => "Passing through objects also makes the rustling sound.",
                    getValue: () => config.PlaySoundWhenPassing,
                    setValue: value => config.PlaySoundWhenPassing = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => "Use custom object drawing",
                    tooltip: () => "Some objects require custom drawing logic in order to shake and calculate the correct layer depth. This logic may be incompatible with other mods. This option disables the custom drawing and may prevent errors that could arise. It is not recommended to disable custom drawing if no issues are present.",
                    getValue: () => config.UseCustomDrawing,
                    setValue: value => config.UseCustomDrawing = value
                );
                configMenu.AddTextOption(
                    mod: manifest,
                    name: () => "Include Names",
                    tooltip: () => "Specify individual objects that are passable, using either base name or qualified item ID, such as Twig or (O)294. Comma-separated.",
                    getValue: () => string.Join(", ", config.IncludeObjects),
                    setValue: value => config.IncludeObjects = value.Split(',').Select(v => v.Trim()).ToArray()
                );
                configMenu.AddTextOption(
                    mod: manifest,
                    name: () => "Exclude Names",
                    tooltip: () => "Specify individual objects that are NOT passable, using either base name or qualified item ID, such as Twig or (O)294. Comma-separated.",
                    getValue: () => string.Join(", ", config.ExcludeObjects),
                    setValue: value => config.ExcludeObjects = value.Split(',').Select(v => v.Trim()).ToArray()
                );
            };
            return config;
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null!, string fieldId = null!);

        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null!, int? min = null, int? max = null, int? interval = null, Func<int, string> formatValue = null!, string fieldId = null!);

        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
    }
}

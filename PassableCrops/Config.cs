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
        public string[] IncludeObjects { get; set; } = Array.Empty<string>();
        public string[] ExcludeObjects { get; set; } = Array.Empty<string>();

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
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Crops") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Crops") ?? "null",
                    getValue: () => config.PassableCrops,
                    setValue: value => config.PassableCrops = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Scarecrows") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Scarecrows") ?? "null",
                    getValue: () => config.PassableScarecrows,
                    setValue: value => config.PassableScarecrows = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Sprinklers") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Sprinklers") ?? "null",
                    getValue: () => config.PassableSprinklers,
                    setValue: value => config.PassableSprinklers = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Forage") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Forage") ?? "null",
                    getValue: () => config.PassableForage,
                    setValue: value => config.PassableForage = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Tea") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Tea") ?? "null",
                    getValue: () => config.PassableTeaBushes,
                    setValue: value => config.PassableTeaBushes = value
                );
                configMenu.AddNumberOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_TreeGrowth") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_TreeGrowth") ?? "null",
                    getValue: () => config.PassableTreeGrowth,
                    setValue: value => config.PassableTreeGrowth = value,
                    min: 0,
                    max: 5
                );
                configMenu.AddNumberOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_FruitGrowth") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_FruitGrowth") ?? "null",
                    getValue: () => config.PassableFruitTreeGrowth,
                    setValue: value => config.PassableFruitTreeGrowth = value,
                    min: -1,
                    max: 5
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Weeds") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Weeds") ?? "null",
                    getValue: () => config.PassableWeeds,
                    setValue: value => config.PassableWeeds = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_AllPass") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_AllPass") ?? "null",
                    getValue: () => config.PassableByAll,
                    setValue: value => config.PassableByAll = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_SlowDown") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_SlowDown") ?? "null",
                    getValue: () => config.SlowDownWhenPassing,
                    setValue: value => config.SlowDownWhenPassing = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Shake") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Shake") ?? "null",
                    getValue: () => config.ShakeWhenPassing,
                    setValue: value => config.ShakeWhenPassing = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Rustle") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Rustle") ?? "null",
                    getValue: () => config.PlaySoundWhenPassing,
                    setValue: value => config.PlaySoundWhenPassing = value
                );
                configMenu.AddBoolOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_CustomDraw") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_CustomDraw") ?? "null",
                    getValue: () => config.UseCustomDrawing,
                    setValue: value => config.UseCustomDrawing = value
                );
                configMenu.AddTextOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Include") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Include") ?? "null",
                    getValue: () => string.Join(", ", config.IncludeObjects),
                    setValue: value => config.IncludeObjects = value.Split(',').Select(v => v.Trim()).ToArray()
                );
                configMenu.AddTextOption(
                    mod: manifest,
                    name: () => helper.Translation.Get($"{manifest?.UniqueID}/config_name_Exclude") ?? "null",
                    tooltip: () => helper.Translation.Get($"{manifest?.UniqueID}/config_desc_Exclude") ?? "null",
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

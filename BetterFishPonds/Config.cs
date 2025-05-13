using StardewModdingAPI;

namespace BetterFishPonds {
    public class Config {
        public bool AllowSpawnedFishAsProduce = true;
        public bool AllowQualityIncrease = true;
        public bool DontRemoveExistingProduce = true;

        public bool AllowQualitySpawn = true;
        public bool AllowQualityRoe = true;
        public bool AllowQualityGems = false;
        public bool AllowQualityMinerals = false;
        public bool AllowQualityTreasure = false;
        public bool AllowQualityBeachForage = true;
        public bool AllowQualityCrops = true;
        public bool AllowQualityArtisanGoods = true;
        public bool AllowQualityAnimalProducts = true;
        public bool AllowQualityCooking = true;
        public bool AllowQualityOther = false;

        public int PerFishChanceToIncreaseQuality = 5;
        public int PerSeasonChanceToIncreaseQuality = 10;
        public int PerLevelChanceToIncreaseQuality = 10;

        public string[] IncludeItemNames = new string[] {
            "Treasure Chest", "Pearl", "Golden Pumpkin", // treasure
            "Coral", "Sea Urchin", "Nautilus Shell", "Rainbow Shell" // beach forage
        };
        public string[] ExcludeItemNames = new string[] {
            "Sap" // crops
        };

        public static Config Instance { get; private set; } = new();

        internal static void Register() {
            if (ModEntry.Instance?.Helper is not null) {
                Instance = ModEntry.Instance.Helper.ReadConfig<Config>();
                ModEntry.Instance.Helper.Events.GameLoop.GameLaunched += (s, e) => {
                    var configMenu = ModEntry.Instance.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                    if (configMenu is null)
                        return;
                    configMenu.Register(
                        mod: ModEntry.Instance.ModManifest,
                        reset: () => Instance = new Config(),
                        save: () => ModEntry.Instance.Helper.WriteConfig(Instance)
                    );

                    configMenu.AddSectionTitle(
                        mod: ModEntry.Instance.ModManifest,
                        text: () => "General",
                        tooltip: () => "Toggle overall mod abilities"
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Increase Quality",
                        tooltip: () => "Fish and produce from ponds can have increased quality.",
                        getValue: () => Instance.AllowQualityIncrease,
                        setValue: value => Instance.AllowQualityIncrease = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Spawn Fish As Produce",
                        tooltip: () => "When at maximum capacity and no produce has been made, spawned fish become produce.",
                        getValue: () => Instance.AllowSpawnedFishAsProduce,
                        setValue: value => Instance.AllowSpawnedFishAsProduce = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Don't Clear Existing Produce",
                        tooltip: () => "If no new item is produced, do not remove the existing item.",
                        getValue: () => Instance.DontRemoveExistingProduce,
                        setValue: value => Instance.DontRemoveExistingProduce = value
                    );

                    configMenu.AddSectionTitle(
                        mod: ModEntry.Instance.ModManifest,
                        text: () => "Quality Increase Chance",
                        tooltip: () => "Adjust specific chances for a quality increase from available sources. Each fish, season, and level is multiplicative, not additive."
                    );
                    configMenu.AddNumberOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Per Fish",
                        tooltip: () => "% chance for each fish in the pond it increase a produced item's quality. Normal 0%; Silver 1x%; Gold 2x%; Iridium 4x%",
                        getValue: () => Instance.PerFishChanceToIncreaseQuality,
                        setValue: value => Instance.PerFishChanceToIncreaseQuality = value,
                        min: 0,
                        max: 25,
                        interval: 1
                    );
                    configMenu.AddNumberOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Per Season",
                        tooltip: () => "% chance for each season the pond has been in continuous use to increase a produced item's quality.",
                        getValue: () => Instance.PerSeasonChanceToIncreaseQuality,
                        setValue: value => Instance.PerSeasonChanceToIncreaseQuality = value,
                        min: 0,
                        max: 25,
                        interval: 1
                    );
                    configMenu.AddNumberOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Per Level",
                        tooltip: () => "% chance for each fishing skill level to increase a produced item's quality.",
                        getValue: () => Instance.PerLevelChanceToIncreaseQuality,
                        setValue: value => Instance.PerLevelChanceToIncreaseQuality = value,
                        min: 0,
                        max: 25,
                        interval: 1
                    );

                    configMenu.AddSectionTitle(
                        mod: ModEntry.Instance.ModManifest,
                        text: () => "Quality Categories",
                        tooltip: () => "Toggle general categories of items that can gain quality when produced in a fish pond."
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Spawned Fish",
                        tooltip: () => "Fish spawned in fish ponds can gain quality.",
                        getValue: () => Instance.AllowQualitySpawn,
                        setValue: value => Instance.AllowQualitySpawn = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Roe",
                        tooltip: () => "Roe produced in fish ponds can gain quality.",
                        getValue: () => Instance.AllowQualityRoe,
                        setValue: value => Instance.AllowQualityRoe = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Gems",
                        tooltip: () => "Gems produced in fish ponds can gain quality. Includes category(-2) items.",
                        getValue: () => Instance.AllowQualityGems,
                        setValue: value => Instance.AllowQualityGems = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Minerals",
                        tooltip: () => "Minerals produced in fish ponds can gain quality. Includes category(-12) items.",
                        getValue: () => Instance.AllowQualityMinerals,
                        setValue: value => Instance.AllowQualityMinerals = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Treasure",
                        tooltip: () => "Treasure produced in fish ponds can gain quality. Includes specific(0) items.",
                        getValue: () => Instance.AllowQualityTreasure,
                        setValue: value => Instance.AllowQualityTreasure = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Beach Forage",
                        tooltip: () => "Beach forage produced in fish ponds can gain quality. Includes specific(-23) items.",
                        getValue: () => Instance.AllowQualityBeachForage,
                        setValue: value => Instance.AllowQualityBeachForage = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Crops",
                        tooltip: () => "Crops produced in fish ponds can gain quality. Includes vegetables(-75), fruits(-79), flowers(-80), and forage(-81).",
                        getValue: () => Instance.AllowQualityCrops,
                        setValue: value => Instance.AllowQualityCrops = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Artisan Goods",
                        tooltip: () => "Artisan Goods produced in fish ponds can gain quality. Includes goods(-26) and syrups(-27).",
                        getValue: () => Instance.AllowQualityArtisanGoods,
                        setValue: value => Instance.AllowQualityArtisanGoods = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Animal Products",
                        tooltip: () => "Animal Products produced in fish ponds can gain quality. Includes milk(-4), eggs(-5), fish(-6), and meat(-14).",
                        getValue: () => Instance.AllowQualityAnimalProducts,
                        setValue: value => Instance.AllowQualityAnimalProducts = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Cooking",
                        tooltip: () => "Cooking produced in fish ponds can gain quality. Includes category(-7) items.",
                        getValue: () => Instance.AllowQualityCooking,
                        setValue: value => Instance.AllowQualityCooking = value
                    );
                    configMenu.AddBoolOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Other",
                        tooltip: () => "Other items produced in fish ponds can gain quality.",
                        getValue: () => Instance.AllowQualityOther,
                        setValue: value => Instance.AllowQualityOther = value
                    );

                    configMenu.AddSectionTitle(
                        mod: ModEntry.Instance.ModManifest,
                        text: () => "Item Exceptions",
                        tooltip: () => "Specific items to include or exclude. Use internal item names only."
                    );
                    configMenu.AddTextOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Include Names",
                        tooltip: () => "Applies to Treasure, Beach Forage, and Other categories.",
                        getValue: () => string.Join(", ", Instance.IncludeItemNames),
                        setValue: value => Instance.IncludeItemNames = value.Split(',').Select(v => v.Trim()).ToArray()
                    );
                    configMenu.AddTextOption(
                        mod: ModEntry.Instance.ModManifest,
                        name: () => "Exclude Names",
                        tooltip: () => "Applies to Spawned Fish, Roe, Gem, Mineral, Crop, Artisan Good, Animal Product, and Cooking categories.",
                        getValue: () => string.Join(", ", Instance.ExcludeItemNames),
                        setValue: value => Instance.ExcludeItemNames = value.Split(',').Select(v => v.Trim()).ToArray()
                    );
                };
            }
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);

        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
    }
}

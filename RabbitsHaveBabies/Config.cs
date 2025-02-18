using StardewModdingAPI;

namespace RabbitsHaveBabies {
    internal class Config {
        public bool AllowCoopPregnancy, AllowBigCoopPregnancy, AllowDeluxCoopPregnancy = true, AllowRabbitPregnancy = true;
        public float ExtraChanceToReproduce = 2.0f;

        public static Config Instance = new();

        public static void Register() {
            if (ModEntry.Instance?.Helper is not null && ModEntry.Instance.ModManifest is not null) {
                Instance = ModEntry.Instance.Helper.ReadConfig<Config>();
                ModEntry.Instance.Helper.Events.GameLoop.GameLaunched += (_, _) => {
                    if (ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu")) {
                        var api = ModEntry.Instance.Helper.ModRegistry.GetApi<GenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                        if (api is not null) {
                            api.Register(
                                mod: ModEntry.Instance.ModManifest,
                                reset: () => Instance = new(),
                                save: () => {
                                    ModEntry.Instance.Helper.WriteConfig(Instance);
                                    ModEntry.Instance.Helper.GameContent.InvalidateCache("Data/Buildings");
                                    ModEntry.Instance.Helper.GameContent.InvalidateCache("Data/FarmAnimals");
                                }); ;

                            api.AddBoolOption(
                                mod: ModEntry.Instance.ModManifest,
                                getValue: () => Instance.AllowRabbitPregnancy,
                                setValue: value => Instance.AllowRabbitPregnancy = value,
                                name: () => "Allow Rabbit Pregnancy",
                                tooltip: () => "Allows rabbits to reproduce. Required for rabbits in the base game."
                            );

                            api.AddBoolOption(
                                mod: ModEntry.Instance.ModManifest,
                                getValue: () => Instance.AllowCoopPregnancy,
                                setValue: value => Instance.AllowCoopPregnancy = value,
                                name: () => "Allow Coop Pregnancy",
                                tooltip: () => "Allows Coop animals to reproduce. Does not interact with rabbits in the base game, but may be useful for compatibility with other mods."
                            );

                            api.AddBoolOption(
                                mod: ModEntry.Instance.ModManifest,
                                getValue: () => Instance.AllowBigCoopPregnancy,
                                setValue: value => Instance.AllowBigCoopPregnancy = value,
                                name: () => "Allow Big Coop Pregnancy",
                                tooltip: () => "Allows Big Coop animals to reproduce. Does not interact with rabbits in the base game, but may be useful for compatibility with other mods."
                            );

                            api.AddBoolOption(
                                mod: ModEntry.Instance.ModManifest,
                                getValue: () => Instance.AllowDeluxCoopPregnancy,
                                setValue: value => Instance.AllowDeluxCoopPregnancy = value,
                                name: () => "Allow Deluxe Coop Pregnancy",
                                tooltip: () => "Allows Deluxe Coop animals to reproduce. Required for rabbits in the base game."
                            );

                            api.AddNumberOption(
                                mod: ModEntry.Instance.ModManifest,
                                getValue: () => Instance.ExtraChanceToReproduce,
                                setValue: value => Instance.ExtraChanceToReproduce = value,
                                name: () => "Rabbit Fertility",
                                tooltip: () => "Rabbits can reproduce faster than other animals. This setting allows an extra attempt, specifically for rabbits, as a multiplier of the base game chance (0.55% per animal). Requires 2 or more rabbits in the same coop which are able to reproduce.",
                                min: 0.0f,
                                max: 10.0f,
                                interval: 0.5f
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

        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
    }
}

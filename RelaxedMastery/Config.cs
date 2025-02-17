using StardewModdingAPI;

namespace RelaxedMastery {
    public class Config {
        public static Config Instance { get; private set; } = new();

        public bool MasteredSkillsGainExp = true;

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
                configMenu.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => "Mastered Skills Gain XP",
                    tooltip: () => "Determines whether mastered skills continue to gain mastery experience.",
                    getValue: () => Instance.MasteredSkillsGainExp,
                    setValue: value => Instance.MasteredSkillsGainExp = value
                );
            };
        }
    }

    public interface IGenericModConfigMenuApi {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}

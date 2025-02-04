using StardewModdingAPI.Events;

namespace GardenPotOptions {
    internal static class Events {
        public static void Register() {
            if (ModEntry.Instance is not null) {
                ModEntry.Instance.Helper.Events.Content.AssetRequested += (_, e)
                    => Content_AssetRequested(e);
            }
        }

        public static void Content_AssetRequested(AssetRequestedEventArgs e) {
            if (e.NameWithoutLocale.Name.Equals("Data/Mail", StringComparison.InvariantCultureIgnoreCase)) {
                e.Edit(a => {
                    var d = a.AsDictionary<string, string>().Data;
                    d[$"{ModEntry.Instance?.ModHarmony?.Id}_Pot"] = $"{ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/mail_text_pot") ?? "null"}%item id (BC)62 %%[#]{ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/mail_title_pot") ?? "null"}";
                    d[$"{ModEntry.Instance?.ModHarmony?.Id}_Recipe"] = $"{ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/mail_text_recipe") ?? "null"}%item craftingRecipe Garden_Pot %%[#]{ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/mail_title_recipe") ?? "null"}";
                });
            } else if (e.NameWithoutLocale.Name.Equals("Data/Events/Farm", StringComparison.InvariantCultureIgnoreCase)) {
                e.Edit(a => {
                    var d = a.AsDictionary<string, string>().Data;
                    // make sure the base game event exists before we try to modify it
                    if (d.ContainsKey("900553/t 600 1130/Hn ccPantry/A cc_Greenhouse/w sunny")) {
                        var hp = ModEntry.Instance?.ModConfig?.HeartsForGardenPot ?? -1;
                        var hr = ModEntry.Instance?.ModConfig?.HeartsForRecipe ?? -1;
                        if (hp > -1) {
                            d[$"{ModEntry.Instance?.ModHarmony?.Id}_Pot_Send/f Evelyn {hp * 250}/k 900553/sendmail {ModEntry.Instance?.ModHarmony?.Id}_Pot"] = "null";
                            if (hr >= hp) {
                                d[$"{ModEntry.Instance?.ModHarmony?.Id}_Recipe_Send/f Evelyn {hr * 250}/k 900553/e {ModEntry.Instance?.ModHarmony?.Id}_Pot_Send/sendmail {ModEntry.Instance?.ModHarmony?.Id}_Recipe"] = "null";
                            }
                            // add new default with requirement
                            d[$"900553/t 600 1130/Hn ccPantry/A cc_Greenhouse/w sunny/k {ModEntry.Instance?.ModHarmony?.Id}_Pot_Send"] = d["900553/t 600 1130/Hn ccPantry/A cc_Greenhouse/w sunny"];
                            // add only recipe event
                            d[$"900553/t 600 1130/Hn ccPantry/A cc_Greenhouse/w sunny/e {ModEntry.Instance?.ModHarmony?.Id}_Pot_Send/k {ModEntry.Instance?.ModHarmony?.Id}_Recipe_Send"] = ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/event_text_recipe") ?? "null";
                            // remove default
                            d.Remove("900553/t 600 1130/Hn ccPantry/A cc_Greenhouse/w sunny");
                        }
                    }
                });
            //} else if (e.NameWithoutLocale.Name.Equals("Strings/Objects", StringComparison.InvariantCultureIgnoreCase)) {
            }
        }
    }
}

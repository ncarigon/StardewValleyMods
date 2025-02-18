using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;

namespace RabbitsHaveBabies {
    internal static class Data {
        public static void Edit() {
            if (ModEntry.Instance?.Helper is not null) {
                ModEntry.Instance.Helper.Events.Content.AssetRequested += (_, args) => {
                    if (args.NameWithoutLocale.IsEquivalentTo("Data/Buildings")) {
                        args.Edit(asset => {
                            var buildings = asset.AsDictionary<string, BuildingData>().Data;
                            buildings["Coop"].AllowAnimalPregnancy = Config.Instance.AllowCoopPregnancy;
                            buildings["Big Coop"].AllowAnimalPregnancy = Config.Instance.AllowBigCoopPregnancy;
                            buildings["Deluxe Coop"].AllowAnimalPregnancy = Config.Instance.AllowDeluxCoopPregnancy;
                        });
                    } else if (args.NameWithoutLocale.IsEquivalentTo("Data/FarmAnimals")) {
                        args.Edit(asset => {
                            asset.AsDictionary<string, FarmAnimalData>().Data["Rabbit"].CanGetPregnant = Config.Instance.AllowRabbitPregnancy;
                        });
                    }
                };
            }
        }
    }
}

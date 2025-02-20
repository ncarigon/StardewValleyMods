using StardewModdingAPI;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Buildings;

namespace UsableHayHoppers {
    internal static class Data {
        public static void Edit(IModHelper helper) {
            if (helper is not null) {
                helper.Events.Content.AssetRequested += (_, args) => {
                    if (args.NameWithoutLocale.IsEquivalentTo("Data/Shops")) {
                        args.Edit(asset => {
                            if (Config.Instance.SoldByMarnie) {
                                // add hay hopper to marnie's shop data
                                var items = asset.AsDictionary<string, ShopData>().Data["AnimalShop"].Items;
                                var heater = items.FirstOrDefault(i => i.Id == "(BC)104");
                                if (heater is not null) {
                                    var index = items.IndexOf(heater);
                                    var price = heater.Price / 10;
                                    items.Insert(index, new ShopItemData() {
                                        TradeItemAmount = 1,
                                        Price = price,
                                        Id = "(BC)99",
                                        ItemId = "(BC)99"
                                    });
                                }
                            }
                        });
                    } else if (args.NameWithoutLocale.IsEquivalentTo("Data/Buildings")) {
                        args.Edit(asset => {
                            // make hay hoppers from premade building data pickupable or not
                            foreach (var entry in asset.AsDictionary<string, BuildingData>().Data.Values.Where(b => b?.IndoorItems?.Count > 0)) {
                                foreach (var item in entry.IndoorItems.Where(i => i?.ItemId?.Equals("(BC)99") == true)) {
                                    item.Indestructible = !Config.Instance.CanPickupHayHoppers;
                                }
                            }
                            if (Config.Instance.SoldByMarnie) {
                                // remove hay hoppers from premade building data
                                foreach (var entry in asset.AsDictionary<string, BuildingData>().Data.Values.Where(b => b?.IndoorItems?.Count > 0)) {
                                    entry.IndoorItems.RemoveAll(i => i?.ItemId?.Equals("(BC)99") == true);
                                }
                            }
                        });
                    }
                };
            }
        }
    }
}

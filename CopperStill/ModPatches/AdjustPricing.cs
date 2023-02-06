using StardewModdingAPI;
using ProducerFrameworkMod.Controllers;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace CopperStill.ModPatches {
    internal static class AdjustPricing {
        public static void Register(IModHelper helper) {
            helper.Events.GameLoop.SaveLoaded += (s, e) => SetMultiplier();
        }

        private static void SetMultiplier() {
            var prices = new Dictionary<string, int>();
            var numDefault = 0;
            foreach (var item in Game1.objectInformation) {
                var parts = item.Value.Split('/');
                switch (parts[0]) {
                    // these are crops we need to reference later for direct price changes
                    case "Cactus Fruit":
                        if (parts[1] == "75")
                            numDefault++;
                        if (int.TryParse(parts[1], out var price))
                            prices[parts[0]] = price;
                        break;
                    case "Corn":
                        if (parts[1] == "50")
                            numDefault++;
                        if (int.TryParse(parts[1], out price))
                            prices[parts[0]] = price;
                        break;
                    case "Blackberry":
                        if (int.TryParse(parts[1], out price))
                            prices[parts[0]] = price;
                        break;
                    case "Wine":
                        if (int.TryParse(parts[1], out price))
                            prices[parts[0]] = price;
                        break;
                    case "Juice":
                        if (int.TryParse(parts[1], out price))
                            prices[parts[0]] = price;
                        break;
                    // the rest are crops with unbalanced prices we can assume would be modified
                    case "Starfruit":
                        if (parts[1] == "750")
                            numDefault++;
                        break;
                    case "Ancient Fruit":
                        if (parts[1] == "550")
                            numDefault++;
                        break;
                    case "Pineapple":
                        if (parts[1] == "300")
                            numDefault++;
                        break;
                    case "Pumpkin":
                        if (parts[1] == "320")
                            numDefault++;
                        break;
                    case "Coffee Bean":
                        if (parts[1] == "15")
                            numDefault++;
                        break;
                }
            }
            // only detect "balanced" prices if the majority of unbalanced crops are no longer default values.
            var isBalanced = numDefault < 4;
            // adjust some internal item spawn prices, just to keep things consistent
            foreach (var key in Game1.objectInformation.Keys.ToArray()) {
                var parts = Game1.objectInformation[key].Split('/');
                switch (parts[0]) {
                    case "Juniper Berry":
                        parts[1] = (prices["Blackberry"] + 10).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                    case "Tequila Blanco":
                        parts[1] = prices["Cactus Fruit"].Multi(3).Multi(4.5).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                    case "Tequila Añejo":
                        parts[1] = prices["Cactus Fruit"].Multi(3).Multi(4.5).Multi(3).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                    case "Moonshine":
                        parts[1] = prices["Corn"].Multi(2.25).Multi(isBalanced ? 4.5 : 15).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                    case "Whiskey":
                        parts[1] = prices["Corn"].Multi(2.25).Multi(isBalanced ? 4.5 : 15).Multi(3).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                    case "Vodka":
                        parts[1] = prices["Juice"].Multi(isBalanced ? 4.5 : 12).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                    case "Gin":
                        parts[1] = prices["Juice"].Multi(isBalanced ? 4.5 : 12).Multi(2).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                    case "Brandy":
                        parts[1] = prices["Wine"].Multi(4.5).ToString();
                        Game1.objectInformation[key] = string.Join('/', parts);
                        break;
                }
            }

            foreach (var rule in ProducerController.GetProducerRules("Still")) {
                if (rule.OutputIdentifier == "Vodka") {
                    rule.OutputPriceMultiplier = isBalanced ? 4.5 : 12;
                    foreach (var add in rule.AdditionalOutputs) {
                        if (add.OutputIdentifier == "Moonshine")
                            add.OutputPriceMultiplier = isBalanced ? 4.5 : 15;
                    }
                    ProducerController.AddProducerItems(rule, null, "NCarigon.CopperStillPFM");
                }
            }
        }

        private static int Multi(this int i, double d) => (int)(i * d);
    }
}

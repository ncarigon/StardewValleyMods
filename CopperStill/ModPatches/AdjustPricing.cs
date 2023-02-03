using StardewModdingAPI;
using ProducerFrameworkMod.Controllers;

namespace CopperStill.ModPatches {
    internal static class AdjustPricing {
        public static void Register(IModHelper helper, ModConfig config) {
            helper.Events.GameLoop.SaveLoaded += (s, e) => SetMultiplier(config.AdjustForBalancedPrices);
            config.OnModify += () => SetMultiplier(config.AdjustForBalancedPrices);
        }

        private static void SetMultiplier(bool isBalanced) {
            foreach (var rule in ProducerController.GetProducerRules("Still")) {
                if (rule.OutputIdentifier == "Vodka") {
                    rule.OutputPriceMultiplier = isBalanced ? 4.5 : 12;
                    foreach (var add in rule.AdditionalOutputs) {
                        if (add.OutputIdentifier == "Moonshine")
                            add.OutputPriceMultiplier = isBalanced ? 4.5 : 15;
                    }
                }
                ProducerController.AddProducerItems(rule, null, "NCarigon.CopperStillPFM");
            }

            /* TODO: dynamically update generic prices when changing balance mode
             * 
                "{{spacechase0.jsonAssets/ObjectId:Juniper Berry}}": {1:"0"},
				"{{spacechase0.jsonAssets/ObjectId:Tequila Blanco}}": {1:"0"},
				"{{spacechase0.jsonAssets/ObjectId:Tequila Añejo}}": {1:"0"},
				"{{spacechase0.jsonAssets/ObjectId:Moonshine}}": {1:"0"},
				"{{spacechase0.jsonAssets/ObjectId:Whiskey}}": {1:"0"},
				"{{spacechase0.jsonAssets/ObjectId:Vodka}}": {1:"0"},
				"{{spacechase0.jsonAssets/ObjectId:Gin}}": {1:"0"},
				"{{spacechase0.jsonAssets/ObjectId:Brandy}}": {1:"0"},
             */
        }
    }
}

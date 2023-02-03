using System;
using Pathoschild.Stardew.Automate;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace GardenPotAutomate {
    /// <summary>
    /// A general recipe for anything that is plantable
    /// </summary>
    internal class SeedRecipe : IRecipe {
        public Func<Item, bool> Input { get; }
        public int InputCount { get; } = 1;
        public Func<Item, Item> Output { get; } = _ => new SObject();
        public Func<Item, int> Minutes { get; } = _ => 0;

        private readonly IndoorPot IndoorPot;
        private readonly GameLocation Location;
        private readonly ModConfig Config;

        public SeedRecipe(IndoorPot indoorPot, GameLocation location, ModConfig config) {
            this.IndoorPot = indoorPot;
            this.Config = config;
            this.Location = location;
            this.Input = this.TryPlant;
        }

        public bool AcceptsInput(ITrackedStack stack) {
            return stack.Type == ItemType.Object && Input(stack.Sample);
        }

        public bool TryPlant(Item item) => TryPlant(this.IndoorPot, item, this.Location, this.Config);

        public static bool TryPlant(IndoorPot indoorPot, Item item, GameLocation location, ModConfig config) {
            return
                // are we enabled?
                config.Enabled
                // can we plant seeds and is it a seed?
                && ((config.PlantSeeds && (item?.Category.Equals(SObject.SeedsCategory) ?? false))
                // can we apply fertilizer and is it a fertilizer?
                || (config.ApplyFertilizers && (item?.Category.Equals(SObject.fertilizerCategory) ?? false)))
                // actually try to place the item
                && (indoorPot?.performObjectDropInAction(item, false, new Farmer() { currentLocation = location }) ?? false);
        }
    }
}
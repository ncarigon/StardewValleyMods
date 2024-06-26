﻿using System;
using Pathoschild.Stardew.Automate;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace GardenPotAutomate {
    internal class SeedRecipe : IRecipe {
        public Func<Item, bool> Input { get; }
        public int InputCount { get; } = 1;
        public Func<Item, Item> Output { get; } = _ => new SObject();
        public Func<Item, int> Minutes { get; } = _ => 0;

        private readonly IndoorPot IndoorPot;
        private readonly Config Config;
        private readonly Farmer TempFarmer;

        public SeedRecipe(IndoorPot indoorPot, Farmer tempFarmer, Config config) {
            this.IndoorPot = indoorPot;
            this.Config = config;
            this.Input = this.TryApply;
            this.TempFarmer = tempFarmer;
        }

        public bool AcceptsInput(ITrackedStack stack) {
            return stack.Type == ItemRegistry.type_object && Input(stack.Sample);
        }

        public bool TryApply(Item item) {
            return
                // are we enabled?
                this.Config.Enabled
                // can we plant seeds?
                && this.Config.PlantSeeds
                // is it a seed?
                && (item?.Category.Equals(SObject.SeedsCategory) ?? false)
                // actually try to place the item
                && (this.IndoorPot?.performObjectDropInAction(item, false, this.TempFarmer) ?? false);
        }
    }
}
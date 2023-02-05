using StardewValley;

namespace GardenPotAutomate {
    /// <summary>
    /// Used to track the first drop from a harvest.
    /// </summary>
    internal class HarvestTracker {
        public Item Item;
        public bool FirstDrop;

        public HarvestTracker(Item item, bool firstDrop = false) {
            Item = item;
            FirstDrop = firstDrop;
        }
    }
}
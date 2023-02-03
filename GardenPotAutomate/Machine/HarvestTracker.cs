using SObject = StardewValley.Object;

namespace GardenPotAutomate {
    /// <summary>
    /// Used to track the first drop from a harvest.
    /// </summary>
    internal class HarvestTracker {
        public SObject Item;
        public bool FirstDrop;

        public HarvestTracker(SObject item, bool firstDrop = false) {
            Item = item;
            FirstDrop = firstDrop;
        }
    }
}
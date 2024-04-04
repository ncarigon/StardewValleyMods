using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Linq;

namespace BushBloomMod {
    public class Api {
#pragma warning disable CA1822 // Mark members as static
        /// <summary>
        /// Returns an array of (item_id, first_day, last_day) for all possible active blooming
        /// schedules on the given season and day, optionally within the given year and/or location.
        /// </summary>
        public (string, WorldDate, WorldDate)[] GetActiveSchedules(
            string season, int dayofMonth, int? year = null,
            GameLocation location = null, Vector2? tile = null
        ) =>
            Schedule.GetAllCandidates(year ?? Game1.year, Helpers.GetDayOfYear(season, dayofMonth), true, false, location, tile)
                .Where(c => c.ShakeOffId != null)
                .Select(c => (c.ShakeOffId, c.Entry.FirstDay, c.Entry.LastDay))
                .ToArray();

        /// <summary>
        /// Returns an array of (item_id, first_day, last_day) for all blooming schedules.
        /// </summary>
        public (string, WorldDate, WorldDate)[] GetAllSchedules() =>
            Schedule.Entries
                .Where(c => c.ShakeOffId != null)
                .Select(c => (c.ShakeOffId, c.Entry.FirstDay, c.Entry.LastDay))
                .ToArray();

        /// <summary>
        /// Clear and reparse all schedules.
        /// </summary>
        public void ReloadSchedules() => Schedule.ReloadEntries();

        /// <summary>
        /// Specifies whether BBM successfully parsed all schedules.
        /// </summary>
        public bool IsReady() => Schedule.IsReady;

        /// <summary>
        /// Performs the general operations of the Bush.shake() function without all the player, debris,
        /// and UI logic. Namely, this will return an item ID if the bush is in bloom and mark the bush
        /// as no longer blooming. You must create the item and handle any operations needed for it.
        /// </summary>
        public string FakeShake(Bush bush) {
            if (bush.IsAbleToBloom()) {
                // overwrite with our item shake logic
                var item = bush.GetShakeOffId();
                // disable blooming
                bush.tileSheetOffset.Value = 0;
                // clear schedule after shaking
                Schedule.SetSchedule(bush, null);
                return item;
            }
            return null;
        }
#pragma warning restore CA1822 // Mark members as static
    }
}

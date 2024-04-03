using Microsoft.Xna.Framework;
using StardewValley;
using System.Linq;

namespace BushBloomMod {
    public class Api {
#pragma warning disable CA1822 // Mark members as static
        public (string, WorldDate, WorldDate)[] GetActiveSchedules(string season, int dayofMonth, int? year = null, GameLocation location = null, Vector2? tile = null) =>
            Schedule.GetAllCandidates(year ?? Game1.year, Helpers.GetDayOfYear(season, dayofMonth), true, false, location, tile)
                .Where(c => c.ShakeOffId != null)
                .Select(c => (c.ShakeOffId, c.Entry.FirstDay, c.Entry.LastDay))
                .ToArray();

        public (string, WorldDate, WorldDate)[] GetAllSchedules() =>
            Schedule.Entries
                .Where(c => c.ShakeOffId != null)
                .Select(c => (c.ShakeOffId, c.Entry.FirstDay, c.Entry.LastDay))
                .ToArray();

        public void ReloadSchedules() => Schedule.ReloadEntries();

        public bool IsReady() => Schedule.IsReady;
#pragma warning restore CA1822 // Mark members as static
    }
}

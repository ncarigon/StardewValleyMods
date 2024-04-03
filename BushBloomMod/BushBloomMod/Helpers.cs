using System;
using System.Linq;
using StardewValley;

namespace BushBloomMod {
    public static class Helpers {
        private static readonly Season Invalid = (Season)(-3);

        public static int GetDayOfYear(string season, int? dayOfMonth) =>
            (dayOfMonth ?? 0)
            + (int)(Enum.TryParse<Season>(season?.Trim(), true, out var s) ? s : Invalid) * 28;

        public static bool IsValidDayOfYear(string season, int? dayOfMonth) =>
            Enum.TryParse<Season>(season?.Trim(), true, out _)
            && dayOfMonth is not null && dayOfMonth > 0 && dayOfMonth < 29;

        public static string GetItemIdFromName(string name) {
            if (!string.IsNullOrWhiteSpace(name)) {
                name = name.Trim();
                if (int.TryParse(name, out var i)) {
                    name = $"(O){i}";
                }
                var item = ItemRegistry.GetMetadata(name);
                if (!item.Exists()) {
                    item = ItemRegistry.GetMetadata($"(O){name}");
                }
                if (item.Exists()) {
                    return item.QualifiedItemId[3..];
                }
                return Game1.objectData
                    .Where(d => string.Compare(d.Value.Name, name, true) == 0)
                    .Select(d => d.Key)
                    .FirstOrDefault();

            }
            return null;
        }
    }
}
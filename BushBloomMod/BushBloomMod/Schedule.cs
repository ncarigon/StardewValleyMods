using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BushBloomMod {
    internal class Schedule {
        private static IModHelper Helper;
        private static IMonitor Monitor;
        private static Config Config;

        public static void Register(IModHelper helper, IMonitor monitor, Config config) {
            Helper = helper;
            Monitor = monitor;
            Config = config;
        }

        public static bool IsReady { get; private set; }

        private static string MakeId(ContentEntry entry, string shakeOff = null) => $"{shakeOff ?? entry.ShakeOff};{Helpers.GetDayOfYear(entry.StartSeason, entry.StartDay)};{Helpers.GetDayOfYear(entry.EndSeason, entry.EndDay)};{entry.StartYear};{entry.EndYear};{entry.Chance};{string.Join(",", entry.Locations ?? Array.Empty<string>())};{string.Join(",", entry.ExcludeLocations ?? Array.Empty<string>())};{string.Join(",", entry.Weather ?? Array.Empty<string>())};{string.Join(",", entry.ExcludeWeather ?? Array.Empty<string>())};{string.Join(",", entry.DestroyWeather ?? Array.Empty<string>())};{string.Join(",", entry.Tiles ?? Array.Empty<Vector2>())};{entry.Texture}";

        private bool IsDefault;

        public bool IsEnabled() => Config.EnableDefaultSchedules || !this.IsDefault;

        public ContentEntry Entry { get; private set; }

        public Texture2D Texture { get; private set; }

        private static string GetShortDir(string dir) => Regex.Replace(dir ?? "", @"^.+[\\\/]Mods[\\\/]", "");

        // need to validate this later in startup to ensure other mod content has loaded and can be found
        private string _shakeOffId;
        public string ShakeOffId {
            get => this._shakeOffId ??= Helpers.GetItemIdFromName(this.Entry.ShakeOff);
        }

        // need to delay id construction for the same reason as shake off id
        private string _id;
        public string Id {
            get => this._id ??= MakeId(this.Entry, this.ShakeOffId);
        }

        public static readonly List<Schedule> Entries = new();

        public static async void ReloadEntries(int tries = 1) {
            IsReady = false;
            while (tries-- > 0) {
                // need to add a delay to help ensure all content packs have loaded
                await Task.Delay(500);
                Entries.Clear();
                try {
                    AddEntries(Helper.DirectoryPath, Helper.Data.ReadJsonFile<ContentEntry[]>("content.json"));
                } catch {
                    Monitor.Log($"Unable to load content pack: {GetShortDir(Helper.DirectoryPath)}. Review that file for syntax errors.", LogLevel.Error);
                }
                foreach (var pack in Helper.ContentPacks.GetOwned()) {
                    try {
                        AddEntries(pack.DirectoryPath, pack.ReadJsonFile<ContentEntry[]>("content.json"));
                    } catch {
                        Monitor.Log($"Unable to load content pack: {GetShortDir(pack.DirectoryPath)}. Review that file for syntax errors.", LogLevel.Error);
                    }
                }
                // check if any schedules have invalid items, likely a content pack that hasn't loaded yet
                if (!Entries.Any(e => e.ShakeOffId is null)) {
                    IsReady = true;
                    break;
                }
            }
        }

        private static void AddEntries(string contentDirectory, ContentEntry[] content) {
            var isDefault = Entries.Count == 0;
            foreach (var entry in content) {
                entry.EndSeason ??= entry.StartSeason;
                entry.EndDay ??= entry.StartDay;
                entry.StartYear ??= 1;
                entry.Chance ??= 0.2;
                var sched = new Schedule() {
                    IsDefault = isDefault,
                    Entry = entry
                };
                if (entry.Texture is not null) {
                    entry.Texture = string.Join(Path.DirectorySeparatorChar, entry.Texture.Split('/', '\\'));
                    var fullPath = Path.Combine(contentDirectory, entry.Texture);
                    try {
                        sched.Texture = Texture2D.FromFile(Game1.graphics.GraphicsDevice, fullPath);
                    } catch {
                        Monitor.Log($"Error in content pack: {GetShortDir(contentDirectory)}; Failed to load texture: {MakeId(entry)}", LogLevel.Error);
                        continue;
                    }
                }
                if (!entry.IsValid()) {
                    Monitor.Log($"Error in content pack: {GetShortDir(contentDirectory)}; Invalid bloom schedule: {MakeId(entry)}", LogLevel.Error);
                    continue;
                }
                Entries.Add(sched);
            }
        }

        public static IEnumerable<Schedule> GetAllCandidates(int year, int doy, bool ignoreWeather, bool allowExisting, GameLocation location = null, Vector2? tile = null) {
            var candidates = Entries.Where(e => e.IsEnabled() && e.Entry.CanBloomToday(year, doy, ignoreWeather, allowExisting, location, tile));
            return candidates.Any(e => e.Entry.Chance > 1f)
                ? candidates.Where(e => e.Entry.Chance > 1f)
                : candidates;
        }

        private const string KeyBushDay = "bush-day", KeyBushSchedule = "bush-schedule";

        public static void SetSchedule(Bush bush, string id) => bush.modData[$"{Helper.ModContent.ModID}/{KeyBushSchedule}"] = id;
        
        public static bool TryGetExistingSchedule(Bush bush, out Schedule schedule) => (schedule = GetExistingSchedule(bush)) is not null;

        public static Schedule GetExistingSchedule(Bush bush) {
            if (bush.IsAbleToBloom()
                && bush.modData.TryGetValue($"{Helper.ModContent.ModID}/{KeyBushSchedule}", out var value)
                && value is not null
                && value != "-1"
            ) {
                return Entries.Where(e => e.IsEnabled() && string.Compare(e.Id, value) == 0).FirstOrDefault();
            }
            return null;
        }

        public static Schedule GetSchedule(Bush bush) {
            if (!bush.IsAbleToBloom())
                return null;
            Schedule entry = null;
            var doy = Helpers.GetDayOfYear(bush.Location.GetSeasonKey(), Game1.dayOfMonth);
            var yDays = (Game1.year - 1) * 112;
            // check if we already picked a schedule for this bush and it's still valid
            if (bush.modData.TryGetValue($"{Helper.ModContent.ModID}/{KeyBushDay}", out var value) && int.TryParse(value ?? "", out var i)
                && i == doy + yDays - 1 // it had to be yesterday's schedule
                && bush.modData.TryGetValue($"{Helper.ModContent.ModID}/{KeyBushSchedule}", out value) && value is not null && value != "-1"
            ) {
                // check for valid schedules
                entry = GetAllCandidates(Game1.year, doy, false, true, bush.Location, bush.Tile).FirstOrDefault(e => e.IsEnabled() && string.Compare(e.Id, value) == 0);
            }
            if (entry == null) { // found no existing valid schedule
                var candidates = GetAllCandidates(Game1.year, doy, false, false, bush.Location, bush.Tile);
                // check if bush is not blooming already, or is from an active schedule and use same
                if (bush.tileSheetOffset.Value == 0
                    || !bush.modData.TryGetValue($"{Helper.ModContent.ModID}/{KeyBushSchedule}", out value) || value is null
                    || (entry = candidates.FirstOrDefault(e => string.Compare(e.Id, value) == 0)) is null
                ) {
                    // else get a new active schedule
                    // calculate the chance of NOT blooming today, based on all schedules
                    var chance = candidates
                        .Select(e => 1.0 - e.Entry.Chance)
                        .DefaultIfEmpty(1.0)
                        .Aggregate((a, e) => a * e);
                    // is the bush blooming?
                    if (Game1.random.NextDouble() > chance) {
                        // select a schedule from the candidates
                        var selected = candidates.Sum(e => e.Entry.Chance) * Game1.random.NextDouble();
                        entry = candidates
                            // find the selected schedule
                            .FirstOrDefault(e => (selected -= e.Entry.Chance) <= 0.0);
                    }
                }
            }
            bush.modData[$"{Helper.ModContent.ModID}/{KeyBushDay}"] = (doy + yDays).ToString();
            SetSchedule(bush, entry?.Id ?? "-1");
            return entry;
        }
    }
}
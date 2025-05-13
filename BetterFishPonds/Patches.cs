using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace BetterFishPonds {
    internal static class Patches {
        public static void Register() {
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(FishPond), "addFishToPond"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_FishPond_addFishToPond))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(FishPond), "CreateFishInstance"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_FishPond_CreateFishInstance))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(FishPond), "SpawnFish"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_FishPond_SpawnFish)),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_FishPond_SpawnFish))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(FishPond), "GetFishProduce"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_FishPond_GetFishProduce))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(FishingRod), "pullFishFromWater"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Prefix_FishingRod_pullFishFromWater))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Constructor(typeof(PondQueryMenu),new Type[] { typeof(FishPond) }),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_PondQueryMenu_ctor))
            );
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(PondQueryMenu), "draw", new Type[] { typeof(SpriteBatch) }),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_PondQueryMenu_draw)),
                transpiler: new HarmonyMethod(typeof(Patches), nameof(Transpile_PondQueryMenu_draw))
            );
            if (ModEntry.Instance?.Helper?.ModRegistry.IsLoaded("DaLion.Professions") == true) {
                try {
                    ModEntry.Instance?.ModHarmony?.Patch(
                        original: AccessTools.Method("DaLion.Professions.Framework.Patchers.Fishing.PondQueryMenuDrawPatcher:PondQueryMenuDrawPrefix"),
                        transpiler: new HarmonyMethod(typeof(Patches), nameof(Transpile_PondQueryMenu_draw))
                    );
                } catch { }
            }
        }

        #region Read/Write FishData
        private static List<int> GetFishData(FishPond pond, bool addingFish = false) {
            if (!pond.modData.TryGetValue($"{ModEntry.Instance?.ModManifest.UniqueID}/FishData", out var data)) {
                var convert = new int[] { 0, 0, 0, 0, 0 };
                if (pond.modData.TryGetValue($"DaLion.Ponds/PondFish", out data)) {
                    foreach (var q in data.Split(';').Select(d => d.Split(','))
                        .Where(d => d.Length == 2)
                        .Select(d => int.TryParse(d[1], out var i) && i >= 0 && i <= 4 ? i : -1)
                        .Where(d => d >= 0)
                    ) {
                        convert[q]++;
                    }
                } else if (!addingFish) {
                    convert[0] = pond.FishCount;
                }
                SetFishData(pond, convert);
                data = pond.modData[$"{ModEntry.Instance?.ModManifest.UniqueID}/FishData"];
            }
            return data
                .Split(' ')
                .Select(i => int.TryParse(i, out var quantity) && quantity >= 0 ? quantity : -1)
                .Where(i => i >= 0)
                .ToList();
        }

        private static void SetFishData(FishPond pond, IEnumerable<int> qualities) {
            pond.modData[$"{ModEntry.Instance?.ModManifest.UniqueID}/FishData"] =
                string.Join(' ', qualities.Where(f => f >= 0));
        }

        private static void AddFish(FishPond pond, int quality) {
            var data = GetFishData(pond, true);
            while (data.Count < quality + 1) {
                data.Add(0);
            }
            data[quality] += 1;
            SetFishData(pond, data);
            if (data.Sum() > 0 && GetAgeData(pond) < 1) {
                SetAgeData(pond, GetAbsoluteDay());
            }
        }

        private static int? RemoveFish(FishPond pond) {
            var data = GetFishData(pond);
            for (var i = 0; i < data.Count; i++) {
                if (data[i] > 0) {
                    data[i]--;
                    SetFishData(pond, data);
                    if (data.Sum() < 1) {
                        SetAgeData(pond, 0);
                    }
                    return i;
                }
            }
            return null;
        }
        #endregion

        #region Read/Write AgeData
        private static int GetAbsoluteDay() {
            return Game1.dayOfMonth + (Game1.seasonIndex * 28) + ((Game1.year - 1) * 28 * 4);
        }

        private static int GetAgeData(FishPond pond) {
            if (!pond.modData.TryGetValue($"{ModEntry.Instance?.ModManifest.UniqueID}/AgeData", out var age)) {
                SetAgeData(pond, 0);
                age = pond.modData[$"{ModEntry.Instance?.ModManifest.UniqueID}/AgeData"];
            }
            return int.TryParse(age, out var i) && i > 0 ? i : 0;
        }

        private static void SetAgeData(FishPond pond, int age) {
            pond.modData[$"{ModEntry.Instance?.ModManifest.UniqueID}/AgeData"] = $"{age}";
        }
        #endregion

        #region Fish added to pond
        private static void Postfix_FishPond_addFishToPond(
            FishPond __instance, SObject fish
        ) {
            AddFish(__instance, fish.Quality);
        }
        #endregion

        #region Fish removed from pond
        private static int? LastFishQuality;

        private static void Postfix_FishPond_CreateFishInstance(
            FishPond __instance, ref Item __result
        ) {
            LastFishQuality = RemoveFish(__instance);
            if (LastFishQuality.HasValue) {
                __result.Quality = LastFishQuality.Value;
            }
        }

        private static void Prefix_FishingRod_pullFishFromWater(
            ref int fishQuality, bool fromFishPond
        ) {
            if (fromFishPond && LastFishQuality.HasValue) {
                fishQuality = LastFishQuality.Value;
                LastFishQuality = null;
            }
        }
        #endregion

        #region Pond menu opened, draw quality fish
        private static int[]? LastPondData;
        private static int CurrentDrawCount;

        private static void Postfix_PondQueryMenu_ctor(
            FishPond fish_pond
        ) {
            // track fish qualities
            var fish = GetFishData(fish_pond);
            var data = new List<int>();
            for (var i = 0; i < fish.Count; i++) {
                for (var j = 0; j < fish[i]; j++) {
                    data.Add(i);
                }
            }
            LastPondData = data.ToArray();
            CurrentDrawCount = 0;
        }

        private static void Postfix_PondQueryMenu_draw() {
            // clear fish qualities
            CurrentDrawCount = 0;
        }

        private static IEnumerable<CodeInstruction> Transpile_PondQueryMenu_draw(IEnumerable<CodeInstruction> instructions) {
            foreach (var instruction in instructions) {
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand?.ToString()?.Contains("drawInMenu") == true) {
                    // swap obj.drawInMenu call with custom method below
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(Patches), nameof(DrawInMenu));
                }
                yield return instruction;
            }
        }

        private static void DrawInMenu(SObject obj, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
            if (color == Color.White && LastPondData is not null && CurrentDrawCount < LastPondData.Length) {
                // set fish quality and show it
                obj.Quality = LastPondData[CurrentDrawCount++];
                drawStackNumber = StackDrawType.HideButShowQuality;
            }
            // call original method
            obj.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
            // reset quality
            obj.Quality = 0;
        }
        #endregion

        #region New fish spawn attempt
        private static int ClampQuality(int quality) {
            // must be at least normal quality
            quality = Math.Max(0, quality);
            if (quality > 2) {
                // anything better than gold must be iridium
                quality = 4;
            }
            return quality;
        }

        private static int ChooseRandomQuality(FishPond pond) {
            if (!Config.Instance.AllowQualityIncrease) {
                return 0;
            }

            var r = new Random(pond.id.Value.GetHashCode() + GetAbsoluteDay());
            var quality = 0;
            var fish = GetFishData(pond);
            // add chance to increase quality based on each population quality
            for (var i = 1; i < fish.Count; i++) {
                for (var j = 0; j < fish[i]; j++) {
                    if (r.NextDouble() < (Config.Instance.PerFishChanceToIncreaseQuality / 100.0) * i) {
                        quality++;
                    }
                }
            }
            // add chance to increase quality based on owner's fishing level
            if (Config.Instance.PerLevelChanceToIncreaseQuality > 0) {
                var level = (Game1.GetPlayer(pond.owner.Value, true) ?? Game1.MasterPlayer).FishingLevel;
                for (var i = 0; i < level; i++) {
                    if (r.NextDouble() < Config.Instance.PerLevelChanceToIncreaseQuality / 100.0) {
                        quality++;
                    }
                }
            }
            // add chance to increase quality based on age of pond population
            var age = GetAgeData(pond);
            if (age > 0 && Config.Instance.PerSeasonChanceToIncreaseQuality > 0) {
                var seasons = (GetAbsoluteDay() - age) / 28.0;
                while (seasons-- >= 1) {
                    if (r.NextDouble() < Config.Instance.PerSeasonChanceToIncreaseQuality / 100.0) {
                        quality++;
                    }
                }
            }
            return ClampQuality(quality);
        }

        private static void Prefix_FishPond_SpawnFish(
            FishPond __instance
        ) {
            if (__instance.currentOccupants.Value >= __instance.maxOccupants.Value && __instance.currentOccupants.Value > 0
                && Config.Instance.AllowSpawnedFishAsProduce
            ) {
                // full capacity, spawn will fail, check as produce
                var quality = Config.Instance.AllowQualitySpawn ? ChooseRandomQuality(__instance) : 0;
                if (__instance.output.Value is null) {
                    // no existing output = create fish as output
                    __instance.output.Value = new SObject(__instance.fishType.Value, 1, quality: quality);
                    __instance.daysSinceSpawn.Value = 0;
                } else if (__instance.output.Value.ItemId.Equals(__instance.fishType.Value) && __instance.output.Value.Quality == quality) {
                    // existing output is fish with same quality = increment amount
                    __instance.output.Value.Stack += 1;
                    __instance.daysSinceSpawn.Value = 0;
                }
            }
        }

        private static void Postfix_FishPond_SpawnFish(
            FishPond __instance
        ) {
            if (__instance.hasSpawnedFish.Value) {
                // base game spawned a new fish in pond
                var quality = Config.Instance.AllowQualitySpawn ? ChooseRandomQuality(__instance) : 0;
                AddFish(__instance, quality);
            }
        }
        #endregion

        #region Fish produce spawned, check to apply quality
        private static void Postfix_FishPond_GetFishProduce(
            FishPond __instance, ref Item __result
        ) {
            if (__result is null) {
                if (Config.Instance.DontRemoveExistingProduce) {
                    __result = __instance.output.Value;
                }
            } else {
                switch (__result.Category) {
                    case 0: // no category
                        if (Config.Instance.AllowQualityTreasure
                            && Config.Instance.IncludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;

                    case -2: // gems
                        if (Config.Instance.AllowQualityGems
                            && !Config.Instance.ExcludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;
                    case -12: // minerals
                        if (Config.Instance.AllowQualityMinerals
                            && !Config.Instance.ExcludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;

                    case -7: //cooking
                        if (Config.Instance.AllowQualityCooking
                            && !Config.Instance.ExcludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;

                    case -4: //fish
                    case -5: //egg
                    case -6: //milk
                    case -14: //meat
                        if (Config.Instance.AllowQualityAnimalProducts
                            && !Config.Instance.ExcludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;

                    case -23: // beach forage
                        if (Config.Instance.AllowQualityRoe && __result.Name.EndsWith(" Roe")
                            && !Config.Instance.ExcludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        } else if (Config.Instance.AllowQualityBeachForage
                            && Config.Instance.IncludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;

                    case -26: //artisan goods
                    case -27: //artisan syrup
                        if (Config.Instance.AllowQualityArtisanGoods
                            && !Config.Instance.ExcludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;

                    case -75: //vegetable
                    case -79: //fruit
                    case -80: //flower
                    case -81: //greens
                        if (Config.Instance.AllowQualityCrops
                            && !Config.Instance.ExcludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;
                    default: // everything else
                        if (Config.Instance.AllowQualityOther
                            && Config.Instance.IncludeItemNames.Contains(__result.Name)
                        ) {
                            __result.Quality = ChooseRandomQuality(__instance);
                        }
                        break;
                }
            }
        }
        #endregion
    }
}
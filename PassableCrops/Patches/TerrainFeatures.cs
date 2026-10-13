using HarmonyLib;
using StardewValley.TerrainFeatures;
using StardewValley;

namespace PassableCrops.Patches {
    internal static class TerrainFeatures {
        private static ModEntry? Mod;

        public static void Register(ModEntry mod) {
            Mod = mod;
            var harmony = new Harmony(Mod?.ModManifest?.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(TerrainFeature), nameof(TerrainFeature.doCollisionAction)),
                postfix: new HarmonyMethod(typeof(TerrainFeatures), nameof(TerrainFeature_doCollisionAction))
            );
        }

        internal static void TerrainFeature_doCollisionAction(
            TerrainFeature __instance,
            Character who
        ) {
            var passable = false;
            if (who is Farmer farmer && farmer is not null) {
                if (Mod?.Config?.SlowDownWhenPassing == true) {
                    var isBig = false;
                    if (__instance is Bush bush && Bushes.AnyPassable(bush)) {
                        passable = true;
                        isBig = bush.getAge() > 19;
                    } else if (__instance is FruitTree fruit && FruitTrees.AnyPassable(fruit)) {
                        passable = true;
                        isBig = fruit.growthStage.Value > 1;
                    } else if (__instance is Tree tree && Trees.AnyPassable(tree)) {
                        passable = true;
                        isBig = tree.growthStage.Value > 2;
                    } else if (__instance is HoeDirt dirt && dirt?.crop?.raisedSeeds?.Value == true) {
                        passable = true;
                        isBig = true;
                    }
                    if (passable) {
                        var amount = (isBig && Mod?.Config?.SlowDownMore == true ? -3f : -1f) * (farmer.stats.Get("Book_Grass") != 0 ? 0.33f : 1f);
                        farmer.temporarySpeedBuff = amount;
                    }
                }
            }
            if (passable && who is not FarmAnimal) {
                Mod?.PlayRustleSound(__instance.Tile, __instance.Location);
            }
        }
    }
}

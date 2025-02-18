using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace UsableHayHoppers {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            if (helper is not null) {
                new Harmony(helper.ModContent.ModID).PatchAll(Assembly.GetExecutingAssembly());
            }
        }

        [HarmonyPatch]
        private static class Patches {
            [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.numberOfObjectsWithName))]
            private static void Postfix(GameLocation __instance, ref int __result, string name) {
                if (name?.Equals("Hay") == true // looking for hay
                    && __instance is AnimalHouse i // in an animal house
                    && Math.Min(Math.Max(1, Math.Min(i.animalsThatLiveHere.Count, __instance.GetRootLocation().piecesOfHay.Value)), i.animalLimit.Value - __result) < 1
                ) {
                    // base game logic will return 0 hay so we skew that by 1 to ensure a pickup
                    __result -= 1;
                }
            }

            [HarmonyPatch(typeof(Building), nameof(Building.load))]
            private static void Postfix(Building __instance) {
                // make all hay hoppers in animal houses able to be picked up
                (__instance?.GetIndoors() as AnimalHouse)?.Objects?.Values?.Where(o => o?.QualifiedItemId?.Equals("(BC)99") == true).Do(o => o.Fragility = 0);
            }

            [HarmonyPatch(typeof(AnimalHouse), nameof(AnimalHouse.feedAllAnimals))]
            private static bool Prefix(AnimalHouse __instance) {
                // only auto-feed in animal houses that contain hay hoppers
                return __instance?.Objects?.Values?.Any(o => o?.QualifiedItemId?.Equals("(BC)99") == true) == true;
            }
        }
    }
}

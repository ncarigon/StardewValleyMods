using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System.Reflection;
using System.Reflection.Emit;
using xTile.Dimensions;
using SObject = StardewValley.Object;

namespace UsableHayHoppers {
    [HarmonyPatch]
    internal static class Patches {
        private static int CountHayHopper(GameLocation location) {
            return (location?.Objects?.Values?.Count(o => o?.QualifiedItemId?.Equals("(BC)99") == true) ?? 0)
                + (location?.buildings?.Sum(b => b?.GetIndoors()?.Objects?.Values?.Count(o => o?.QualifiedItemId?.Equals("(BC)99") == true)) ?? 0);
        }

        private static Item CreateHayStack(int count) {
            var hay = ItemRegistry.Create("(O)178").getOne();
            hay.Stack = count;
            return hay;
        }

        [HarmonyPatch]
        private static class AlwaysPullHayFromHoppers {
            [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.numberOfObjectsWithName))]
            private static void Postfix(GameLocation __instance, ref int __result, string name) {
                // base game only allows players to pull hay from hoppers if there are empty feed trough spots
                // this patch ensures that check always returns at least one empty feed trough to allow pickup in any situation
                if (name?.Equals("Hay") == true // looking for hay
                    && __instance is AnimalHouse i // in an animal house
                    && Math.Min(Math.Max(1, Math.Min(i.animalsThatLiveHere.Count, __instance.GetRootLocation().piecesOfHay.Value)), i.animalLimit.Value - __result) < 1
                ) {
                    // base game logic will return 0 hay so we skew that by 1 to ensure a pickup
                    __result -= 1;
                }
            }
        }

        [HarmonyPatch]
        private static class CutGrassWithoutSilo {
            [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.StoreHayInAnySilo))]
            private static void Postfix(ref int __result, GameLocation currentLocation) {
                // base game will not generate hay from scythed grass if no silo exists
                // this patch checks if any possible hay was not put in a silo and drops it as debris instead
                if (Config.Instance.CanScytheHayWithoutSilo && __result > 0) {
                    Game1.createItemDebris(CreateHayStack(__result), new Vector2(Game1.player.Tile.X * 64 + 32, Game1.player.Tile.Y * 64 + 32), -1, currentLocation);
                }
            }
        }

        [HarmonyPatch]
        private static class HayHoppersArePickupable {
            [HarmonyPatch(typeof(Building), nameof(Building.load))]
            private static void Postfix(Building __instance) {
                // hay hoppers in building data are all marked indestructible so they spawn as immovable
                // this patch makes all existing hay hoppers in any building able to be picked up or not, depending on config
                __instance?.GetIndoors()?.Objects?.Values?.Where(o => o?.QualifiedItemId?.Equals("(BC)99") == true).Do(o => o.Fragility = Config.Instance.CanPickupHayHoppers ? 0 : 2);
            }

            [HarmonyPatch(typeof(SObject), nameof(SObject.performDropDownAction))]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                // the base game shows hay in the hopper if there are "piecesOfHay >= 0"
                // this needs to change to "piecesOfHay > 0" to prevent showing hay with no/empty silo
                return new CodeMatcher(instructions)
                    .MatchStartForward(new CodeMatch(CodeInstruction.LoadField(typeof(GameLocation), nameof(GameLocation.piecesOfHay)))) // find check for hay count
                    .MatchStartForward(new CodeMatch(new CodeInstruction(OpCodes.Ldc_I4_0))) // find next 0 comparison
                    .Set(OpCodes.Ldc_I4_1, null) // change to 1 comparison
                    .Instructions();
            }
        }

        [HarmonyPatch]
        private static class OnlyAutoFeedWithHopper {
            [HarmonyPatch(typeof(AnimalHouse), nameof(AnimalHouse.feedAllAnimals))]
            private static bool Prefix(AnimalHouse __instance) {
                // base game does not actually look for a hay hopper in any building before attempting an auto-feed
                // this patch prevents auto-feed in animal houses that don't contain a hay hopper
                return __instance?.Objects?.Values?.Any(o => o?.QualifiedItemId?.Equals("(BC)99") == true) == true;
            }
        }

        [HarmonyPatch]
        private static class PullHayFromSilo {
            private static readonly SObject FakeHayHopper = new(Vector2.Zero, "99");
            private static readonly MethodInfo CheckForActionOnFeedHopper = AccessTools.Method(typeof(SObject), "CheckForActionOnFeedHopper");
            private static GameLocation? LastLocation;

            [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new Type[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
            private static void Prefix(GameLocation __instance, ref string[] action, Farmer who) {
                // base game only allows players to put hay into a silo
                // this patch allows pulling hay from the silo, using a fake hay hopper
                if (who?.IsLocalPlayer == true
                    && action?.FirstOrDefault()?.Equals("BuildingSilo") == true // interacting with a silo
                ) {
                    if (who?.ActiveObject?.QualifiedItemId?.Equals("(O)178") == true) { // holding hay
                                                                                        // track location when putting hay into silo so we can change the UI message later
                        LastLocation = __instance;
                    } else { // not holding hay
                        FakeHayHopper.Location = who?.currentLocation ?? __instance; // set correct location
                        CheckForActionOnFeedHopper.Invoke(FakeHayHopper, new object?[] { who, false }); // pull hay from the fake hopper
                        action[0] = ""; // prevent default behavior
                    }
                }
            }

            [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new Type[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
            private static void Postfix() {
                // clear location tracking to prevent any oddities
                LastLocation = null;
            }

            [HarmonyPatch(typeof(Game1), nameof(Game1.drawObjectDialogue), new Type[] { typeof(string) })]
            private static void Prefix(ref string dialogue) {
                // if location tracking is found, player is adding hay to silo. swap UI message with previous "check silo" message.
                if (LastLocation is not null) {
                    var location = LastLocation;
                    LastLocation = null;
                    dialogue = Game1.content.LoadString("Strings\\Buildings:PiecesOfHay", location.piecesOfHay.Value, location.GetHayCapacity());
                }
            }
        }

        [HarmonyPatch]
        private static class HayHoppersHoldHay {
            [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.GetHayCapacity))]
            private static void Postfix(GameLocation __instance, ref int __result) {
                // if any hay hoppers exist on the map, add their hay capacity
                __result += CountHayHopper(__instance) * Config.Instance.HayHopperCapacity;
            }

            [HarmonyPatch(typeof(SObject), nameof(SObject.performRemoveAction))]
            private static void Postfix(SObject __instance) {
                // if a hay hopper is being picked up, drop excess hay due to it's lost capacity
                if (__instance?.QualifiedItemId?.Equals("(BC)99") == true) {
                    var location = __instance.Location?.GetRootLocation() ?? Game1.player.currentLocation;
                    var diff = location.GetHayCapacity() - Config.Instance.HayHopperCapacity - location.piecesOfHay.Value;
                    if (diff < 0) {
                        location.piecesOfHay.Value += diff;
                        Game1.createItemDebris(CreateHayStack(-diff), new Vector2(Game1.player.Tile.X * 64 + 32, Game1.player.Tile.Y * 64 + 32), -1, __instance.Location);
                    }
                }
            }
        }

    }
}

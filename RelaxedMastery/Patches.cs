using HarmonyLib;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Menus;
using System.Reflection;
using System.Reflection.Emit;
using xTile.Dimensions;

namespace RelaxedMastery {
    internal static class Patches {
        [HarmonyPatch(typeof(Farmer), nameof(Farmer.gainExperience))]
        private static class Farmer_gainExperience {
            [HarmonyPriority(1000)] // need very high priority to get pre-experience
            private static void Prefix(
                ref uint? __state, Farmer __instance, int which, int howMuch
            ) {
                __state = null;
                if (__instance is not null // player exists
                    && !(which == 5 || howMuch <= 0) // skill can gain experience
                    && (!(!__instance.IsLocalPlayer && Game1.IsServer)) // is local player
                ) {
                    // store mastery exp value
                    __state = Game1.stats.Get("MasteryExp");
                }
            }

            private static void Postfix(
                uint? __state, Farmer __instance, int which, int howMuch
            ) {
                if (__state is not null // prefix checks passed
                    && __state == Game1.stats.Get("MasteryExp") // did not accumulate mastery exp
                    && __instance.GetSkillLevel(which) >= 10 // at mastery level
                    && (Config.Instance?.MasteredSkillsGainExp == true // allow continued experience
                        || __instance.stats.Get(StatKeys.Mastery(which)) == 0) // not mastered
                ) {
                    // duplicate of base game mastery accumulation logic
                    int old = MasteryTrackerMenu.getCurrentMasteryLevel();
                    Game1.stats.Increment("MasteryExp", Math.Max(1, (which == 0) ? (howMuch / 2) : howMuch));
                    if (MasteryTrackerMenu.getCurrentMasteryLevel() > old) {
                        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:Mastery_newlevel"));
                        Game1.playSound("newArtifact");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new Type[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
        private static class GameLocation_performAction {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                var found = 0;
                foreach (var instruction in instructions) {
                    switch (found) {
                        case 0 when instruction.operand?.ToString()?.Contains("MasteryRoom") == true: // interacting with Mastery Room entrance
                        case 1 when instruction.operand?.ToString()?.Contains("combatLevel") == true: // checked all skill levels
                            found++;
                            break;
                        case 2 when instruction.opcode == OpCodes.Ldc_I4_5: // checking if at least "5" skills are level 10
                            found++;
                            instruction.opcode = OpCodes.Ldc_I4_0; // replace with at least "0" levels are 10
                            break;
                    }
                    yield return instruction;
                }
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.MakeMapModifications))]
        private static class GameLocation_MakeMapModifications {
            private static void Postfix(GameLocation __instance) {
                if (__instance?.name?.Value?.Equals("MasteryCave") == true) {
                    if (Game1.player.farmingLevel.Value < 10) {
                        __instance.removeTemporarySpritesWithID(8765);
                    }
                    if (Game1.player.fishingLevel.Value < 10) {
                        __instance.removeTemporarySpritesWithID(8766);
                    }
                    if (Game1.player.foragingLevel.Value < 10) {
                        __instance.removeTemporarySpritesWithID(8767);
                    }
                    if (Game1.player.miningLevel.Value < 10) {
                        __instance.removeTemporarySpritesWithID(8768);
                    }
                    if (Game1.player.combatLevel.Value < 10) {
                        __instance.removeTemporarySpritesWithID(8769);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MasteryTrackerMenu))]
        private static class MasteryTrackerMenu_Patches {
            [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(int) })]
            private static void Postfix(ref bool ___canClaim, int whichSkill) {
                switch (whichSkill) {
                    case 0 when Game1.player.farmingLevel.Value < 10:
                    case 1 when Game1.player.fishingLevel.Value < 10:
                    case 2 when Game1.player.foragingLevel.Value < 10:
                    case 3 when Game1.player.miningLevel.Value < 10:
                    case 4 when Game1.player.combatLevel.Value < 10:
                        ___canClaim = false;
                        break;
                }
            }

            private static int? LastWhich = null;

            [HarmonyPatch(nameof(MasteryTrackerMenu.draw))]
            private static void Prefix(int ___which) {
                LastWhich = ___which;
            }

            [HarmonyPatch(nameof(MasteryTrackerMenu.getCurrentMasteryLevel))]
            private static void Postfix(ref int __result) {
                if (LastWhich.HasValue) {
                    switch (LastWhich.Value) {
                        case 0 when Game1.player.farmingLevel.Value < 10:
                        case 1 when Game1.player.fishingLevel.Value < 10:
                        case 2 when Game1.player.foragingLevel.Value < 10:
                        case 3 when Game1.player.miningLevel.Value < 10:
                        case 4 when Game1.player.combatLevel.Value < 10:
                            __result = 0;
                            break;
                    }
                    LastWhich = null;
                }
            }
        }

        [HarmonyPatch]
        private static class WalkOfLife_Patches {
            private static IEnumerable<MethodBase> TargetMethods() {
                var methods = new List<MethodBase>() {
                    AccessTools.Method(typeof(WalkOfLife_Patches), nameof(WalkOfLife_Patches.Empty))
                };
                try {
                    // patch out its custom mastery room message
                    methods.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Where(t => t?.FullName?.Equals("DaLion.Professions.Framework.Patchers.Prestige.GameLocationPerformActionPatcher") == true
                            || t?.FullName?.Equals("DaLion.Professions.Framework.Patchers.Prestige.MasteryTrackerMenuReceiveLeftClickPatcher") == true)
                        .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
                        .Where(m => m?.Name?.Equals("GameLocationPerformActionPrefix") == true
                            || m?.Name?.Equals("MasteryTrackerMenuReceiveLeftClickPrefix") == true)
                        .Cast<MethodBase>());
                } catch { }
                return methods;
            }

            private static bool Empty() {
                // empty method to ensure TargetMethods() does not return empty and crash Harmony
                return false;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _) {
                // this is a generic code replacement to skip out of a patch method and run the original code
                return new CodeInstruction[] {
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Ret)
                };
            }
        }
    }
}
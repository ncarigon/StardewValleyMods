using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;
using SMachineData = StardewValley.GameData.Machines.MachineData;
using System.Linq;
using System;

namespace CopperStill.ModPatches {
    internal static class MachineData {
        public static void Register() {
            ModEntry.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(MachineDataUtility), "GetOutputItem"),
                postfix: new HarmonyMethod(typeof(MachineData), nameof(Postfix_MachineDataUtility_GetOutputItem))
            );
            ModEntry.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(SObject), "PlaceInMachine"),
                prefix: new HarmonyMethod(typeof(MachineData), nameof(Prefix_Object_PlaceInMachine)),
                postfix: new HarmonyMethod(typeof(MachineData), nameof(Postfix_Object_PlaceInMachine))
            );
        }

        private static void Postfix_MachineDataUtility_GetOutputItem(
            ref Item __result,
            MachineItemOutput outputData, Item inputItem
        ) {
            if ((outputData?.PreserveId ?? "") == "DROP_IN_PRESERVE"
                && __result is SObject item1 && inputItem is SObject item2
                && item1 is not null & item2 is not null
            ) {
                item1!.preservedParentSheetIndex.Value = item2!.preservedParentSheetIndex.Value;
            }
        }

        private static bool FromOverride = false;

        private static void Prefix_Object_PlaceInMachine(
            ref bool probe, out Tuple<bool, bool>? __state
        ) {
            __state = new(false, probe);
            if (!FromOverride) {
                FromOverride = true;
                __state = new(true, probe);
                probe = true;
            }
        }

        private static void Postfix_Object_PlaceInMachine(
           SObject __instance, ref bool __result, Tuple<bool, bool>? __state,
           SMachineData machineData, Item inputItem, Farmer who, bool showMessages, bool playSounds
        ) {
            if (__state?.Item1 ?? false) {
                foreach (var machineOverride in Game1.content.Load<Dictionary<string, SMachineData>>("Data\\Machines")
                    .Where(m => m.Key?.StartsWith(__instance.QualifiedItemId) ?? false)
                    .Where(m => !m.Key.Equals(__instance.QualifiedItemId))
                    .Select(m => m.Value)
                ) {
                    if (MachineDataUtility.TryGetMachineOutputRule(
                        __instance, machineOverride, MachineOutputTrigger.ItemPlacedInMachine, inputItem, who, __instance.Location,
                        out _, out _, out _, out var triggerIgnoringCount)
                        || triggerIgnoringCount is not null
                    ) {
                        __result = __state.Item2 || __instance.PlaceInMachine(machineOverride, inputItem, __state.Item2, who, showMessages, playSounds);
                        FromOverride = false;
                        return;
                    }
                }
                __result = __instance.PlaceInMachine(machineData, inputItem, __state.Item2, who, showMessages, playSounds);
                FromOverride = false;
            }
        }
    }
}
